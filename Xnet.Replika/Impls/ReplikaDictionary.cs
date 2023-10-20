using System.Collections.Concurrent;
using XnetDsa;
using XnetReplika.Impls.Packets;

namespace XnetReplika.Impls
{
    /// <summary>
    /// Replika dictionary.
    /// </summary>
    internal class ReplikaDictionary : IReplikaDictionary
    {
        private readonly Dictionary<Guid, RPK_RECORD> m_Packets = new();
        private readonly HashSet<Xnet> m_Holders = new();
        private Guid[] m_CachedKeys = null;

        /// <summary>
        /// Initialize a new <see cref="ReplikaDictionary"/> instance.
        /// </summary>
        /// <param name="Manager"></param>
        /// <param name="Owner"></param>
        /// <param name="Key"></param>
        public ReplikaDictionary(ReplikaRepository Repository, DsaPubKey Owner, Guid Key)
        {
            this.Repository = Repository;
            this.Owner = Owner;
            this.Key = Key;
        }

        /// <summary>
        /// Repository.
        /// </summary>
        public ReplikaRepository Repository { get; }

        /// <summary>
        /// Replika Manager.
        /// </summary>
        public ReplikaManager Manager => Repository.Manager;

        /// <summary>
        /// Dictionary Key.
        /// </summary>
        public Guid Key { get; }

        /// <summary>
        /// Owner.
        /// </summary>
        public DsaPubKey Owner { get; }

        /// <summary>
        /// Indicates whether the local host has authority or not.
        /// </summary>
        public bool HasAuthority => Manager.PubKey == Owner;

        /// <inheritdoc/>
        public IEnumerable<Guid> Keys
        {
            get
            {
                lock (this)
                {
                    if (m_CachedKeys != null)
                        return m_CachedKeys;

                    return m_CachedKeys = m_Packets
                        .Where(X => X.Value.Value != null)
                        .Select(X => X.Key).ToArray();
                }
            }
        }

        /// <inheritdoc/>
        public event Action<IReplikaDictionary, Guid> Changed;

        /// <summary>
        /// Reset the dictionary.
        /// </summary>
        public void Reset()
        {
            lock (this)
                m_Packets.Clear();
        }

        /// <summary>
        /// Handle the <see cref="RPK_RECORD"/> packet.
        /// </summary>
        /// <param name="Packet"></param>
        /// <returns></returns>
        public bool Handle(RPK_RECORD Packet)
        {
            var Changed = false;
            try
            {
                lock (this)
                {
                    if (m_Packets.TryGetValue(Packet.ItemId, out var PrevPacket) == false)
                        m_CachedKeys = null;

                    // --> if timeline is not newer.
                    else if (PrevPacket.Timeline >= Packet.Timeline)
                        return false;

                    // --> store received packet here.
                    m_Packets[Packet.ItemId] = Packet;
                    return Changed = true;
                }
            }

            finally
            {
                if (Changed)
                    this.Changed?.Invoke(this, Packet.ItemId);
            }
        }

        /// <summary>
        /// Find last packet of the item id.
        /// </summary>
        /// <param name="ItemId"></param>
        /// <returns></returns>
        private RPK_RECORD FindLast(Guid ItemId)
        {
            lock (this)
            {
                if (m_Packets.TryGetValue(ItemId, out var PrevPacket) == true)
                    return PrevPacket;

                return null;
            }
        }

        /// <summary>
        /// Add a holder connection.
        /// </summary>
        /// <param name="Connection"></param>
        /// <returns></returns>
        public bool AddHolder(Xnet Connection)
        {
            lock (this)
            {
                return m_Holders.Add(Connection);
            }
        }

        /// <summary>
        /// Remove the holder connection.
        /// </summary>
        /// <param name="Connection"></param>
        /// <returns></returns>
        public bool RemoveHolder(Xnet Connection)
        {
            lock(this)
            {
                if (m_Holders.Remove(Connection) == false)
                    return false;

                if (m_Holders.Count > 0)
                    return false;

                return true;
            }
        }

        /// <summary>
        /// Replicate records to the connection.
        /// </summary>
        /// <param name="Connection"></param>
        /// <returns></returns>
        public async Task<bool> ReplicateAsync(Xnet Connection)
        {
            RPK_RECORD[] Records;

            lock(this)
            {
                Records = m_Packets.Values
                    .Where(X => X.Value != null)
                    .ToArray();
            }

            foreach(var Each in Records)
            {
                if (await Connection.EmitAsync(Each) == false)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Try to get the <paramref name="ItemId"/>.
        /// </summary>
        /// <param name="ItemId"></param>
        /// <param name="Record"></param>
        /// <returns></returns>
        public bool TryGet(Guid ItemId, out RPK_RECORD Record)
        {
            lock (this)
            {
                if (m_Packets.TryGetValue(ItemId, out Record) == true && Record != null)
                    return true;

                Record = null;
                return false;
            }
        }

        /// <inheritdoc/>
        bool IReplikaDictionary.TryGet(Guid Key, out byte[] Data)
        {
            if (TryGet(Key, out var Record))
            {
                Data = Record != null ? Record.Value : null;
                return true;
            }

            Data = null;
            return false;
        }

        /// <inheritdoc/>
        public bool Set(Guid ItemId, byte[] Value)
        {
            return SetAsync(ItemId, Value)
                .ConfigureAwait(false).GetAwaiter().GetResult();
        }

        /// <inheritdoc/>
        public async Task<bool> SetAsync(Guid ItemId, byte[] Value, CancellationToken Token = default)
        {
            if (Manager.Key.Validity == false)
                return false;

            if (Manager.PubKey != Owner)
                return false;

            var Last = FindLast(ItemId);
            while (Token.IsCancellationRequested == false)
            {
                var Timeline = (Last != null ? Last.Timeline : 0) + 1;
                var Packet = new RPK_RECORD
                {
                    Timeline = Timeline,
                    Key = this.Key,
                    ItemId = ItemId,
                    Value = Value
                };

                if (Packet.Sign(Manager.Key, Manager.PubKey) == false)
                    return false;

                if (Repository.Handle(null, Packet) == false)
                    continue;

                await Manager.BroadcastAsync(Packet, Token);
                return true;
            }

            return false;
        }
    }
}
