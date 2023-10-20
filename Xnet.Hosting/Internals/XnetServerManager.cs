namespace XnetInternals.Internals
{
    internal class XnetServerManager : IXnetServerManager, Xnet.ConnectionExtender
    {
        private readonly HashSet<Xnet> m_Connections = new();

        /// <inheritdoc/>
        public Xnet[] Snapshot()
        {
            lock (m_Connections)
                return m_Connections.ToArray();
        }

        /// <inheritdoc/>
        public Xnet[] FindAll(Func<Xnet, bool> Predicate)
        {
            lock (m_Connections)
                return m_Connections.Where(Predicate).ToArray();
        }

        /// <inheritdoc/>
        public Xnet Find(Func<Xnet, bool> Predicate)
        {
            lock (m_Connections)
                return m_Connections.FirstOrDefault(Predicate);
        }

        /// <inheritdoc/>
        public Xnet FindLast(Func<Xnet, bool> Predicate)
        {
            lock (m_Connections)
                return m_Connections.LastOrDefault(Predicate);
        }

        /// <inheritdoc/>
        public ValueTask OnConnectedAsync(Xnet Connection)
        {
            lock (m_Connections) m_Connections.Add(Connection);
            return ValueTask.CompletedTask;
        }

        /// <inheritdoc/>
        public ValueTask OnDisconnectedAsync(Xnet Connection)
        {
            lock (m_Connections) m_Connections.Remove(Connection);
            return ValueTask.CompletedTask;
        }

    }
}

