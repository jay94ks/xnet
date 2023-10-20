using Microsoft.Extensions.DependencyInjection;
using System;

namespace XnetInternals.Internals
{
    internal class XnetConnectionManager : IXnetConnectionManager
    {
        private static readonly Xnet[] EMPTY = new Xnet[0];

        private readonly IXnetServerManager m_Server;
        private readonly IXnetClientManager m_Client;

        /// <summary>
        /// Initialize a new <see cref="XnetConnectionManager"/> instance.
        /// </summary>
        /// <param name="Server"></param>
        /// <param name="Client"></param>
        public XnetConnectionManager(IServiceProvider Services)
        {
            m_Server = Services.GetService<IXnetServerManager>();
            m_Client = Services.GetService<IXnetClientManager>();
        }

        /// <inheritdoc/>
        public Xnet[] FindAll(Func<Xnet, bool> Predicate)
        {
            var Servers = m_Server != null ? m_Server.FindAll(Predicate) : EMPTY;
            var Clients = m_Client != null ? m_Client.FindAll(Predicate) : EMPTY;
            return Servers.Concat(Clients).ToArray();
        }

        /// <inheritdoc/>
        public Xnet Find(Func<Xnet, bool> Predicate)
        {
            Xnet Xnet = null;
            if (m_Server != null)
                Xnet = m_Server.Find(Predicate);

            if (Xnet is null && m_Client != null)
                Xnet = m_Client.Find(Predicate);

            return Xnet;
        }

        /// <inheritdoc/>
        public Xnet FindLast(Func<Xnet, bool> Predicate)
        {
            Xnet Xnet = null;
            if (m_Client != null)
                Xnet = m_Client.FindLast(Predicate);

            if (Xnet is null && m_Server != null)
                Xnet = m_Server.FindLast(Predicate);

            return Xnet;
        }

        /// <inheritdoc/>
        public Xnet[] Snapshot()
        {
            var Servers = m_Server != null ? m_Server.Snapshot() : EMPTY;
            var Clients = m_Client != null ? m_Client.Snapshot() : EMPTY;
            return Servers.Concat(Clients).ToArray();
        }
    }
}
