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
    public class SpatializerStream : SampleAggregatorBase
    {
        #region Data

        private float[] localSampleBuffer;

        private Complex[] bufferL;
        private Complex[] bufferR;

        private Complex[] overlapL;
        private Complex[] overlapR;

        private Complex[] fftBufferL;
        private Complex[] fftBufferR;

        private Complex[] leftSpeakerIRL;
        private Complex[] leftSpeakerIRR;

        private Complex[] rightSpeakerIRL;
        private Complex[] rightSpeakerIRR;

        private int _fftSamples;
        //2^9 = 512
        private const int FFT_EXP_MIN = 9;
        private int _fftSize = 12;
        private int _fftSizeBump = 1;

        private int _bufferCopied = 0;
        private int _samplesPerOverlap = 0;
        private int _overlapSize = 0;

        private bool _writingTail = false;

        private volatile object _bufferLock = new object();

        #endregion Data
        #region Constructor

        public SpatializerStream(ISampleSource source)
            : base(source)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }

            if (source.WaveFormat.Channels != 2)
            {
                throw new ArgumentException("Source must have 2 channels");
            }

            PrepareNewIRFs();

        }

        public static SpatializerStream CreateSpatializerStream(ISampleSource source)
        {
            return new SpatializerStream(source);
        }


        #endregion Constructor
        #region SampleAggregatorBase Overrides

        public override int Read(float[] buffer, int offset, int count)
        {
            if (!SpatializationManager.Instance.EnableSpatializer)
            {
                return base.Read(buffer, offset, count);
            }

            if (_writingTail)
            {
                return ReadTail(buffer, offset, count);
            }

            //Copy samples left from last convolution
            int samplesWritten = ReadBody(buffer, offset, count);

            while (count > samplesWritten)
            {
                int read = base.Read(localSampleBuffer, 0, 2 * _samplesPerOverlap);

                if (read <= 0)
                {
                    //Move to copying the tail
                    _writingTail = true;
                    _bufferCopied = 0;

                    samplesWritten += ReadTail(buffer, offset + samplesWritten, count - samplesWritten);
                    break;
                }

                for (int i = 0; i < (read / 2); i++)
                {
                    bufferL[i].Real = localSampleBuffer[2 * i];
                    bufferL[i].Imaginary = 0f;
                    bufferR[i].Real = localSampleBuffer[2 * i + 1];
                    bufferR[i].Imaginary = 0f;
                }

                //Probably hitting EOF?
                if (read < 2 * _samplesPerOverlap)
                {
                    Console.WriteLine("EOF, right?");
                    //Zero out the rest
                    for (int i = read / 2; i < _samplesPerOverlap; i++)
                    {
                        bufferL[i].Real = 0f;
                        bufferL[i].Imaginary = 0f;
                        bufferR[i].Real = 0f;
                        bufferR[i].Imaginary = 0f;
                    }
                }

                Convolve();

                samplesWritten += ReadBody(buffer, offset + samplesWritten, count - samplesWritten);
            }

            return samplesWritten;
        }

        public override long Position
        {
            get => base.Position;
            set
            {
                FlushBuffers();
                base.Position = Math.Min(value, base.Length);
            }
        }

        public override long Length => (base.Length + 2 * _overlapSize);

        #endregion SampleAggregatorBase Overrides
        #region Helper Methods

        public void PrepareNewIRFs()
        {
            lock(_bufferLock)
            {
                float[] leftIRF = SpatializationManager.Instance.GetIRF(
                    speaker: AudioChannel.Left,
                    channel: AudioChannel.Left);
                float[] rightIRF = SpatializationManager.Instance.GetIRF(
                    speaker: AudioChannel.Left,
                    channel: AudioChannel.Right);

                _fftSize = Math.Max(FFT_EXP_MIN, ToNextExponentOf2(leftIRF.Length) + _fftSizeBump);

                _fftSamples = (int)Math.Pow(2, _fftSize);

                _samplesPerOverlap = _fftSamples - leftIRF.Length;
                _overlapSize = leftIRF.Length - 1;

                _bufferCopied = _samplesPerOverlap;

                localSampleBuffer = new float[2 * _samplesPerOverlap];

                bufferL = new Complex[_samplesPerOverlap];
                bufferR = new Complex[_samplesPerOverlap];

                overlapL = new Complex[_overlapSize];
                overlapR = new Complex[_overlapSize];

                fftBufferL = new Complex[_fftSamples];
                fftBufferR = new Complex[_fftSamples];

                //Prepare left speaker
                leftSpeakerIRL = new Complex[_fftSamples];
                leftSpeakerIRR = new Complex[_fftSamples];

                for (int i = 0; i < leftIRF.Length; i++)
                {
                    leftSpeakerIRL[i] = new Complex(leftIRF[i]);
                    leftSpeakerIRR[i] = new Complex(rightIRF[i]);
                }

                FastFourierTransformation.Fft(leftSpeakerIRL, _fftSize);
                FastFourierTransformation.Fft(leftSpeakerIRR, _fftSize);

                //Prepare right speaker
                leftIRF = SpatializationManager.Instance.GetIRF(
                    speaker: AudioChannel.Right,
                    channel: AudioChannel.Left);
                rightIRF = SpatializationManager.Instance.GetIRF(
                    speaker: AudioChannel.Right,
                    channel: AudioChannel.Right);

                rightSpeakerIRL = new Complex[_fftSamples];
                rightSpeakerIRR = new Complex[_fftSamples];

                for (int i = 0; i < leftIRF.Length; i++)
                {
                    rightSpeakerIRL[i] = new Complex(leftIRF[i]);
                    rightSpeakerIRR[i] = new Complex(rightIRF[i]);
                }

                FastFourierTransformation.Fft(rightSpeakerIRL, _fftSize);
                FastFourierTransformation.Fft(rightSpeakerIRR, _fftSize);

                _writingTail = false;
            }
        }

        void FlushBuffers()
        {
            PrepareNewIRFs();
        }

        void Convolve()
        {
            //Copy initial samples
            for (int i = 0; i < _samplesPerOverlap; i++)
            {
                fftBufferL[i] = bufferL[i];
                fftBufferR[i] = bufferR[i];
            }

            //Zero out rest of buffer
            for (int i = _samplesPerOverlap; i < fftBufferL.Length; i++)
            {
                fftBufferL[i] = new Complex();
                fftBufferR[i] = new Complex();
            }

            FastFourierTransformation.Fft(fftBufferL, _fftSize);
            FastFourierTransformation.Fft(fftBufferR, _fftSize);

            Complex tempL, tempR;

            for (int i = 0; i < fftBufferL.Length; i++)
            {
                tempL = fftBufferL[i];
                tempR = fftBufferR[i];

                fftBufferL[i] = tempL.Times(leftSpeakerIRL[i]).Add(tempR.Times(rightSpeakerIRL[i]));
                fftBufferR[i] = tempL.Times(leftSpeakerIRR[i]).Add(tempR.Times(rightSpeakerIRR[i]));
            }

            FastFourierTransformation.Fft(fftBufferL, _fftSize, FftMode.Backward);
            FastFourierTransformation.Fft(fftBufferR, _fftSize, FftMode.Backward);

            for (int i = 0; i < _overlapSize; i++)
            {
                bufferL[i] = overlapL[i].Add(fftBufferL[i]);
                bufferR[i] = overlapR[i].Add(fftBufferR[i]);

                overlapL[i] = fftBufferL[_samplesPerOverlap + i];
                overlapR[i] = fftBufferR[_samplesPerOverlap + i];
            }

            for (int i = _overlapSize; i < _samplesPerOverlap; i++)
            {
                bufferL[i] = fftBufferL[i];
                bufferR[i] = fftBufferR[i];
            }

            _bufferCopied = 0;
        }

        private const float factor = 512f;


        private int ReadTail(float[] buffer, int offset, int count)
        {
            lock (_bufferLock)
            {
                int samplesWritten = Math.Max(0, Math.Min(count, 2 * (_overlapSize - _bufferCopied)));

                for (int i = 0; i < samplesWritten / 2; i++)
                {
                    buffer[offset + 2 * i] = factor * overlapL[_bufferCopied + i].Real;
                    buffer[offset + 2 * i + 1] = factor * overlapR[_bufferCopied + i].Real;
                }

                _bufferCopied += samplesWritten / 2;

                return samplesWritten;
            }
        }

        private int ReadBody(float[] buffer, int offset, int count)
        {
            lock (_bufferLock)
            {
                int samplesWritten = Math.Max(0, Math.Min(count, 2 * (_samplesPerOverlap - _bufferCopied)));

                for (int i = 0; i < samplesWritten / 2; i++)
                {
                    buffer[offset + 2 * i] = factor * bufferL[_bufferCopied + i].Real;
                    buffer[offset + 2 * i + 1] = factor * bufferR[_bufferCopied + i].Real;
                }

                _bufferCopied += samplesWritten / 2;

                return samplesWritten;
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

        public static float UnitaryClamp(float value)
        {
            return Math.Max(0, Math.Min(1, value));
        }

        #endregion Utility Methods
    }

    public static class ComplexExtension
    {
        public static Complex Times(this Complex A, Complex B)
        {
            return new Complex(
                real: A.Real * B.Real - A.Imaginary * B.Imaginary,
                img: A.Real * B.Imaginary + A.Imaginary * B.Real);
        }

        public static Complex Add(this Complex A, Complex B)
        {
            return new Complex(
                real: A.Real + B.Real,
                img: A.Imaginary + B.Imaginary);
        }
    }
}
