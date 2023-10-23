using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XnetBuckets.Impls.Packets;

namespace XnetBuckets.Impls
{
    /// <summary>
    /// Bucket subject.
    /// </summary>
    internal class BucketSubject
    {
        private readonly HashSet<Xnet> m_Consumers = new();
        private readonly HashSet<Bucket> m_Buckets = new();
        private readonly Guid m_BucketId;
        private long m_Counter = 0;

        /// <summary>
        /// Initialize a new <see cref="BucketSubject"/> instance.
        /// </summary>
        /// <param name="BucketId"></param>
        public BucketSubject(Guid BucketId) => m_BucketId = BucketId;

        /// <summary>
        /// Indicates whether this subject has buckets or not.
        /// </summary>
        public bool HasBuckets { get { lock (m_Buckets) return m_Buckets.Count > 0; } }

        /// <summary>
        /// Indicates whether this subject has buckets or consumers or not.
        /// </summary>
        public bool HasBucketsOrConsumers
            => Interlocked.CompareExchange(ref m_Counter, 0, 0) != 0;

        /// <summary>
        /// Take a snapshot.
        /// </summary>
        /// <returns></returns>
        private Xnet[] Snapshot()
        {
            lock (m_Consumers)
                return m_Consumers.ToArray();
        }

        /// <summary>
        /// Take a snapshot of buckets.
        /// </summary>
        /// <returns></returns>
        private Bucket[] BucketSnapshot()
        {
            lock (m_Buckets)
                return m_Buckets.ToArray();
        }

        /// <summary>
        /// Replicate bucket items to the connection.
        /// </summary>
        /// <param name="Connection"></param>
        /// <returns></returns>
        public async Task ReplicateAsync(Xnet Connection)
        {
            var Dups = new HashSet<Guid>();
            var Packet = new BKT_ITEM()
            {
                Ttl = 64,
                BucketId = m_BucketId
            };

            foreach (var Each in BucketSnapshot())
            {
                await Each.EachAsync(Entry =>
                {
                    if (Dups.Add(Entry.ItemId) == false)
                        return Task.CompletedTask;

                    Packet.ItemId = Entry.ItemId;
                    Packet.Data = Entry.Data;

                    return Connection.EmitAsync(Packet);
                }, Connection.Closing);
            }
        }

        /// <summary>
        /// Add a bucket.
        /// </summary>
        /// <param name="Bucket"></param>
        /// <returns></returns>
        public bool AddBucket(Bucket Bucket)
        {
            lock (m_Buckets)
            {
                if (m_Buckets.Add(Bucket))
                    return true;

                Interlocked.Increment(ref m_Counter);
                return false;
            }
        }

        /// <summary>
        /// Remove a bucket.
        /// </summary>
        /// <param name="Bucket"></param>
        /// <returns></returns>
        public bool RemoveBucket(Bucket Bucket)
        {
            lock (m_Buckets)
            {
                if (m_Buckets.Remove(Bucket))
                    return true;

                Interlocked.Increment(ref m_Counter);
                return false;
            }
        }

        /// <summary>
        /// Add a consumer to subject.
        /// </summary>
        /// <param name="Consumer"></param>
        /// <returns></returns>
        public bool AddConsumer(Xnet Consumer)
        {
            lock (m_Consumers)
            {
                if (m_Consumers.Add(Consumer) == false)
                    return false;

                BucketManager
                    .GetConsumingSet(Consumer)
                    .Add(m_BucketId);

                Interlocked.Increment(ref m_Counter);
                return true;
            }
        }

        /// <summary>
        /// Add a consumer to subject.
        /// </summary>
        /// <param name="Consumer"></param>
        /// <returns></returns>
        public bool RemoveConsumer(Xnet Consumer)
        {
            lock (m_Consumers)
            {
                if (m_Consumers.Remove(Consumer) == false)
                    return false;

                BucketManager
                    .GetConsumingSet(Consumer)
                    .Remove(m_BucketId);

                Interlocked.Increment(ref m_Counter);
                return true;
            }
        }

        /// <summary>
        /// Push an item to the subject.
        /// </summary>
        /// <param name="ItemId"></param>
        /// <param name="Data"></param>
        /// <returns></returns>
        public async Task<bool> PushItem(Guid ItemId, byte[] Data)
        {
            var Count = 0;

            foreach(var Each in BucketSnapshot())
            {
                if (await Each.PushFromNetworkAsync(ItemId, Data) == false)
                    Count++;
            }

            return Count <= 0;
        }

        /// <summary>
        /// Send a packet to bucket consumers.
        /// </summary>
        /// <param name="Packet"></param>
        /// <param name="Token"></param>
        /// <returns></returns>
        public async Task<bool> SendAsync(Xnet.Packet Packet, CancellationToken Token = default)
        {
            var Count = 0;
            foreach (var Each in Snapshot())
            {
                if (Token.IsCancellationRequested)
                    break;

                if (await Each.EmitAsync(Packet, Token))
                    Count++;
            }

            return Count > 0;
        }

        /// <summary>
        /// Send a packet to bucket consumers.
        /// </summary>
        /// <param name="Sender"></param>
        /// <param name="Packet"></param>
        /// <param name="Token"></param>
        /// <returns></returns>
        public async Task<bool> SendAsync(Xnet Sender, Xnet.Packet Packet, CancellationToken Token = default)
        {
            var Count = 0;
            foreach (var Each in Snapshot())
            {
                if (Token.IsCancellationRequested)
                    break;

                if (Sender == Each)
                    continue;

                if (await Each.EmitAsync(Packet, Token))
                    Count++;
            }

            return Count > 0;
        }
    }
}
