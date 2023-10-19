using System.Buffers;
using System.Net.Sockets;

namespace XnetInternals.Sockets
{
    /// <summary>
    /// Base class for socket wrappers.
    /// </summary>
    internal abstract class SocketBase : IDisposable
    {
        private readonly CancellationTokenSource m_Closing;
        private readonly SemaphoreSlim m_Semaphore;
        private bool m_Disposed;

        private byte[] m_Buffer;
        private int m_BufferSize;

        /// <summary>
        /// Initialize a new <see cref="SocketBase"/> instance.
        /// </summary>
        public SocketBase()
        {
            Closing = (m_Closing = new()).Token;
            m_Semaphore = new SemaphoreSlim(1);

            m_Buffer = null;
            m_BufferSize = 0;
        }

        /// <summary>
        /// Triggered when the underlying connection is closing.
        /// </summary>
        public CancellationToken Closing { get; }

        /// <summary>
        /// Test whether the <see cref="SocketException"/> is retryable or not.
        /// </summary>
        /// <param name="Error"></param>
        /// <returns></returns>
        protected static bool CanRetry(SocketException Error)
        {
            switch (Error.SocketErrorCode)
            {
                case SocketError.Interrupted:
                case SocketError.WouldBlock:
                case SocketError.IOPending:
                case SocketError.InProgress:
                case SocketError.AlreadyInProgress:
                    return true;

                default:
                    return false;
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            lock (this)
            {
                if (m_Disposed)
                    return;

                m_Disposed = true;
            }

            m_Closing.Cancel();
            OnDispose();

            try { m_Semaphore.Dispose(); } catch { }
            m_Closing.Dispose();
        }

        /// <summary>
        /// Called to dispose internal objects.
        /// </summary>
        protected abstract void OnDispose();

        /// <summary>
        /// Receive bytes from the remote host.
        /// </summary>
        /// <param name="Buffer"></param>
        /// <returns></returns>
        public abstract ValueTask<int> ReceiveAsync(ArraySegment<byte> Buffer);

        /// <summary>
        /// Send bytes to the remote host.
        /// </summary>
        /// <param name="Buffer"></param>
        /// <returns></returns>
        public async ValueTask<bool> SendAsync(ArraySegment<byte> Buffer, CancellationToken Token = default)
        {
            try { await m_Semaphore.WaitAsync(Token); }
            catch
            {
                return false;
            }

            try { return await SendInternalAsync(Buffer); }
            finally
            {
                try { m_Semaphore.Release(); }
                catch
                {
                }
            }
        }

        /// <summary>
        /// Send bytes to the remote host.
        /// </summary>
        /// <param name="Buffer"></param>
        /// <returns></returns>
        protected abstract ValueTask<bool> SendInternalAsync(ArraySegment<byte> Buffer);

        /// <summary>
        /// Receive bytes with buffering asynchronously.
        /// </summary>
        /// <param name="Length"></param>
        /// <returns></returns>
        public async ValueTask<byte[]> ReceiveAsync(int Length)
        {
            var Buffer = new ArraySegment<byte>(new byte[Length]);
            while (Closing.IsCancellationRequested == false && Buffer.Count > 0)
            {
                var Recv = ReadFromBuffer(Buffer);
                if (Recv > 0)
                {
                    Buffer = new ArraySegment<byte>(Buffer.Array,
                        Buffer.Offset + Recv, Buffer.Count - Recv);

                    continue;
                }

                await FillBufferAsync();
            }

            if (Buffer.Count > 0)
                return Array.Empty<byte>();

            return Buffer.Array;
        }

        /// <summary>
        /// Receive bytes and fill the buffer.
        /// </summary>
        /// <returns></returns>
        private async Task<bool> FillBufferAsync()
        {
            var Buffer = ArrayPool<byte>.Shared.Rent(4096);
            try
            {
                var Length = await ReceiveAsync(Buffer);
                if (Length <= 0)
                {
                    Dispose();
                    return false;
                }

                PushToBuffer(Buffer, Length);
                return true;
            }

            finally
            {
                ArrayPool<byte>.Shared.Return(Buffer);
            }
        }

        /// <summary>
        /// Push bytes to the buffer.
        /// </summary>
        /// <param name="Buffer"></param>
        /// <param name="Length"></param>
        private void PushToBuffer(byte[] Buffer, int Length)
        {
            if (m_Buffer == null)
            {
                new Span<byte>(Buffer, 0, Length)
                    .CopyTo(m_Buffer = new byte[Length]);
            }

            else
            {
                var Offset = m_Buffer.Length;
                Array.Resize(ref m_Buffer, m_Buffer.Length + Length);
                Array.Copy(Buffer, 0, m_Buffer, Offset, Length);
            }

            m_BufferSize += Length;
        }

        /// <summary>
        /// Read bytes from the buffer.
        /// </summary>
        /// <param name="Buffer"></param>
        /// <returns></returns>
        private int ReadFromBuffer(ArraySegment<byte> Buffer)
        {
            var Slice = Math.Min(Buffer.Count, m_BufferSize);
            if (Slice > 0)
            {
                // --> copy bytes to the buffer.
                new Span<byte>(m_Buffer, 0, Slice).CopyTo(Buffer);

                // --> then remove copied bytes.
                if ((m_BufferSize -= Slice) > 0)
                    m_Buffer = m_Buffer.Skip(Slice).ToArray();

                else
                    m_Buffer = null;
            }

            return Slice;
        }
    }
}
