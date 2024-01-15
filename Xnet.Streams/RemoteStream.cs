using XnetStreams.Internals;
using XnetStreams.Internals.Packets;

namespace XnetStreams
{
    /// <summary>
    /// Stream wrapper.
    /// </summary>
    public class RemoteStream : Stream
    {
        private readonly CancellationTokenSource m_Disposing;
        private readonly PKT_OPEN_RESULT m_Open;
        private readonly StreamExtender m_Extender;
        private readonly IDisposable m_Closing;
        private readonly IStreamStatisticsCapturer m_Capturer;
        private long m_Cursor;
        private bool m_Opened = true;
        /// <summary>
        /// Initialize a new <see cref="RemoteStream"/> instance.
        /// </summary>
        /// <param name="Xnet"></param>
        /// <param name="Open"></param>
        internal RemoteStream(Xnet Xnet, PKT_OPEN_RESULT Open, IStreamStatisticsCapturer Capturer, StreamOptions Options)
        {
            Connection = Xnet;
            Closing = (m_Disposing = new()).Token;

            m_Open = Open;

            m_Capturer = Capturer;
            m_Extender = StreamExtender.Get(Connection);

            m_Closing = Xnet.Closing.Register(Dispose, false);
        }

        /// <summary>
        /// Stream Id.
        /// </summary>
        internal Guid Id => m_Open.Id;

        /// <summary>
        /// Underlying connection.
        /// </summary>
        public Xnet Connection { get; }

        /// <summary>
        /// Triggered when the stream is closing anyway.
        /// </summary>
        public CancellationToken Closing { get; }

        /// <inheritdoc/>
        public override bool CanTimeout
            => m_Open.ReadTimeout >= 0
            || m_Open.WriteTimeout >= 0;

        /// <inheritdoc/>
        public override bool CanSeek => m_Open.CanSeek;

        /// <inheritdoc/>
        public override bool CanRead => m_Open.CanRead;

        /// <inheritdoc/>
        public override bool CanWrite => m_Open.CanWrite;

