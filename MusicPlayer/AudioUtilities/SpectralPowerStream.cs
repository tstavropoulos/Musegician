using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CSCore;
using CSCore.DSP;
using CSCore.Streams.Effects;
using CSCore.Utils;

namespace MusicPlayer.AudioUtilities
{
    public class SpectralPowerStream : SampleAggregatorBase
    {
        #region Data

        private List<float> freqs;
        private List<int> freqIndices;

        private Complex[] bufferL;
        private Complex[] bufferR;
        private Complex[] fftBufferL;
        private Complex[] fftBufferR;

        private static readonly float[] defaultFrequencies = new float[]
        {
            32, 64, 125, 250, 500, 1000, 2000, 4000, 8000, 16000
        };

        private int samplesToProcess;
        //2^12 = 4096
        private const int FFT_EXP_MIN = 12;
        private const int MIN_PERIOD = 4;
        private int _fftSize = 12;

        private int _blocksToProcess = 0;
        private int _blocksProcessed = 0;

        #endregion Data
        #region Events

        public EventHandler<Equalizer.MeterUpdateArgs> PowerUpdate;

        #endregion
        #region Properties

        public int Interval
        {
            get { return (int)((1000.0 * _blocksToProcess) / WaveFormat.SampleRate); }
            set { _blocksToProcess = (int)((value / 1000.0) * WaveFormat.SampleRate); }
        }

        #endregion Properties
        #region Constructor

        public SpectralPowerStream(ISampleSource source, ICollection<float> frequencies)
            : base(source)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }

            if (frequencies is null || frequencies.Count == 0)
            {
                throw new ArgumentException("Frequencies must be defined");
            }

            if (source.WaveFormat.Channels != 2)
            {
                throw new ArgumentException("Source must have 2 channels");
            }

            freqs = new List<float>(frequencies);

            int sampleRate = source.WaveFormat.SampleRate;

            _fftSize = Math.Max(FFT_EXP_MIN,
               ToNextExponentOf2((int)Math.Ceiling(MIN_PERIOD * sampleRate / freqs[0])));

            samplesToProcess = (int)Math.Pow(2, _fftSize);

            bufferL = new Complex[samplesToProcess];
            bufferR = new Complex[samplesToProcess];
            fftBufferL = new Complex[samplesToProcess];
            fftBufferR = new Complex[samplesToProcess];

            freqIndices = new List<int>(freqs.Count);

            foreach (float freq in freqs)
            {
                freqIndices.Add(GetFrequencySample(
                    frequency: freq,
                    fftSize: samplesToProcess,
                    sampleRate: sampleRate));
            }

            Interval = 50;
        }

        public static SpectralPowerStream CreatePowerStream(ISampleSource source)
        {
            return CreatePowerStream(source, defaultFrequencies);
        }

        public static SpectralPowerStream CreatePowerStream(
            ISampleSource source,
            ICollection<float> frequencies)
        {
            return new SpectralPowerStream(source, frequencies);
        }

        #endregion Constructor
        #region SampleAggregatorBase Overrides

        public override int Read(float[] buffer, int offset, int count)
        {
            int read = base.Read(buffer, offset, count);

            for (int i = offset; i < read; i += 2)
            {
                if (_blocksProcessed >= 0)
                {
                    bufferL[_blocksProcessed].Real = buffer[i];
                    bufferR[_blocksProcessed].Real = buffer[i + 1];
                }
                
                _blocksProcessed++;

                if (_blocksProcessed == samplesToProcess)
                {
                    CalculatePower();
                    Reset();
                }

            }

            return read;
        }

        #endregion SampleAggregatorBase Overrides
        #region Helper Methods

        private void CalculatePower()
        {
            Array.Copy(
                sourceArray: bufferL,
                destinationArray: fftBufferL,
                length: samplesToProcess);

            Array.Copy(
                sourceArray: bufferR,
                destinationArray: fftBufferR,
                length: samplesToProcess);

            for (int i = 0; i < samplesToProcess; i++)
            {
                fftBufferL[i].Real *= Hamming(i, samplesToProcess);
                fftBufferR[i].Real *= Hamming(i, samplesToProcess);
            }

            FastFourierTransformation.Fft(fftBufferL, _fftSize);
            FastFourierTransformation.Fft(fftBufferR, _fftSize);

            for (int i = 0; i < freqIndices.Count; i++)
            {
                PowerUpdate?.Invoke(
                    this,
                    new Equalizer.MeterUpdateArgs()
                    {
                        Power = GetPowers(fftBufferL, fftBufferR, freqIndices[i]),
                        Index = i
                    });
            }
        }

        private void Reset()
        {
            _blocksProcessed = samplesToProcess - _blocksToProcess;

            if (_blocksProcessed > 0)
            {
                //Shift remaining samples left here
                Array.Copy(
                    sourceArray: bufferL,
                    sourceIndex: _blocksToProcess,
                    destinationArray: bufferL,
                    destinationIndex: 0,
                    length: _blocksProcessed);

                Array.Copy(
                    sourceArray: bufferR,
                    sourceIndex: _blocksToProcess,
                    destinationArray: bufferR,
                    destinationIndex: 0,
                    length: _blocksProcessed);
            }
        }

        #endregion Helper Methods
        #region Utility Methods

        private static int ToNextExponentOf2(int x)
        {
            return (int)Math.Ceiling(Math.Log(x) / Math.Log(2));
        }

        public delegate float WindowFunction(int index, int width);

        public static readonly WindowFunction Hamming = (index, width)
            => (float)(0.54 - 0.46 * Math.Cos((2 * Math.PI * index) / (width - 1)));

        public static int GetFrequencySample(float frequency, int fftSize, int sampleRate)
        {
            return (int)((frequency / (sampleRate / 2.0f)) * (fftSize / 2));
        }

        public static float UnitaryClamp(float value)
        {
            return Math.Max(0, Math.Min(1, value));
        }

        public static (float, float) GetPowers(Complex[] bufferL, Complex[] bufferR, int index)
        {
            const int radius = 4;

            int min = index - radius;
            int max = min + 2 * radius + 2;

            int count = bufferL.Length;

            double avgFactor = Math.Sqrt(count) / (2 * radius + 1);

            if (min < 0)
            {
                max -= min;
                min = 0;
            }

            if (max > count)
            {
                min -= max - count;
                max = count;
            }

            double left = 0.0;
            double right = 0.0;

            for (int i = min; i < max; i++)
            {
                left += bufferL[i].Value;
                right += bufferR[i].Value;
            }


            left *= avgFactor;
            right *= avgFactor;

            //left = Math.Log(left / avgFactor, 10.0) + 60.0;
            //right = Math.Log(right / avgFactor, 10.0) + 60.0;

            return (UnitaryClamp((float)left), UnitaryClamp((float)right));
        }

        #endregion Utility Methods
    }
}
