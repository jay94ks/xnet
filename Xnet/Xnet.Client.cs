using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using XnetInternals.Impls;

public sealed partial class Xnet
{
    /// <summary>
    /// Client options.
    /// </summary>
    public sealed class ClientOptions : Options
    {
        /// <summary>
        /// Allow `Retry` when failed to connect.
        /// </summary>
        public bool AllowRetry { get; set; } = true;

        /// <summary>
        /// Allow `Recovery` when connection lost.
        /// </summary>
        public bool AllowRecovery { get; set; } = true;

        /// <summary>
        /// Delay to retry if failed to connect.
        /// </summary>
        public TimeSpan DelayToRetry { get; set; } = TimeSpan.FromSeconds(1);

        /// <summary>
        /// Delay to recovery if connection lost.
        /// </summary>
        public TimeSpan DelayToRecovery { get; set; } = TimeSpan.FromSeconds(1);

        /// <summary>
        /// Options for SSL.
        /// If set, SSL will be enabled.
        /// </summary>
        public Action<SslClientAuthenticationOptions> SslOptions { get; set; }

        /// <inheritdoc/>
        internal override bool IsSslRequired => SslOptions != null;

        /// <inheritdoc/>
        internal override Task OnSslAuthentication(SslStream Stream)
        {
            var Options = new SslClientAuthenticationOptions()
            {
                EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13,
                EncryptionPolicy = EncryptionPolicy.RequireEncryption,
                RemoteCertificateValidationCallback = SslAllowAllRemoteCertificates,
                CertificateRevocationCheckMode = X509RevocationMode.NoCheck,
                AllowRenegotiation = true,
            };

            SslOptions?.Invoke(Options);
            return Stream.AuthenticateAsClientAsync(Options);
        }

        /// <inheritdoc/>
        internal override void ValidateAndThrow()
        {
            base.ValidateAndThrow();

            if (AllowRetry && DelayToRetry.TotalMilliseconds <= 0)
                throw new InvalidOperationException("`DelayToRetry` can not be zero or less.");

            if (AllowRecovery && DelayToRecovery.TotalMilliseconds <= 0)
                throw new InvalidOperationException("`DelayToReconnect` can not be zero or less.");
        }
    }

    /// <summary>
    /// Run the Xnet client asynchronously.
    /// And return true if connected at least once.
    /// </summary>
    /// <param name="Services"></param>
    /// <param name="Options"></param>
    /// <param name="Token"></param>
    /// <returns></returns>
    public static async Task<bool> Client(IServiceProvider Services, ClientOptions Options, CancellationToken Token = default)
    {
        Options.ValidateAndThrow();
        var Impls = new CachedImpls(Options);
        var Success = false;

        while (Token.IsCancellationRequested == false)
        {
            var Socket = new Socket(Options.Endpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            try { await Socket.ConnectAsync(Options.Endpoint, Token); }
            catch
            {
                try { Socket.Close(); } catch { }
                try { Socket.Dispose(); } catch { }

                // --> retry to connect.
                if (Options.AllowRetry == false)
                    break;

                try { await Task.Delay(Options.DelayToRetry, Token); }
                catch
                {
                }

                continue;
            }

            Success = true;
            await RunAsync(Services, Socket, Impls, Options, Token);

            // --> retry to connect.
            if (Options.AllowRecovery == false)
                break;

            try { await Task.Delay(Options.DelayToRecovery, Token); }
            catch
            {
            }
        }

        return Success;
    }
}