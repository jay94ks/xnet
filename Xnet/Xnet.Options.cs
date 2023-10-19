using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

public sealed partial class Xnet
{
    /// <summary>
    /// Xnet options.
    /// </summary>
    public abstract class Options
    {
        /// <summary>
        /// Predefined callback to allow all remote certificates.
        /// </summary>
        /// <param name="Sender"></param>
        /// <param name="Cert"></param>
        /// <param name="Chain"></param>
        /// <param name="Error"></param>
        /// <returns></returns>
        internal static bool SslAllowAllRemoteCertificates(object Sender, X509Certificate Cert, X509Chain Chain, SslPolicyErrors Error) => true;

        /// <summary>
        /// Network Id to filter clients.
        /// </summary>
        public Guid NetworkId { get; set; }

        /// <summary>
        /// Protocol extenders.
        /// </summary>
        public HashSet<Extender> Extenders { get; } = new();

        /// <summary>
        /// Packet providers.
        /// </summary>
        public HashSet<PacketProvider> PacketProviders { get; } = new();

        /// <summary>
        /// Endpoint to connect or listen.
        /// </summary>
        public IPEndPoint Endpoint { get; set; }

        /// <summary>
        /// Called before the connection loop.
        /// </summary>
        public Func<Xnet, Task> BeforeConnectionLoop { get; set; }

        /// <summary>
        /// Called before the connection loop.
        /// </summary>
        public Func<Xnet, Task> AfterConnectionLoop { get; set; }

        /// <summary>
        /// Validate options and throw exception if invalid.
        /// </summary>
        /// <exception cref="InvalidOperationException"></exception>
        internal virtual void ValidateAndThrow()
        {
            if (Endpoint is null)
            {
                if (this is ServerOptions)
                    throw new InvalidOperationException("No endpoint specified to listen.");

                throw new InvalidOperationException("No endpoint specified to connect.");
            }

            if (PacketProviders.Count <= 0)
            {
                if (Extenders.FirstOrDefault(X => X is PacketProvider) != null)
                    return;

                throw new InvalidOperationException("No packet providers are configured.");
            }
        }

        /// <summary>
        /// Indicates whether the Xnet requires SSL or not.
        /// </summary>
        internal virtual bool IsSslRequired => false;

        /// <summary>
        /// Called to authenticate the <see cref="SslStream"/>.
        /// </summary>
        /// <param name="Stream"></param>
        /// <returns></returns>
        internal virtual Task OnSslAuthentication(SslStream Stream) => Task.CompletedTask;
    }
}