using XnetBuckets.Impls.Packets;

namespace XnetBuckets.Impls
{
    /// <summary>
    /// Bucket manager.
    /// </summary>
    internal class BucketManager : Xnet.BasicPacketProvider<BucketManager>, Xnet.ConnectionExtender, IBucketManager
    {
        private static readonly object KEY_CACHE = new();
        private static readonly object KEY_CONSET = new();
        private readonly Dictionary<Guid, BucketSubject> m_Subjects = new();
        private readonly HashSet<Xnet> m_Connections = new();

        /// <summary>
        /// Get the <see cref="BucketManager"/> instance.
        /// </summary>
        /// <param name="Xnet"></param>
        /// <returns></returns>
        public static BucketManager Get(Xnet Xnet)
        {
            if (Xnet.Items.TryGetValue(KEY_CACHE, out var Temp))
                return Temp as BucketManager;

            Xnet.Items.TryAdd(KEY_CACHE, Xnet.GetExtender<BucketManager>());
            return Get(Xnet);
        }

        /// <summary>
        /// Get the <see cref="BucketConsumingSet"/> instance.
        /// </summary>
        /// <param name="Xnet"></param>
        /// <returns></returns>
        public static BucketConsumingSet GetConsumingSet(Xnet Xnet)
        {
            if (Xnet.Items.TryGetValue(KEY_CONSET, out var Temp))
                return Temp as BucketConsumingSet;

            Xnet.Items.TryAdd(KEY_CONSET, new BucketConsumingSet());
            return GetConsumingSet(Xnet);
        }

        /// <inheritdoc/>
        protected override void MapTypes()
        {
            MapFrom(
                typeof(BKT_START),
                typeof(BKT_ITEM));
        }

        /// <summary>
        /// Get the bucket subject.
        /// </summary>
        /// <returns></returns>
        private BucketSubject GetSubject(Guid BucketId, bool AllowNew = false)
        {
            lock (m_Subjects)
            {
                if (m_Subjects.TryGetValue(BucketId, out var Subject) == false && AllowNew)
                    m_Subjects[BucketId] = Subject = new BucketSubject(BucketId);

                return Subject;
            }
        }

        /// <summary>
        /// Get active subjects.
        /// </summary>
        /// <returns></returns>
        private Guid[] GetActiveSubjects()
        {
            lock (m_Subjects)
                return m_Subjects.Keys.ToArray();
        }

        /// <summary>
        /// Broadcast a packet to all connections.
        /// </summary>
        /// <param name="Packet"></param>
        /// <param name="Token"></param>
        /// <returns></returns>
        private async Task BroadcastAsync(Xnet.Packet Packet, CancellationToken Token = default)
        {
            var Set = new HashSet<Xnet>();
            while (Token.IsCancellationRequested == false)
            {
                Xnet Xnet;

                lock (m_Connections)
                {
                    Xnet = m_Connections.FirstOrDefault(
                        X => Set.Contains(X) == false);

                    if (Xnet is null)
                        break;

                    Set.Add(Xnet);
                }

                await Xnet.EmitAsync(Packet);
            }
        }

        /// <inheritdoc/>
        public async ValueTask<bool> ActivateAsync(Bucket Bucket)
        {
            var Subject = GetSubject(Bucket.BucketId, true);
            lock (m_Subjects)
            {
                var HadBuckets = Subject.HasBuckets;
                if (Subject.AddBucket(Bucket) == false)
                    return false;

                Bucket.SetBucketSubject(Subject);
                if (HadBuckets)
                    return true;
            }

            var Packet = new BKT_START
            {
                BucketId = Bucket.BucketId,
            };

            await BroadcastAsync(Packet);
            return true;
        }

        /// <inheritdoc/>
        public ValueTask<bool> DeactivateAsync(Bucket Bucket)
        {
            var Subject = GetSubject(Bucket.BucketId, true);
            lock (m_Subjects)
            {
                if (Subject.RemoveBucket(Bucket) == false)
                    return new ValueTask<bool>(false);

                Bucket.SetBucketSubject(null);
                if (Subject.HasBucketsOrConsumers)
                    return new ValueTask<bool>(true);

                m_Subjects.Remove(Bucket.BucketId);
                return new ValueTask<bool>(true);
            }
        }

        /// <inheritdoc/>
        public async ValueTask OnConnectedAsync(Xnet Connection)
        {
            lock (m_Connections)
                m_Connections.Add(Connection);

            foreach (var Each in GetActiveSubjects())
            {
                var Packet = new BKT_START
                {
                    BucketId = Each,
                };

                lock (m_Subjects)
                {
                    var Subject = GetSubject(Each, false);
                    if (Subject is null || Subject.HasBuckets == false)
                        continue;
                }

                if (await Connection.EmitAsync(Packet) == false)
                    break;
            }
        }

        /// <inheritdoc/>
        public ValueTask OnDisconnectedAsync(Xnet Connection)
        {
            lock (m_Connections)
                m_Connections.Remove(Connection);

            foreach (var BucketId in GetConsumingSet(Connection))
            {
                var Subject = GetSubject(BucketId, false);
                if (Subject != null)
                {
                    Subject.RemoveConsumer(Connection);

                    lock (m_Subjects)
                    {
                        if (Subject.HasBucketsOrConsumers)
                            continue;

                        m_Subjects.Remove(BucketId);
                    }
                }
            }

            return ValueTask.CompletedTask;
        }

        /// <summary>
        /// Called when <see cref="BKT_START"/> packet received.
        /// </summary>
        /// <param name="Connection"></param>
        /// <param name="Packet"></param>
        /// <returns></returns>
        public async Task BucketStart(Xnet Connection, BKT_START Packet)
        {
            var Subject = GetSubject(Packet.BucketId, true);
            if (Subject.AddConsumer(Connection))
            {
                await Subject.ReplicateAsync(Connection);
            }
        }

        /// <summary>
        /// Called when <see cref="BKT_ITEM"/> packet received.
        /// </summary>
        /// <param name="Connection"></param>
        /// <param name="Packet"></param>
        /// <returns></returns>
        public async Task BucketItem(Xnet Connection, BKT_ITEM Packet)
        {
            var ItemId = BKT_ITEM.MakeItemId(Packet.Data);
            if (ItemId != Packet.ItemId)
                return;

            var Subject = GetSubject(Packet.BucketId, false);
            if (Subject is null)
                return;

            if (await Subject.PushItem(Packet.ItemId, Packet.Data) == false)
                return;

            if (Packet.Ttl <= 0 || --Packet.Ttl <= 0)
                return;

            // --> send item to all consumers.
            await Subject.SendAsync(Connection, Packet);
        }
    }
}
