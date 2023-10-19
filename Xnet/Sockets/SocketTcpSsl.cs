using System.Net.Security;
using System.Net.Sockets;

namespace XnetInternals.Sockets
{
    /// <summary>
    /// Socket wrapper for SSL/TCP sockets.
    /// </summary>
    internal class SocketTcpSsl : SocketBase
    {
        private readonly SslStream m_Stream;
        private readonly Func<SslStream, Task> m_Authenticator;
        private readonly TaskCompletionSource m_Authentication;
        private bool m_Initiated = false;

        /// <summary>
        /// Initialize a new <see cref="SocketTcpSsl"/> instance.
        /// </summary>
        /// <param name="Socket"></param>
        /// <param name="Authenticator"></param>
        public SocketTcpSsl(Socket Socket, Func<SslStream, Task> Authenticator)
        {
            try
            {
                Socket.NoDelay = true;
                Socket.Blocking = false;
            }
            catch { }

            var Network = new NetworkStream(Socket, true);
            m_Authenticator = Authenticator;
            m_Authentication = new TaskCompletionSource();
            m_Stream = new SslStream(Network, false);
        }

        /// <summary>
        /// Authenticate <see cref="SslStream"/> asynchronously.
        /// </summary>
        /// <returns></returns>
        private async Task AuthenticateAsync()
        {
            var InitiateNow = false;
            lock(this)
            {
                if (m_Initiated == false)
                {
                    m_Initiated = true;
                    InitiateNow = true;
                }

            }

            if (InitiateNow)
            {
                try
                {
                    await m_Authenticator.Invoke(m_Stream)
                        .ConfigureAwait(false);
                }

                catch { Dispose(); }
                finally
                {
                    m_Authentication.TrySetResult();
                }

                return;
            }

            await m_Authentication.Task.ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public override async ValueTask<int> ReceiveAsync(ArraySegment<byte> Buffer)
        {
            await AuthenticateAsync().ConfigureAwait(false);
            while (Closing.IsCancellationRequested == false)
            {
                var Length = 0;
                try { Length = await m_Stream.ReadAsync(Buffer, Closing); }
                catch (SocketException Error) when (CanRetry(Error)) { continue; }
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
            await AuthenticateAsync().ConfigureAwait(false);
            while (Closing.IsCancellationRequested == false)
            {
                try { await m_Stream.WriteAsync(Buffer, Closing); }
                catch (SocketException Error) when (CanRetry(Error)) { continue; }
                catch
                {
                    Dispose();
                    break;
                }

                return true;
            }

            return false;
        }

        /// <inheritdoc/>
        protected override void OnDispose()
        {
            try { m_Stream.Close(); } catch { }
            try { m_Stream.Dispose(); } catch { }
        }
    }
}
