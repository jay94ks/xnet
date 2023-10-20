using Microsoft.Extensions.Hosting;

namespace XnetInternals.Internals
{
    /// <summary>
    /// Server service.
    /// </summary>
    internal class XnetServerService : BackgroundService
    {
        private readonly IServiceProvider m_Services;
        private readonly Xnet.ServerOptions m_Options;

        /// <summary>
        /// Initialize a new <see cref="XnetServerService"/> instance.
        /// </summary>
        /// <param name="Services"></param>
        /// <param name="Options"></param>
        public XnetServerService(IServiceProvider Services, Xnet.ServerOptions Options)
        {
            m_Services = Services;
            m_Options = Options;
        }

        /// <inheritdoc/>
        protected override Task ExecuteAsync(CancellationToken Token)
        {
            return Xnet.Server(m_Services, m_Options, Token);
        }
    }
}
