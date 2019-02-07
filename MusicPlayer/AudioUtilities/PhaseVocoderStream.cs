using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CSCore;
using CSCore.DSP;
using CSCore.Streams.Effects;
using CSCore.Utils;
using Musegician.Spatializer;
using System.IO;

namespace Musegician.AudioUtilities
{
    public class PhaseVocoderStream : SampleAggregatorBase
    {
        #region Data

        /// <summary>
        /// Holds samples from underlying stream
        /// </summary>
        private float[] localSampleBuffer;

        /// <summary>
        /// Store computed samples ready to deliver
        /// </summary>
        private float[] cachedSampleBuffer;

        private int _baseFFTSamples;
        private int _sampleOffset;

        private Complex[,] phasors;
        private Complex[][] inputBuffers;
        private Complex[][] expandedBuffers;
        private int[] windowCounts;
        private int[] inputWindowPowers;
        private int[] outputWindowPowers;
        private int[] freqIndices;


        private float _speed = 1f;

        private readonly int _channels;

        private const int BASE_FFT_SIZE = 12;
        private const int EXPANDED_FFT_SIZE = BASE_FFT_SIZE + 1;
        private const int PARTITION_COUNT = BASE_FFT_SIZE - 3;
        private const int INPUT_FFT_SIZE = 4;
        private const int INPUT_SIZE = 16;

        private int _bufferIndex = 0;
        private int _bufferCount = 0;

        private volatile object _bufferLock = new object();

        #endregion Data
        #region Properties

        public float Speed
        {
            get => _speed;
            set
            {
                if (_speed == value)
                {
                    return;
                }

                if (value < 0.5f || value > 1.0f)
                {
                    Console.WriteLine($"Error: Speed {value} not in range [0.5,1.0]");
                    _speed = (float)MathExt.Clamp(value, 0.5, 1.0);
                }
                else
                {
                    _speed = value;
                }
            }
        }

        #endregion
        #region Constructor

        public PhaseVocoderStream(ISampleSource source)
            : base(source)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }
            
            _channels = source.WaveFormat.Channels;

