using System.Net.Sockets;

namespace XnetInternals.Sockets
{
    /// <summary>
    /// Socket wrapper for TCP sockets.
    /// </summary>
    internal class SocketTcp : SocketBase
    {
        private readonly Socket m_Socket;

        /// <summary>
        /// Initialize a new <see cref="SocketTcp"/> instance.
        /// </summary>
        /// <param name="Socket"></param>
        public SocketTcp(Socket Socket)
        {
            m_Socket = Socket;
            try
            {
                Socket.NoDelay = true;
                Socket.Blocking = false;
            }
            catch { }
        }

        /// <inheritdoc/>
        protected override void OnDispose()
        {
            try { m_Socket.Close(); } catch { }
            try { m_Socket.Dispose(); } catch { }
        }

        /// <inheritdoc/>
        public override async ValueTask<int> ReceiveAsync(ArraySegment<byte> Buffer)
        {
            while (Closing.IsCancellationRequested == false)
            {
                var Length = 0;
                try { Length = await m_Socket.ReceiveAsync(Buffer, SocketFlags.None, Closing); }
                catch(SocketException Error) when(CanRetry(Error)) { continue; }
                catch
                {
                }

                if (Length <= 0)
                    Dispose();

                return Length;
            }

            return 0;
        }

        /// <inheritdoc/>
        protected override async ValueTask<bool> SendInternalAsync(ArraySegment<byte> Buffer)
        {
            while (Closing.IsCancellationRequested == false && Buffer.Count > 0)
            {
                var Length = 0;
                var Slice = new ArraySegment<byte>(
                    Buffer.Array, Buffer.Offset, Math.Min(2048, Buffer.Count));

                try { Length = await m_Socket.SendAsync(Slice, SocketFlags.None, Closing); }
                catch (SocketException Error) when (CanRetry(Error)) { continue; }
                catch
                {
                }

                if (Length <= 0)
                {
                    Dispose();
                    break;
                }

                Buffer = new ArraySegment<byte>(Buffer.Array,
                    Buffer.Offset + Length, Buffer.Count - Length);
            }

            return Buffer.Count <= 0;
        }
    }
}
