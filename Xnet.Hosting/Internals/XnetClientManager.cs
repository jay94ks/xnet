namespace XnetInternals.Internals
{
    /// <summary>
    /// Service to manage multiple clients if needed.
    /// </summary>
    internal class XnetClientManager : IXnetClientManager, Xnet.ConnectionExtender
    {
        private readonly CancellationTokenSource m_TokenSource = new();
        private readonly List<Xnet.ClientOptions> m_ClientOptions = new();

        private IServiceProvider m_Services;
        private bool m_Triggered = false;

        private readonly HashSet<Xnet> m_Connections = new();

        /// <summary>
        /// Initialize a new <see cref="XnetClientManager"/> instance.
        /// </summary>
        public XnetClientManager()
        {
            Token = m_TokenSource.Token;
        }

        /// <summary>
        /// Token.
        /// </summary>
        public CancellationToken Token { get; }

        /// <summary>
        /// Trigger the token.
        /// </summary>
        public void Trigger()
        {
            lock (m_Connections)
            {
                if (m_Triggered)
                    return;

                m_Triggered = true;
            }

            m_TokenSource.Cancel();
            m_TokenSource.Dispose();
        }

        /// <summary>
        /// Set the service provider.
        /// </summary>
        /// <param name="Services"></param>
        public void SetServices(IServiceProvider Services)
        {
            Xnet.ClientOptions[] Options = null;
            lock (m_Connections)
            {
                m_Services = Services;

                if (m_ClientOptions.Count <= 0)
                    return;

                Options = m_ClientOptions.ToArray();
                m_ClientOptions.Clear();
            }

            if (Token.IsCancellationRequested)
                return;

            foreach (var Each in Options)
            {
                if (Token.IsCancellationRequested)
                    break;

                Push(Each);
            }
        }

        /// <summary>
        /// Push an option to run.
        /// </summary>
        /// <param name="Options"></param>
        public void Push(Xnet.ClientOptions Options)
        {
            Options.Extenders.Add(this);

            lock (m_Connections)
            {
                if (m_Services is null)
                {
                    m_ClientOptions.Add(Options);
                    return;
                }
            }

            _ = RunClient(Options);
        }

        /// <summary>
        /// Run the client.
        /// </summary>
        /// <param name="Options"></param>
        /// <returns></returns>
        private Task RunClient(Xnet.ClientOptions Options)
        {
            return Xnet.Client(m_Services, Options, Token);
        }

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

        /// <summary>
        /// Add client option to connect.
        /// </summary>
        /// <param name="Options"></param>
        public bool Add(Xnet.ClientOptions Options)
        {
            // --> these two options shouldn't be added dynamically.
            //   : if these are needed, it should be configured through service collection.

            Options.AllowRetry = false;
            Options.AllowRecovery = false;

            lock (m_Connections)
            {
                var TargetEndpoint = Options.Endpoint.ToString();
                foreach(var Each in m_Connections)
                {
                    var Endpoint = Each.RemoteEndpoint.ToString();
                    if (Endpoint == TargetEndpoint)
                        return false;
                }

                foreach(var Each in m_ClientOptions)
                {
                    var Endpoint = Each.Endpoint.ToString();
                    if (Endpoint == TargetEndpoint)
                        return false;
                }

                if (m_Services is null)
                {
                    m_ClientOptions.Add(Options);
                    return true;
                }
            }

            _ = RunClient(Options);
            return true;
        }
    }
}