        /// <inheritdoc/>
        public override long Position
        {
            get => CanSeek ? m_Cursor : throw new NotSupportedException();
            set => SeekAsync(value, SeekOrigin.Begin).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        /// <inheritdoc/>
        public override int ReadTimeout
        {
            get => m_Open.ReadTimeout >= 0 ? m_Open.ReadTimeout : throw new NotSupportedException();
            set => SetReadTimeout(value).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        /// <inheritdoc/>
        public override int WriteTimeout
        {
            get => m_Open.WriteTimeout >= 0 ? m_Open.WriteTimeout : throw new NotSupportedException();
            set => SetWriteTimeout(value).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        /// <inheritdoc/>
        public override long Length => CanSeek ? Math.Max(m_Cursor, m_Open.Length) : throw new NotSupportedException();

        /// <summary>
        /// Emit a request packet to remote host and wait its result.
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="Packet"></param>
        /// <param name="Token"></param>
        /// <returns></returns>
        /// <exception cref="ObjectDisposedException"></exception>
        private async Task<TResult> RequestAsync<TResult>(PKT_BASE Packet, CancellationToken Token) where TResult : PKT_BASE_RESULT
        {
            using var Cts = CancellationTokenSource.CreateLinkedTokenSource(Closing, Token);
            try { return await m_Extender.RequestAsync<TResult>(Connection, Packet, Cts.Token); }
            catch (Exception Exception)
            {
                if (Exception is OperationCanceledException Oce)
                {
                    if (Oce.CancellationToken == Cts.Token)
                    {
                        Token.ThrowIfCancellationRequested();
                        throw new ObjectDisposedException(nameof(RemoteStream));
                    }
                }

                throw;
            }
        }

        /// <summary>
        /// Set the read-timeout asynchronously.
        /// </summary>
        /// <param name="Timeout"></param>
        /// <param name="Token"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="StreamException"></exception>
        private async ValueTask SetReadTimeout(int Timeout, CancellationToken Token = default)
        {
            var ReadTimeout = new PKT_READTIMEOUT
            {
                Id = Id,
                Timeout = Timeout,
            };

            var Result = await RequestAsync<PKT_READTIMEOUT_RESULT>(ReadTimeout, Token);
            if (Result is null)
                throw new InvalidOperationException("the remote connection is closed before completion.");

            if (Result.Status == StreamStatus.Ok)
            {
                m_Open.ReadTimeout = Result.Timeout;
                return;
            }

            throw new StreamException(Result.Status);
        }

        /// <summary>
        /// Set the write-timeout asynchronously.
        /// </summary>
        /// <param name="Timeout"></param>
        /// <param name="Token"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="StreamException"></exception>
        private async ValueTask SetWriteTimeout(int Timeout, CancellationToken Token = default)
        {
            var WriteTimeout = new PKT_WRITETIMEOUT
            {
                Id = Id,
                Timeout = Timeout,
            };

            var Result = await RequestAsync<PKT_WRITETIMEOUT_RESULT>(WriteTimeout, Token);
            if (Result is null)
                throw new InvalidOperationException("the remote connection is closed before completion.");

            if (Result.Status == StreamStatus.Ok)
            {
                m_Open.WriteTimeout = Result.Timeout;
                return;
            }

            throw new StreamException(Result.Status);
        }

        /// <summary>
        /// Seek to offset asynchronously.
        /// </summary>
        /// <param name="Token"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">the remote connection is not alive.</exception>
        /// <exception cref="InvalidOperationException">the remote connection is closed before completion.</exception>
        /// <exception cref="InvalidDataException">the dispatched result is not correct.</exception>
        /// <exception cref="OperationCanceledException">the token is triggered.</exception>
        /// <exception cref="ObjectDisposedException">the stream has been disposed.</exception>
        /// <exception cref="StreamException"></exception>
        public async ValueTask<long> TellAsync(CancellationToken Token = default)
        {
            var Tell = new PKT_TELL
            {
                Id = Id
            };

            var Result = await RequestAsync<PKT_TELL_RESULT>(Tell, Token);
            if (Result is null)
                throw new InvalidOperationException("the remote connection is closed before completion.");

            if (Result.Status == StreamStatus.Ok)
            {
                m_Cursor = Result.Cursor;
                return Result.Cursor;
            }

            throw new StreamException(Result.Status);
        }

        /// <summary>
        /// Seek to offset asynchronously.
        /// </summary>
        /// <param name="Offset"></param>
        /// <param name="Origin"></param>
        /// <param name="Token"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">the remote connection is not alive.</exception>
        /// <exception cref="InvalidOperationException">the remote connection is closed before completion.</exception>
        /// <exception cref="InvalidDataException">the dispatched result is not correct.</exception>
        /// <exception cref="OperationCanceledException">the token is triggered.</exception>
        /// <exception cref="StreamException"></exception>
        public async ValueTask<long> SeekAsync(long Offset, SeekOrigin Origin, CancellationToken Token = default)
        {
            var Seek = new PKT_SEEK
            {
                Id = Id,
                Origin = Origin,
                Cursor = Offset
            };

            var Result = await RequestAsync<PKT_SEEK_RESULT>(Seek, Token);
            if (Result is null)
                throw new InvalidOperationException("the remote connection is closed before completion.");

            if (Result.Status == StreamStatus.Ok)
            {
                m_Cursor = Result.Cursor;
                return Result.Cursor;
            }

            throw new StreamException(Result.Status);
        }

        /// <inheritdoc/>
        public override long Seek(long offset, SeekOrigin origin)
        {
            return SeekAsync(offset, origin)
                .ConfigureAwait(false)
                .GetAwaiter().GetResult();
        }

        /// <summary>
        /// Read bytes from the remote stream asynchronously.
        /// </summary>
        /// <param name="Size"></param>
        /// <param name="Token"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">the remote connection is not alive.</exception>
        /// <exception cref="InvalidOperationException">the remote connection is closed before completion.</exception>
        /// <exception cref="InvalidDataException">the dispatched result is not correct.</exception>
        /// <exception cref="OperationCanceledException">the token is triggered.</exception>
        /// <exception cref="StreamException"></exception>
        public async ValueTask<byte[]> ReadAsync(int Size, CancellationToken Token = default)
        {
            var Read = new PKT_READ
            {
                Id = Id,
                Size = Size
            };

            while (true)
            {
                var Result = await RequestAsync<PKT_READ_RESULT>(Read, Token);
                if (Result is null)
                    throw new InvalidOperationException("the remote connection is closed before completion.");

                if (Result.Status == StreamStatus.Ok)
                {
                    if (CanSeek == true)
                        m_Cursor += Result.Data.Length;

                    m_Capturer?.PushRx(Connection, Result.Data.Length / 1024.0);
                    return Result.Data;
                }

                throw new StreamException(Result.Status);
            }
        }

        /// <inheritdoc/>
        public override async Task<int> ReadAsync(byte[] Buffer, int Offset, int Count, CancellationToken Token)
        {
            var Read = 0;
            while (Count > 0)
            {
                var Slice = Math.Min(Count, ushort.MaxValue * 1024);
                if (Slice <= 0)
                    break;

                var Data = await ReadAsync(Slice, Token);
                if (Data is null || Data.Length <= 0)
                    break;

                Data.CopyTo(new Span<byte>(Buffer, Offset, Data.Length));
                Read += Data.Length;
                Offset += Data.Length;
                Count -= Data.Length;
            }

            return Read;
        }

        /// <inheritdoc/>
        public override async ValueTask<int> ReadAsync(Memory<byte> Buffer, CancellationToken Token = default)
        {
            var Read = 0;

            while (Buffer.Length > 0)
            {
                var Slice = Math.Min(Buffer.Length - Read, ushort.MaxValue * 1024);
                if (Slice <= 0)
                    break;

                var Data = await ReadAsync(Slice, Token);
                if (Data is null || Data.Length <= 0)
                    break;

                Data.CopyTo(Buffer.Slice(0, Data.Length));
                Buffer = Buffer.Slice(Data.Length);
                Read += Data.Length;
            }

            return Read;
        }

        /// <summary>
        /// Read bytes from the remote stream.
        /// </summary>
        /// <param name="Size"></param>
        /// <param name="Token"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">the remote connection is not alive.</exception>
        /// <exception cref="InvalidOperationException">the remote connection is closed before completion.</exception>
        /// <exception cref="InvalidDataException">the dispatched result is not correct.</exception>
        /// <exception cref="OperationCanceledException">the token is triggered.</exception>
        /// <exception cref="StreamException"></exception>
        public byte[] Read(int Size, CancellationToken Token = default)
        {
            return ReadAsync(Size, Token)
                .ConfigureAwait(false)
                .GetAwaiter().GetResult();
        }

        /// <inheritdoc/>
        public override int Read(byte[] buffer, int offset, int count)
        {
            return ReadAsync(new ArraySegment<byte>(buffer, offset, count))
                .ConfigureAwait(false).GetAwaiter().GetResult();
        }

        /// <inheritdoc/>
        public override int Read(Span<byte> Buffer)
        {
            var Read = 0;

            while (Buffer.Length > 0)
            {
                var Slice = Math.Min(Buffer.Length - Read, ushort.MaxValue * 1024);
                if (Slice <= 0)
                    break;

                var Data = this.Read(Slice);
                if (Data is null || Data.Length <= 0)
                    break;

                Data.CopyTo(Buffer.Slice(0, Data.Length));
                Buffer = Buffer.Slice(Data.Length);
                Read += Data.Length;
            }

            return Read;
        }

        /// <summary>
        /// Write bytes to the stream asynchronously.
        /// </summary>
        /// <param name="Data"></param>
        /// <param name="Token"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">the remote connection is not alive.</exception>
        /// <exception cref="InvalidOperationException">the remote connection is closed before completion.</exception>
        /// <exception cref="InvalidDataException">the dispatched result is not correct.</exception>
        /// <exception cref="OperationCanceledException">the token is triggered.</exception>
        /// <exception cref="StreamException"></exception>
        public async ValueTask<int> WriteOnceAsync(byte[] Data, CancellationToken Token = default)
        {
            if (Data.Length >= ushort.MaxValue * 1024)
                Array.Resize(ref Data, ushort.MaxValue * 1024);

            var Write = new PKT_WRITE
            {
                Id = Id,
                Data = Data
            };

            while (true)
            {
                var Result = await RequestAsync<PKT_WRITE_RESULT>(Write, Token);
                if (Result is null)
                    throw new InvalidOperationException("the remote connection is closed before completion.");

                if (Result.Status == StreamStatus.Ok)
                {
                    if (CanSeek == true)
                        m_Cursor += Result.Size;

                    m_Capturer?.PushTx(Connection, Result.Size / 1024.0);
                    return Result.Size;
                }

                throw new StreamException(Result.Status);
            }
        }

        /// <inheritdoc/>
        public override async Task WriteAsync(byte[] Buffer, int Offset, int Count, CancellationToken Token)
        {
            await WriteAsync(new ArraySegment<byte>(Buffer, Offset, Count), Token);
        }

        /// <inheritdoc/>
        public override async ValueTask WriteAsync(ReadOnlyMemory<byte> Buffer, CancellationToken Token = default)
        {
            var Data = Buffer.ToArray();
            while (Buffer.Length > 0)
            {
                var Slice = await WriteOnceAsync(Data, Token);
                if (Slice <= 0)
                    break;

                Buffer = Buffer.Slice(Slice);
            }
        }

        /// <inheritdoc/>
        public override void Write(ReadOnlySpan<byte> Buffer)
        {
            var Data = Buffer.ToArray();
            while (Buffer.Length > 0)
            {
                var Slice = WriteOnceAsync(Data)
                    .ConfigureAwait(false)
                    .GetAwaiter().GetResult();

                if (Slice <= 0)
                    break;

                Buffer = Buffer.Slice(Slice);
            }
        }

        /// <inheritdoc/>
        public override void Write(byte[] buffer, int offset, int count)
        {
            Write(new ArraySegment<byte>(buffer, offset, count));
        }

        /// <summary>
        /// Set the stream length asynchronously.
        /// </summary>
        /// <param name="Value"></param>
        /// <param name="Token"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">the remote connection is not alive.</exception>
        /// <exception cref="InvalidOperationException">the remote connection is closed before completion.</exception>
        /// <exception cref="InvalidDataException">the dispatched result is not correct.</exception>
        /// <exception cref="OperationCanceledException">the token is triggered.</exception>
        /// <exception cref="StreamException"></exception>
        public async ValueTask SetLengthAsync(long Value, CancellationToken Token = default)
        {
            var SetLength = new PKT_SETLENGTH
            {
                Id = Id,
                Length = Value
            };

            while (true)
            {
                var Result = await RequestAsync<PKT_SETLENGTH_RESULT>(SetLength, Token);
                if (Result is null)
                    throw new InvalidOperationException("the remote connection is closed before completion.");

                if (Result.Status == StreamStatus.Ok)
                {
                    m_Open.Length = Result.Length;
                    return;
                }

                throw new StreamException(Result.Status);
            }
        }

        /// <inheritdoc/>
        public override void SetLength(long Value) => SetLengthAsync(Value).ConfigureAwait(false).GetAwaiter().GetResult();

        /// <inheritdoc/>
        public override async Task FlushAsync(CancellationToken Token = default)
        {
            var Flush = new PKT_FLUSH
            {
                Id = Id,
            };

            try { await RequestAsync<PKT_FLUSH_RESULT>(Flush, Token); }
            catch
            {
            }
        }

        /// <inheritdoc/>
        public override void Flush() => FlushAsync().ConfigureAwait(false).GetAwaiter().GetResult();

        /// <inheritdoc/>
        public override async ValueTask DisposeAsync()
        {
            await CloseAsync(false);
            await base.DisposeAsync();
        }   

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            CloseAsync(false).ConfigureAwait(false).GetAwaiter().GetResult();
            base.Dispose(disposing);
        }

        /// <inheritdoc/>
        public override void Close()
        {
            CloseAsync(true).ConfigureAwait(false).GetAwaiter().GetResult();
            base.Close();
        }

        /// <inheritdoc/>
        public async ValueTask CloseAsync(bool FlushRequired = true, CancellationToken Token = default)
        {
            lock(this)
            {
                if (m_Opened == false)
                    return;

                m_Opened = false;
            }

            var Close = new PKT_CLOSE
            {
                Id = Id,
                TraceId = Guid.Empty,
                FlushRequired = FlushRequired,
            };

            try { await Connection.EmitAsync(Close, Token); }
            catch
            {
            }

            m_Disposing.Cancel();
            try { m_Closing?.Dispose(); }
            catch
            {
            }

            m_Disposing.Dispose();
        }
    }
}
