using Microsoft.Extensions.DependencyInjection;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using XnetInternals.Impls;

public sealed partial class Xnet
{
    /// <summary>
    /// Server options.
    /// </summary>
    public sealed class ServerOptions : Options
    {
        /// <summary>
        /// Options for SSL.
        /// If set, SSL will be enabled.
        /// </summary>
        public Action<SslServerAuthenticationOptions> SslOptions { get; set; }

        /// <inheritdoc/>
        internal override bool IsSslRequired => SslOptions != null;

        /// <inheritdoc/>
        internal override Task OnSslAuthentication(SslStream Stream)
        {
            var Options = new SslServerAuthenticationOptions()
            {
                EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13,
                EncryptionPolicy = EncryptionPolicy.RequireEncryption,
                RemoteCertificateValidationCallback = SslAllowAllRemoteCertificates,
                CertificateRevocationCheckMode = X509RevocationMode.NoCheck,
                ClientCertificateRequired = false,
                AllowRenegotiation = true,
            };

            SslOptions?.Invoke(Options);
            return Stream.AuthenticateAsServerAsync(Options);
        }
    }

    /// <summary>
    /// Run the Xnet server asynchronously.
    /// </summary>
    /// <param name="Services"></param>
    /// <param name="Options"></param>
    /// <param name="Token"></param>
    /// <returns></returns>
    public static async Task Server(IServiceProvider Services, ServerOptions Options, CancellationToken Token = default)
    {
        Options.ValidateAndThrow();
        var Impls = new CachedImpls(Options);
        var Listener = new TcpListener(Options.Endpoint);

        using var Cts = CancellationTokenSource.CreateLinkedTokenSource(Token);
        Listener.Start();
        try
        {
            while (Cts.Token.IsCancellationRequested == false)
            {
                Socket Socket = null;

                try { Socket = await Listener.AcceptSocketAsync(Cts.Token); }
                catch
                {
                }

                if (Socket is null)
                    continue;

                _ = RunAsync(Services, Socket, Impls, Options, Token);
            }
        }

        finally
        {
            Cts.Cancel();
            Listener.Stop();
        }

    }
}