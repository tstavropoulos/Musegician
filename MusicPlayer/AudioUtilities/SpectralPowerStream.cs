using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CSCore;
using CSCore.DSP;
using CSCore.Streams.Effects;
using CSCore.Utils;
using Musegician.Core;

namespace Musegician.AudioUtilities
{
    public class SpectralPowerStream : SampleAggregatorBase
    {
        #region Data

        private float[] freqs;
        private int[] freqIndices;

        private int[] freqBandLB;
        private int[] freqBandUB;

        private RingBuffer<Complex> bufferL;
        private RingBuffer<Complex> bufferR;
        private Complex[] fftBufferL;
        private Complex[] fftBufferR;

        private double powerCoeff;

        private static readonly float[] defaultFrequencies = new float[]
        {
            32, 64, 125, 250, 500, 1000, 2000, 4000, 8000, 16000
        };

        private int samplesToProcess;
        //2^12 = 4096
        private const int FFT_EXP_MIN = 12;
        private const int MIN_PERIOD = 4;
        private const int SAMPLE_STRIDE = 2;
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
            get { return (int)(_blocksToProcess / (WaveFormat.SampleRate / (SAMPLE_STRIDE * 1000.0))); }
            set { _blocksToProcess = (int)(value * (WaveFormat.SampleRate / (SAMPLE_STRIDE * 1000.0))); }
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

            freqs = frequencies.ToArray();

            int sampleRate = source.WaveFormat.SampleRate;

            _fftSize = Math.Max(FFT_EXP_MIN,
               ToNextExponentOf2((int)Math.Ceiling(MIN_PERIOD * sampleRate / (SAMPLE_STRIDE * freqs[0]))));

            samplesToProcess = (int)Math.Pow(2, _fftSize);

            bufferL = new RingBuffer<Complex>(samplesToProcess);
            bufferR = new RingBuffer<Complex>(samplesToProcess);
            fftBufferL = new Complex[samplesToProcess];
            fftBufferR = new Complex[samplesToProcess];

            freqIndices = new int[freqs.Length];

            for (int i = 0; i < freqs.Length; i++)
            {
                freqIndices[i] = GetFrequencySample(
                    frequency: freqs[i],
                    fftSize: samplesToProcess,
                    sampleRate: sampleRate);
            }

            freqBandLB = new int[freqs.Length];
            freqBandUB = new int[freqs.Length];

            freqBandLB[0] = 1;
            freqBandUB[freqs.Length - 1] = samplesToProcess / 2;

            for (int i = 0; i < freqs.Length - 1; i++)
            {
                freqBandUB[i] = (freqIndices[i + 1] + freqIndices[i] + 1) / 2;
                freqBandLB[i + 1] = freqBandUB[i];
            }

            powerCoeff = sampleRate / (2.0 * SAMPLE_STRIDE * samplesToProcess);

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

            for (int i = offset; i < read; i += 2 * SAMPLE_STRIDE)
            {
                if (_blocksProcessed >= 0)
                {
                    bufferL.Add(new Complex(buffer[i]));
                    bufferR.Add(new Complex(buffer[i + 1]));
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
            bufferL.CopyTo(fftBufferL, 0);
            bufferR.CopyTo(fftBufferR, 0);

            for (int i = 0; i < samplesToProcess; i++)
            {
                fftBufferL[i].Real *= Hamming(i, samplesToProcess);
                fftBufferR[i].Real *= Hamming(i, samplesToProcess);
            }

            FastFourierTransformation.Fft(fftBufferL, _fftSize);
            FastFourierTransformation.Fft(fftBufferR, _fftSize);

            for (int i = 0; i < freqIndices.Length; i++)
            {
                PowerUpdate?.Invoke(
                    this,
                    new Equalizer.MeterUpdateArgs()
                    {
                        Power = GetPowers(i),
                        Index = i
                    });
            }
        }

        private void Reset()
        {
            _blocksProcessed = samplesToProcess - _blocksToProcess;
        }

        private (float, float) GetPowers(int index)
        {
            double left = 0.0;
            double right = 0.0;

            for (int i = freqBandLB[index]; i < freqBandUB[index]; i++)
            {
                left += fftBufferL[i].Value;
                right += fftBufferR[i].Value;
            }

            left *= powerCoeff;
            right *= powerCoeff;

            return (UnitaryClamp((float)left), UnitaryClamp((float)right));
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
            int value = (int)(frequency * SAMPLE_STRIDE * fftSize / sampleRate);

            return Math.Min(value, fftSize / 2);
        }

        public static double GetFrequency(int index, int fftSize, int sampleRate)
        {
            return index * ((double)sampleRate / SAMPLE_STRIDE) / fftSize;
        }

        public static float UnitaryClamp(float value)
        {
            return Math.Max(0, Math.Min(1, value));
        }

        #endregion Utility Methods
    }
}
