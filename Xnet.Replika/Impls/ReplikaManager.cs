using XnetDsa;
using XnetReplika.Impls.Packets;

namespace XnetReplika.Impls
{
    /// <summary>
    /// Replika Manager.
    /// </summary>
    internal class ReplikaManager : IReplikaManager, Xnet.Extender
    {
        private readonly HashSet<Xnet> m_Connections = new();
        private readonly ReplikaRepository m_Repository;

        /// <summary>
        /// Initialize a new <see cref="ReplikaManager"/> instance.
        /// </summary>
        public ReplikaManager(DsaKey Key, DsaPubKey PubKey)
        {
            this.Key = Key;
            this.PubKey = PubKey;

            m_Repository = new ReplikaRepository(this);
        }

        /// <summary>
        /// DSA key.
        /// </summary>
        public DsaKey Key { get; }

        /// <summary>
        /// DSA public key.
        /// </summary>
        public DsaPubKey PubKey { get; }

        /// <inheritdoc/>
        public IReplikaRepository Local => Get(PubKey);

        /// <inheritdoc/>
        public IReplikaRepository Overlay => m_Repository;

        /// <inheritdoc/>
        public IReplikaRepository Get(DsaPubKey OwnerKey)
        {
            return m_Repository.GetDictionarySet(OwnerKey);
        }

        /// <inheritdoc/>
        public IReplikaDictionary Get(DsaPubKey OwnerKey, Guid Key)
        {
            return m_Repository.GetDictionary(OwnerKey, Key);
        }

        /// <summary>
        /// Called when new <see cref="Xnet"/> connection attached.
        /// </summary>
        /// <param name="Connection"></param>
        /// <returns></returns>
        public async ValueTask OnConnectedAsync(Xnet Connection)
        {
            lock (m_Connections)
            {
                m_Connections.Add(Connection);
            }

            await m_Repository.ReplicateAsync(Connection);
        }

        /// <summary>
        /// Called when the <see cref="Xnet"/> connection detached.
        /// </summary>
        /// <param name="Connection"></param>
        /// <returns></returns>
        public ValueTask OnDisconnectedAsync(Xnet Connection)
        {
            lock (m_Connections)
            {
                m_Connections.Remove(Connection);
            }

            m_Repository.OnDisconnected(Connection);
            return ValueTask.CompletedTask;
        }

        /// <summary>
        /// Broadcast the packet to all connections.
        /// </summary>
        /// <param name="Packet"></param>
        /// <param name="Token"></param>
        /// <returns></returns>
        public async Task<int> BroadcastAsync(Xnet.Packet Packet, CancellationToken Token = default)
        {
            Xnet[] Conns = null;
            lock (this)
                Conns = m_Connections.ToArray();

            var Counter = 0;
            foreach (var Each in Conns)
            {
                if (Token.IsCancellationRequested == true)
                    break;

                if (await Each.EmitAsync(Packet, Token))
                    Counter++;
            }

            return Counter;
        }

        /// <summary>
        /// Broadcast the packet to all connections.
        /// </summary>
        /// <param name="Packet"></param>
        /// <param name="Token"></param>
        /// <returns></returns>
        public async Task<int> BroadcastAsync(Xnet Sender, Xnet.Packet Packet, CancellationToken Token = default)
        {
            Xnet[] Conns = null;
            lock (this)
                Conns = m_Connections.ToArray();

            var Counter = 0;
            foreach (var Each in Conns)
            {
                if (Token.IsCancellationRequested == true)
                    break;

                if (Each == Sender)
                    continue;

                if (await Each.EmitAsync(Packet, Token))
                    Counter++;
            }

            return Counter;
        }

        /// <summary>
        /// Called to handle <see cref="RPK_RECORD"/> packet.
        /// </summary>
        /// <param name="Sender"></param>
        /// <param name="Record"></param>
        /// <returns></returns>
        public async Task OnRecord(Xnet Sender, RPK_RECORD Record)
        {
            if (m_Repository.Handle(Sender, Record))
                await BroadcastAsync(Sender, Record);
        }
    }
}