            lock (_bufferLock)
            {
                _baseFFTSamples = (int)Math.Pow(2, BASE_FFT_SIZE);

                int readSamples = _channels * _baseFFTSamples;

                localSampleBuffer = new float[readSamples];
                cachedSampleBuffer = new float[2 * readSamples];

                phasors = new Complex[PARTITION_COUNT, 4];

                inputBuffers = new Complex[PARTITION_COUNT][];
                expandedBuffers = new Complex[PARTITION_COUNT][];
                windowCounts = new int[PARTITION_COUNT];
                inputWindowPowers = new int[PARTITION_COUNT];
                outputWindowPowers = new int[PARTITION_COUNT];
                freqIndices = new int[PARTITION_COUNT + 1];
                freqIndices[0] = 4;

                for (int i = 0; i < PARTITION_COUNT; i++)
                {
                    inputWindowPowers[i] = BASE_FFT_SIZE - i;
                    outputWindowPowers[i] = EXPANDED_FFT_SIZE - i;

                    freqIndices[i + 1] = (int)Math.Pow(2, 3 + i);
                    inputBuffers[i] = new Complex[(int)Math.Pow(2, inputWindowPowers[i])];
                    expandedBuffers[i] = new Complex[(int)Math.Pow(2, outputWindowPowers[i])];

                    windowCounts[i] = (int)Math.Pow(2, i);

                    for (int j = 0; j < 4; j++)
                    {
                        //Initialize phasors to 2 so that it doubles the amplitudes on copy and rotation
                        phasors[i, j] = new Complex(2f, 0f);
                    }
                }

                _sampleOffset = (int)((INPUT_SIZE / 2) * (Math.Pow(2, EXPANDED_FFT_SIZE - BASE_FFT_SIZE) - 1));
            }
        }

        public static PhaseVocoderStream CreatePhaseVocodedStream(ISampleSource source) => new PhaseVocoderStream(source);


        #endregion Constructor
        #region SampleAggregatorBase Overrides

        public override int Read(float[] buffer, int offset, int count)
        {
            bool enabled = true;

            if (!enabled)
            {
                return base.Read(buffer, offset, count);
            }

            //Copy samples left from prior pulls
            int samplesWritten = ReadBody(buffer, offset, count);

            while (count > samplesWritten)
            {
                int read = base.Read(localSampleBuffer, 0, localSampleBuffer.Length);

                if (read <= 0)
                {
                    //Done
                    break;
                }
                else if (read < localSampleBuffer.Length)
                {
                    //Set rest to zero
                    Array.Clear(localSampleBuffer, read, localSampleBuffer.Length - read);
                }

                lock (_bufferLock)
                {
                    Array.Clear(cachedSampleBuffer, 0, cachedSampleBuffer.Length);

                    float requestedOutputSamples = _baseFFTSamples / Speed;
                    int smallestWindowOutputSamples = (int)Math.Round(requestedOutputSamples / windowCounts[PARTITION_COUNT - 1]);
                    int outputSampleCount = windowCounts[PARTITION_COUNT - 1] * smallestWindowOutputSamples;
                    _bufferCount = _channels * outputSampleCount;
                    _bufferIndex = 0;

                    int additionalSamples = outputSampleCount - _baseFFTSamples;

                    float effectiveSpeed = _baseFFTSamples / (float)outputSampleCount;
                    for (int partition = 0; partition < PARTITION_COUNT; partition++)
                    {
                        int inputSamplesPerWindow = _baseFFTSamples / windowCounts[partition];
                        int outputSamplesPerWindow = outputSampleCount / windowCounts[partition];
                        int additionalSamplesPerWindow = outputSamplesPerWindow - inputSamplesPerWindow;
                        for (int window = 0; window < windowCounts[partition]; window++)
                        {
                            int inputStartSample = _channels * window * inputSamplesPerWindow;
                            int outputStartSample = _channels * window * outputSamplesPerWindow;
                            for (int channel = 0; channel < _channels; channel++)
                            {
                                //Copy to buffer
                                for (int i = 0; i < inputBuffers[partition].Length; i++)
                                {
                                    inputBuffers[partition][i].Real = 
                                        Hamming(i, inputBuffers[partition].Length) *
                                        localSampleBuffer[inputStartSample + _channels * i + channel];

                                    inputBuffers[partition][i].Imaginary = 0f;
                                }

                                //FFT
                                FastFourierTransformation.Fft(inputBuffers[partition], inputWindowPowers[partition], FftMode.Forward);

                                //Clear IFFT Buffer
                                Array.Clear(expandedBuffers[partition], 0, expandedBuffers[partition].Length);

                                //Copy values into IFFT Buffer
                                for (int i = 0; i < 4; i++)
                                {
                                    expandedBuffers[partition][2*(i + 4)] = inputBuffers[partition][i + 4].Times(phasors[partition, i]);
                                }

                                //IFFT
                                FastFourierTransformation.Fft(expandedBuffers[partition], outputWindowPowers[partition], FftMode.Backward);

                                //Accumualte the window samples
                                for (int i = 0; i < outputSamplesPerWindow; i++)
                                {
                                    cachedSampleBuffer[outputStartSample + _channels * i + channel] += expandedBuffers[partition][i].Real;
                                }
                            }

                            //Advance phasor
                            for (int i = 0; i < 4; i++)
                            {
                                phasors[partition, i] = phasors[partition, i].Times(GetPhasor(2*(4 + i), effectiveSpeed));
                            }
                        }

                    }
                }

                samplesWritten += ReadBody(buffer, offset + samplesWritten, count - samplesWritten);
            }

            return samplesWritten;
        }

        public override long Position
        {
            get => (long)Math.Ceiling(base.Position / Speed);
            set
            {
                FlushBuffers();
                base.Position = Math.Min((long)Math.Floor(value * Speed), base.Length);
            }
        }

        public override long Length => (long)Math.Ceiling(base.Length / Speed);

        #endregion SampleAggregatorBase Overrides
        #region Helper Methods

        private void FlushBuffers()
        {
            lock (_bufferLock)
            {
                //Reset Phasors
                for (int i = 0; i < PARTITION_COUNT; i++)
                {
                    for (int j = 0; j < 4; j++)
                    {
                        //Initialize phasors to 2 so that it doubles the amplitudes on copy and rotation
                        phasors[i, j] = new Complex(2f, 0f);
                    }
                }

                //Clear output cache
                Array.Clear(cachedSampleBuffer, 0, cachedSampleBuffer.Length);
            }
        }

        private int ReadBody(float[] buffer, int offset, int count)
        {
            lock (_bufferLock)
            {
                int samplesWritten = Math.Max(0, Math.Min(count, _bufferCount - _bufferIndex));

                for (int i = 0; i < samplesWritten; i++)
                {
                    buffer[offset + i] = cachedSampleBuffer[_bufferIndex + i];
                }

                _bufferIndex += samplesWritten;

                return samplesWritten;
            }
        }

        public delegate float WindowFunction(int index, int width);

        public static readonly WindowFunction Hamming =
            (index, width) => (float)(0.54 - 0.46 * Math.Cos((2 * Math.PI * index) / (width - 1)));

        private Complex GetPhasor(int freqSample, float speed) =>
            GetRotator(-freqSample * 2 * (float)Math.PI * (1 - speed) / speed);

        private Complex GetRotator(float theta) => new Complex((float)Math.Cos(theta), (float)Math.Sin(theta));

        #endregion Helper Methods
    }
}
