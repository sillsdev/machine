using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace SIL.Machine.Utils
{
    public class BoundedStream : Stream
    {
        private readonly Stream _innerStream;
        private readonly long _maxSize;
        private long _totalBytesProcessed;

        public BoundedStream(Stream innerStream, long maxSize)
        {
            _innerStream = innerStream ?? throw new ArgumentNullException(nameof(innerStream));
            _maxSize = maxSize < 0 ? throw new ArgumentOutOfRangeException(nameof(maxSize)) : maxSize;
        }

        public override bool CanRead => _innerStream.CanRead;
        public override bool CanSeek => _innerStream.CanSeek;
        public override bool CanWrite => _innerStream.CanWrite;
        public override long Length => _innerStream.Length;

        public override long Position
        {
            get => _innerStream.Position;
            set => _innerStream.Position = value;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int bytesRead = _innerStream.Read(buffer, offset, count);
            TrackAndValidate(bytesRead);
            return bytesRead;
        }

        public override async Task<int> ReadAsync(
            byte[] buffer,
            int offset,
            int count,
            CancellationToken cancellationToken
        )
        {
            int bytesRead = await _innerStream.ReadAsync(buffer, offset, count, cancellationToken);
            TrackAndValidate(bytesRead);
            return bytesRead;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            TrackAndValidate(count);
            _innerStream.Write(buffer, offset, count);
        }

        public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            TrackAndValidate(count);
            await _innerStream.WriteAsync(buffer, offset, count, cancellationToken);
        }

        public override void Flush() => _innerStream.Flush();

        public override Task FlushAsync(CancellationToken cancellationToken) =>
            _innerStream.FlushAsync(cancellationToken);

        public override long Seek(long offset, SeekOrigin origin) => _innerStream.Seek(offset, origin);

        public override void SetLength(long value)
        {
            if (value > _maxSize)
            {
                throw new IOException(
                    $"SetLength value of {value} bytes exceeds the maximum limit of {_maxSize} bytes."
                );
            }

            _innerStream.SetLength(value);
        }

        private void TrackAndValidate(int bytesProcessed)
        {
            _totalBytesProcessed += bytesProcessed;
            if (_totalBytesProcessed > _maxSize)
                throw new IOException($"Stream operation aborted. Exceeded maximum limit of {_maxSize} bytes.");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                _innerStream.Dispose();

            base.Dispose(disposing);
        }
    }
}
