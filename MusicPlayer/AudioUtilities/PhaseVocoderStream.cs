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

        private readonly int _halfFFTSamples;
        private readonly int _baseFFTSamples;
        private readonly int _expandedFFTSamples;

        private readonly int _channels;

        private readonly int _stepSize;
        private readonly int _overlap;

        private Complex[] phasors;
        private Complex[][] inputBuffers;
        private Complex[] fftBuffer;
        private Complex[] ifftBuffer;
        private float[] outputAccumulation;

        private float _speed = 1f;


        private const int BASE_FFT_SIZE = 12;
        private const int EXPANDED_FFT_SIZE = BASE_FFT_SIZE + 1;
        private const int OVERLAP_FACTOR = 32;

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

        private bool _enabled = true;
        public bool Enabled
        {
            get => _enabled;
            set
            {
                if (_enabled != value)
                {
                    if (value)
                    {
                        //Clear the buffers when we reenable it
                        ClearBuffers();
                    }

                    _enabled = value;
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

            _baseFFTSamples = (int)Math.Pow(2, BASE_FFT_SIZE);
            _expandedFFTSamples = 2 * _baseFFTSamples;
            _halfFFTSamples = _baseFFTSamples / 2;

            _stepSize = _baseFFTSamples / OVERLAP_FACTOR;
            _overlap = _baseFFTSamples - _stepSize;

            localSampleBuffer = new float[_channels * _stepSize];
            cachedSampleBuffer = new float[2 * _channels * _stepSize];

            phasors = new Complex[_halfFFTSamples + 1];
            inputBuffers = new Complex[_channels][];
            outputAccumulation = new float[_channels * _expandedFFTSamples];

            fftBuffer = new Complex[_baseFFTSamples];
            ifftBuffer = new Complex[_expandedFFTSamples];

            for (int i = 0; i < _channels; i++)
            {
                inputBuffers[i] = new Complex[_baseFFTSamples];
            }

            for (int j = 0; j <= _halfFFTSamples; j++)
            {
                //Initialize phasors to 2 so that it doubles the amplitudes on copy and rotation
                phasors[j] = new Complex(2f, 0f);
            }
        }

        public static PhaseVocoderStream CreatePhaseVocodedStream(ISampleSource source) => new PhaseVocoderStream(source);


        #endregion Constructor
        #region SampleAggregatorBase Overrides

        public override int Read(float[] buffer, int offset, int count)
        {
            if (!Enabled)
            {
                //Passthrough when disabled
                return base.Read(buffer, offset, count);
            }

            //Copy samples left from prior pulls
            int samplesWritten = ReadBody(buffer, offset, count);

            while (samplesWritten < count)
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
                    //We have used all of our cachedSamples, so clear them
                    Array.Clear(cachedSampleBuffer, 0, cachedSampleBuffer.Length);

                    int outputSamples = (int)(_baseFFTSamples / Speed);
                    int realStep = outputSamples / OVERLAP_FACTOR;

                    float effectiveSpeed = _baseFFTSamples / (float)outputSamples;
                    _bufferIndex = 0;
                    _bufferCount = _channels * realStep;

                    for (int channel = 0; channel < _channels; channel++)
                    {
                        //Slide input samples over
                        Array.Copy(
                            sourceArray: inputBuffers[channel],
                            sourceIndex: _stepSize,
                            destinationArray: inputBuffers[channel],
                            destinationIndex: 0,
                            length: _overlap);

                        //Copy new samples into buffer
                        for (int i = 0; i < localSampleBuffer.Length / _channels; i++)
                        {
                            inputBuffers[channel][_overlap + i].Real = localSampleBuffer[_channels * i + channel];
                            inputBuffers[channel][i].Imaginary = 0f;
                        }

                        //Copy and Window into fftbuffer
                        for (int i = 0; i < _baseFFTSamples; i++)
                        {
                            fftBuffer[i].Real = inputBuffers[channel][i].Real * Hamming(i, _baseFFTSamples);
                            fftBuffer[i].Imaginary = 0f;
                        }

                        //FFT
                        FastFourierTransformation.Fft(fftBuffer, BASE_FFT_SIZE, FftMode.Forward);

                        //Clear IFFT Buffer
                        Array.Clear(ifftBuffer, 0, ifftBuffer.Length);

                        //Copy values into IFFT Buffer
                        for (int i = 0; i <= _halfFFTSamples; i++)
                        {
                            ifftBuffer[2 * i] = fftBuffer[i].Times(phasors[i]);
                        }

                        //IFFT
                        FastFourierTransformation.Fft(ifftBuffer, EXPANDED_FFT_SIZE, FftMode.Backward);

                        //Accumualte the window samples
                        //for (int i = 0; i < outputSamples; i++)
                        for (int i = 0; i < _expandedFFTSamples; i++)
                        {
                            //outputAccumulation[_channels * i + channel] += Hamming(i, _expandedFFTSamples) * ifftBuffer[i].Real;
                            //outputAccumulation[_channels * i + channel] += Hamming(i, outputSamples) * ifftBuffer[i].Real;
                            outputAccumulation[_channels * i + channel] += Hamming(i, _expandedFFTSamples) * ifftBuffer[i].Real;
                            //outputAccumulation[_channels * i + channel] += ifftBuffer[i].Real;
                        }
                    }

                    //Advance phasor
                    for (int i = 0; i <= _halfFFTSamples; i++)
                    {
                        //phasors[i] = phasors[i].Times(GetPhasor(i, effectiveSpeed));
                        phasors[i] = phasors[i].Times(GetPhasor2(i, realStep - _stepSize));
                    }

                    //Copy output samples to output buffer
                    Array.Copy(
                        sourceArray: outputAccumulation,
                        destinationArray: cachedSampleBuffer,
                        length: _bufferCount);

                    //Slide down output accumulation
                    Array.Copy(
                        sourceArray: outputAccumulation,
                        sourceIndex: _bufferCount,
                        destinationArray: outputAccumulation,
                        destinationIndex: 0,
                        length: outputAccumulation.Length - _bufferCount);

                    //Clear empty output accumulation region
                    Array.Clear(outputAccumulation, outputAccumulation.Length - _bufferCount, _bufferCount);
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
                ClearBuffers();
                base.Position = Math.Min((long)Math.Floor(value * Speed), base.Length);
            }
        }

        public override long Length => (long)Math.Ceiling(base.Length / Speed);

        #endregion SampleAggregatorBase Overrides
        #region Helper Methods

        private void ClearBuffers()
        {
            lock (_bufferLock)
            {
                //Reset Phasors
                for (int i = 0; i < _channels; i++)
                {
                    Array.Clear(inputBuffers[i], 0, _baseFFTSamples);
                }

                for (int j = 0; j <= _halfFFTSamples; j++)
                {
                    //Initialize phasors to 2 so that it doubles the amplitudes on copy and rotation
                    phasors[j] = new Complex(2f, 0f);
                }

                //Clear output cache
                Array.Clear(outputAccumulation, 0, outputAccumulation.Length);
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

        //private Complex GetPhasor(int freqSample, float speed) =>
        //    GetRotator(-freqSample * 2 * (float)Math.PI * (1 - speed) / (speed * OVERLAP_FACTOR));
        private Complex GetPhasor2(int freqSample, int extraSamples) =>
            GetRotator(-freqSample * 2f * (float)Math.PI * extraSamples / OVERLAP_FACTOR);

        private Complex GetRotator(float theta) => new Complex((float)Math.Cos(theta), (float)Math.Sin(theta));

        #endregion Helper Methods
    }
}
