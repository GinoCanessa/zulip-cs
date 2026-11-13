using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace zulip_cs_lib
{
    /// <summary>Provides read access to a caller-owned stream without transferring ownership to HTTP content.</summary>
    internal sealed class NonDisposingStream : Stream
    {
        /// <summary>The caller-owned stream.</summary>
        private readonly Stream _stream;

        /// <summary>Initializes a non-owning stream view.</summary>
        /// <param name="stream">The readable caller-owned stream.</param>
        internal NonDisposingStream(Stream stream) => _stream = stream;

        /// <summary>Gets whether the underlying stream is readable.</summary>
        public override bool CanRead => _stream.CanRead;

        /// <summary>Gets whether the underlying stream is seekable.</summary>
        public override bool CanSeek => _stream.CanSeek;

        /// <summary>Gets whether this read-only view is writable.</summary>
        public override bool CanWrite => false;

        /// <summary>Gets the underlying stream length when available.</summary>
        public override long Length => _stream.Length;

        /// <summary>Gets or sets the underlying stream position.</summary>
        public override long Position { get => _stream.Position; set => _stream.Position = value; }

        /// <summary>Reads bytes from the underlying stream.</summary>
        /// <param name="buffer">The destination buffer.</param>
        /// <param name="offset">The destination offset.</param>
        /// <param name="count">The maximum byte count.</param>
        /// <returns>The number of bytes read.</returns>
        public override int Read(byte[] buffer, int offset, int count) => _stream.Read(buffer, offset, count);

        /// <summary>Reads bytes without buffering the entire upload.</summary>
        /// <param name="buffer">The destination buffer.</param>
        /// <param name="cancellationToken">The upload cancellation token.</param>
        /// <returns>The number of bytes read.</returns>
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) =>
            _stream.ReadAsync(buffer, cancellationToken);

        /// <summary>Reads bytes using the array-based stream API.</summary>
        /// <param name="buffer">The destination buffer.</param>
        /// <param name="offset">The destination offset.</param>
        /// <param name="count">The maximum byte count.</param>
        /// <param name="cancellationToken">The upload cancellation token.</param>
        /// <returns>The number of bytes read.</returns>
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
            _stream.ReadAsync(buffer, offset, count, cancellationToken);

        /// <summary>Seeks the underlying stream when supported.</summary>
        /// <param name="offset">The seek offset.</param>
        /// <param name="origin">The seek origin.</param>
        /// <returns>The resulting position.</returns>
        public override long Seek(long offset, SeekOrigin origin) => _stream.Seek(offset, origin);

        /// <summary>Does not flush the read-only caller stream.</summary>
        public override void Flush() { }

        /// <summary>Rejects writes to the read-only view.</summary>
        /// <param name="buffer">The source buffer.</param>
        /// <param name="offset">The source offset.</param>
        /// <param name="count">The byte count.</param>
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        /// <summary>Rejects changes to the caller stream length.</summary>
        /// <param name="value">The requested length.</param>
        public override void SetLength(long value) => throw new NotSupportedException();

        /// <summary>Disposes this view, never its underlying caller-owned stream.</summary>
        /// <param name="disposing">Whether disposal was explicitly requested.</param>
        protected override void Dispose(bool disposing) => base.Dispose(disposing);
    }
}
