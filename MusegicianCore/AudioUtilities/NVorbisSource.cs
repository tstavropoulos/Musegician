using System;
using System.IO;
using CSCore;
using NVorbis;

namespace Musegician.Sources
{
    /// <summary>
    /// Stolen shamelessly from CSCore Examples
    /// </summary>
    public sealed class NVorbisSource : ISampleSource
    {
        private readonly Stream _stream;
        private readonly VorbisReader _vorbisReader;
        private bool _disposed;

        public NVorbisSource(Stream stream)
        {
            if (stream == null)
            {
                throw new ArgumentNullException("stream");
            }

            if (!stream.CanRead)
            {
                throw new ArgumentException("Stream is not readable.", "stream");
            }

            _stream = stream;
            _vorbisReader = new VorbisReader(stream, false);
            WaveFormat = new WaveFormat(_vorbisReader.SampleRate, 32, _vorbisReader.Channels, AudioEncoding.IeeeFloat);
        }

        public bool CanSeek => _stream.CanSeek;

        public WaveFormat WaveFormat { get; }
        
        public long Length => CanSeek ? 
            (long)(_vorbisReader.TotalTime.TotalSeconds * WaveFormat.SampleRate * WaveFormat.Channels) : 0;
        
        public long Position
        {
            get => CanSeek ? (long)(_vorbisReader.DecodedTime.TotalSeconds * _vorbisReader.SampleRate * _vorbisReader.Channels) : 0;
            set
            {
                if (!CanSeek)
                {
                    throw new InvalidOperationException("NVorbisSource is not seekable.");
                }

                if (value < 0 || value > Length)
                {
                    throw new ArgumentOutOfRangeException("value");
                }

                _vorbisReader.DecodedTime = TimeSpan.FromSeconds((double)value / _vorbisReader.SampleRate / _vorbisReader.Channels);
            }
        }

        public int Read(float[] buffer, int offset, int count) =>
            _vorbisReader.ReadSamples(buffer, offset, count);

        public void Dispose()
        {
            if (!_disposed)
            {
                _vorbisReader.Dispose();
            }

            _disposed = true;
        }
    }
}
