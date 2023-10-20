using Microsoft.Extensions.Hosting;

namespace XnetInternals.Internals
{
    /// <summary>
    /// Client service.
    /// </summary>
    internal class XnetClientService : BackgroundService
    {
        private readonly IServiceProvider m_Services;
        private readonly XnetClientManager m_Manager;

        /// <summary>
        /// Initialize a new <see cref="XnetServerService"/> instance.
        /// </summary>
        /// <param name="Services"></param>
        /// <param name="Options"></param>
        public XnetClientService(IServiceProvider Services, XnetClientManager Manager)
        {
            m_Services = Services;
            m_Manager = Manager;
        }

        /// <inheritdoc/>
        protected override async Task ExecuteAsync(CancellationToken Token)
        {
            var Tcs = new TaskCompletionSource();
            using var _1 = Token.Register(m_Manager.Trigger, false);
            using var _2 = m_Manager.Token.Register(Tcs.SetResult, false);

            m_Manager.SetServices(m_Services);
            await Tcs.Task.ConfigureAwait(false);
        }
    }

}
