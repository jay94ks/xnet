using System.Text;
using XnetBuckets.Impls;
using XnetBuckets.Impls.Packets;

namespace XnetBuckets
{
    /// <summary>
    /// Base class for buckets.
    /// </summary>
    public abstract class Bucket : IDisposable
    {
        private static readonly AsyncLocal<Bucket> CURRENT = new();

        private static Bucket Current => CURRENT.Value;

        /// <summary>
        /// Set current bucket and return previous.
        /// </summary>
        /// <param name="Bucket"></param>
        /// <returns></returns>
        private static Bucket SetCurrent(Bucket Bucket)
        {
            var Prev = CURRENT.Value;
            CURRENT.Value = Bucket;
            return Prev;
        }

        /// <summary>
        /// Make the bucket id from type.
        /// This refers <see cref="Type"/>'s name value (not FullName).
        /// </summary>
        /// <param name="ItemType"></param>
        /// <returns></returns>
        public static Guid MakeBucketId(Type ItemType, string BucketName)
            => BKT_ITEM.MakeItemId(Encoding.UTF8.GetBytes($"BucketId, {BucketName}#{ItemType.Name}"));

        // --

        private readonly List<BucketItemEntry> m_Entries = new();
        private readonly SemaphoreSlim m_Semaphore = new(1);

        private readonly IBucketManager m_BucketManager;
        private TaskCompletionSource<BucketSubject> m_SubjectTcs;
        private BucketSubject m_Subject;

        /// <summary>
        /// Initialize a new <see cref="Bucket"/> instance.
        /// </summary>
        private Bucket(IBucketManager BucketManager, Guid BucketId)
        {
            m_BucketManager = BucketManager ?? throw new ArgumentNullException(nameof(BucketManager));
            m_SubjectTcs = new TaskCompletionSource<BucketSubject>();
            this.BucketId = BucketId;
        }

        /// <summary>
        /// Initialize a new <see cref="Bucket"/> instance.
        /// </summary>
        public Bucket(IBucketManager BucketManager, Type ItemType, string BucketName)
            : this(BucketManager, MakeBucketId(ItemType, BucketName)) { }

        /// <inheritdoc/>
        public void Dispose()
        {
            try { m_Semaphore.Dispose(); } catch { }
            lock (this)
            {
                if (m_Subject is null)
                    return;
            }

            Deactivate();
            OnDispose();
        }

        /// <summary>
        /// Called to dispose internal objects.
        /// </summary>
        protected virtual void OnDispose() { }

        /// <summary>
        /// Bucket Id.
        /// </summary>
        internal Guid BucketId { get; }

        /// <summary>
        /// Get count of entries.
        /// </summary>
        public int Count => Invoke(() => m_Entries.Count);

        /// <summary>
        /// Called when an item is pushed to the bucket.
        /// </summary>
        public event Action<Bucket, IBucketItem> OnPush;

        /// <summary>
        /// Called when an item is pushed from the network to the bucket.
        /// </summary>
        public event Action<Bucket, IBucketItem> OnNetworkPush;

        /// <summary>
        /// Element accessor.
        /// </summary>
        /// <param name="Index"></param>
        /// <returns></returns>
        public IBucketItem this[int Index] => Invoke(() =>
        {
            if (m_Entries.Count <= Index)
                return default;

            return GetItemFromEntry(m_Entries[Index]);
        });

        /// <summary>
        /// Fetch N elements and make them to be consumed.
        /// </summary>
        /// <param name="Count"></param>
        /// <returns></returns>
        public IBucketItem Fetch() => Invoke(() =>
        {
            if (m_Entries.Count <= 0)
                return default;

            var Entry = m_Entries[0];
            m_Entries.RemoveAt(0);

            return GetItemFromEntry(Entry);
        });

        /// <summary>
        /// Fetch N elements and make them to be consumed.
        /// </summary>
        /// <param name="Count"></param>
        /// <returns></returns>
        public IBucketItem[] FetchMulti(int Count) => Invoke(() =>
        {
            if ((Count = Math.Min(Count, m_Entries.Count)) <= 0)
                return Array.Empty<IBucketItem>();

            var Items = m_Entries.Take(Count).ToArray();
            m_Entries.RemoveAll(Items.Contains);

            return Items.Select(GetItemFromEntry).ToArray();
        });

        /// <summary>
        /// Get elements in range.
        /// </summary>
        /// <param name="Offset"></param>
        /// <param name="Count"></param>
        /// <returns></returns>
        public IBucketItem[] Range(int Offset, int Count) => Invoke(() =>
        {
            if (Offset < 0)
            {
                Count += Offset;
                Offset = 0;
            }

            if (m_Entries.Count <= 0 || Offset >= m_Entries.Count)
                return Array.Empty<IBucketItem>();

            if ((Count = Math.Min(Count, m_Entries.Count - Offset)) <= 0)
                return Array.Empty<IBucketItem>();

            return m_Entries
                .Skip(Offset).Take(Count)
                .Select(GetItemFromEntry)
                .ToArray();
        });

        /// <summary>
        /// Get elements by predicate.
        /// </summary>
        /// <param name="Predicate"></param>
        /// <param name="Offset"></param>
        /// <param name="Count"></param>
        /// <returns></returns>
        public IBucketItem[] FindAll(Func<IBucketItem, bool> Predicate, int Offset, int Count) => Invoke(() =>
        {
            return m_Entries
                .Select(GetItemFromEntry)
                .Where(Predicate).Skip(Offset).Take(Count)
                .ToArray();
        });

        /// <summary>
        /// Get an element by predicate.
        /// </summary>
        /// <param name="Predicate"></param>
        /// <returns></returns>
        public IBucketItem Find(Func<IBucketItem, bool> Predicate) => Invoke(() =>
        {
            return m_Entries
                .Select(GetItemFromEntry)
                .FirstOrDefault(Predicate);
        });

        /// <summary>
        /// Get an element by predicate.
        /// </summary>
        /// <param name="Predicate"></param>
        /// <returns></returns>
        public IBucketItem FindLast(Func<IBucketItem, bool> Predicate) => Invoke(() =>
        {
            return m_Entries
                .Select(GetItemFromEntry)
                .LastOrDefault(Predicate);
        });

        /// <summary>
        /// Activate the bucket asynchronously.
        /// </summary>
        /// <returns></returns>
        public ValueTask<bool> ActivateAsync() => m_BucketManager.ActivateAsync(this);

        /// <summary>
        /// Activate the bucket immediately.
        /// </summary>
        /// <returns></returns>
        public bool Activate() => ActivateAsync().ConfigureAwait(false).GetAwaiter().GetResult();

        /// <summary>
        /// Deactivate the bucket asynchronously.
        /// </summary>
        /// <returns></returns>
        public ValueTask<bool> DeactivateAsync() => m_BucketManager.DeactivateAsync(this);

        /// <summary>
        /// Deactivate the bucket immediately.
        /// </summary>
        /// <returns></returns>
        public bool Deactivate() => DeactivateAsync().ConfigureAwait(false).GetAwaiter().GetResult();

        /// <summary>
        /// Set the bucket subject instance.
        /// </summary>
        /// <param name="Subject"></param>
        internal void SetBucketSubject(BucketSubject Subject)
        {
            TaskCompletionSource<BucketSubject> Tcs;
            lock (this)
            {
                if (Subject == m_Subject)
                    return;

                m_Subject = Subject;

                if (Subject is null)
                {
                    if (m_SubjectTcs.Task.IsCompleted == false)
                        return;

                    m_SubjectTcs = new TaskCompletionSource<BucketSubject>();
                    return;
                }

                if (m_SubjectTcs.Task.IsCompleted == true)
                    m_SubjectTcs = new TaskCompletionSource<BucketSubject>();

                Tcs = m_SubjectTcs;
            }

            Tcs?.TrySetResult(Subject);
        }

        /// <summary>
        /// Get the bucket subject asynchronouly.
        /// </summary>
        /// <param name="Token"></param>
        /// <returns></returns>
        /// <exception cref="OperationCanceledException"></exception>
        internal async Task<BucketSubject> GetSubjectAsync(CancellationToken Token = default)
        {
            Token.ThrowIfCancellationRequested();

            var Tcs = new TaskCompletionSource();
            using(Token.Register(() => Tcs.TrySetResult(), false))
            {
                Task<BucketSubject> Subject;
                lock(this)
                    Subject = m_SubjectTcs.Task;

                await Task.WhenAny(Subject, Tcs.Task).ConfigureAwait(false);
                Token.ThrowIfCancellationRequested();
                return await Subject.ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Execute an action once per entry.
        /// </summary>
        /// <param name="Action"></param>
        /// <returns></returns>
        internal async Task EachAsync(Func<BucketItemEntry, Task> Action, CancellationToken Token = default)
        {
            var Dups = new HashSet<Guid>();
            while (Token.IsCancellationRequested == false)
            {
                BucketItemEntry Entry;
                lock (m_Entries)
                {
                    Entry = m_Entries.FirstOrDefault(
                        X => Dups.Contains(X.ItemId) == false);

                    if (Entry is null)
                        break;

                    Dups.Add(Entry.ItemId);
                }

                await Action.Invoke(Entry);
            }
        }

        /// <summary>
        /// Invoke an action with semaphore.
        /// </summary>
        /// <typeparam name="TReturn"></typeparam>
        /// <param name="Action"></param>
        /// <returns></returns>
        protected TReturn Invoke<TReturn>(Func<TReturn> Action) => Invoke(() => Task.FromResult(Action.Invoke()));

        /// <summary>
        /// Invoke an action with semaphore.
        /// </summary>
        /// <typeparam name="TReturn"></typeparam>
        /// <param name="Action"></param>
        /// <returns></returns>
        protected TReturn Invoke<TReturn>(Func<Task<TReturn>> Action)
        {
            return InvokeAsync(Action)
                .ConfigureAwait(false)
                .GetAwaiter().GetResult();
        }

        /// <summary>
        /// Invoke an action with semaphore.
        /// </summary>
        /// <typeparam name="TReturn"></typeparam>
        /// <param name="Action"></param>
        /// <returns></returns>
        protected async Task<TReturn> InvokeAsync<TReturn>(Func<Task<TReturn>> Action)
        {
            if (Current == this)
                return await Action.Invoke();

            try { await m_Semaphore.WaitAsync(); }
            catch
            {
                return default;
            }

            var Prev = SetCurrent(this);
            try { return await Action.Invoke(); ; }
            finally
            {
                SetCurrent(Prev);
                try { m_Semaphore.Release(); }
                catch
                {
                }
            }
        }

        /// <summary>
        /// Push an item to the bucket.
        /// This called to push remotely received items.
        /// Returns true if the specified item is added.
        /// </summary>
        /// <param name="ItemId"></param>
        /// <param name="Data"></param>
        internal Task<bool> PushFromNetworkAsync(Guid ItemId, byte[] Data) => InvokeAsync(() => PushFromNetworkImplAsync(ItemId, Data));

        /// <summary>
        /// Implementation of <see cref="PushFromNetworkAsync(Guid, byte[])"/> method.
        /// </summary>
        /// <param name="ItemId"></param>
        /// <param name="Data"></param>
        /// <returns></returns>
        private async Task<bool> PushFromNetworkImplAsync(Guid ItemId, byte[] Data)
        {
            BucketItemEntry Entry;

            // --> find an entry for the item.
            if ((Entry = m_Entries.Find(X => X.ItemId == ItemId)) != null)
                return false;

            Entry = new BucketItemEntry
            {
                ItemId = ItemId,
                Data = Data
            };

            // --> filter the item asynchronously.
            if (await OnBeforePush(Entry) == false)
                return false;

            try
            {
                m_Entries.Add(Entry);
                await OnAfterPush(Entry);
            }

            finally
            {
                RaiseNetworkPushEvent(Entry);
            }

            return true;
        }

        /// <summary>
        /// Create an item from existing entry.
        /// </summary>
        /// <param name="Data"></param>
        /// <returns></returns>
        private IBucketItem CreateItem(byte[] Data)
        {
            var Item = CreateItem();
            using var Stream = new MemoryStream(Data, false);
            using (var Reader = new BinaryReader(Stream, Encoding.UTF8, true))
                Item.Deserialize(Reader);

            return Item;
        }

        /// <summary>
        /// Get an item from the entry.
        /// </summary>
        /// <param name="Entry"></param>
        /// <returns></returns>
        protected IBucketItem GetItemFromEntry(BucketItemEntry Entry)
        {
            if (Entry.CachedItem != null)
                return Entry.CachedItem;

            return Entry.CachedItem 
                = CreateItem(Entry.Data);
        }

        /// <summary>
        /// Push an item to the bucket.
        /// </summary>
        /// <param name="Item"></param>
        /// <returns></returns>
        public bool Push(IBucketItem Item) => Invoke(() => PushAsync(Item));

        /// <summary>
        /// Push an item to the bucket.
        /// </summary>
        /// <param name="Item"></param>
        /// <returns></returns>
        /// <exception cref="OperationCanceledException"></exception>
        public async Task<bool> PushAsync(IBucketItem Item, CancellationToken Token = default)
        {
            if (CanSupport(Item) == false)
                return false;

            var Data = SerializeItem(Item);
            var ItemId = BKT_ITEM.MakeItemId(Data);

            var Subject = await GetSubjectAsync(Token);
            var Result = await InvokeAsync(async () =>
            {
                Token.ThrowIfCancellationRequested();
                BucketItemEntry Entry;

                // --> find an entry for the item.
                if ((Entry = m_Entries.Find(X => X.ItemId == ItemId)) != null)
                    return false;

                // --> filter the item asynchronously.
                if (await OnBeforePush(Entry) == false)
                    return false;

                Entry = new BucketItemEntry
                {
                    ItemId = ItemId,
                    Data = Data,
                };

                try
                {
                    m_Entries.Add(Entry);
                    await OnAfterPush(Entry);
                }

                finally
                {
                    RaisePushEvent(Entry);
                }

                return true;
            });

            if (Result)
            {
                var Packet = new BKT_ITEM
                {
                    Ttl = 64,
                    BucketId = BucketId,
                    ItemId = ItemId,
                    Data = Data,
                };

                await Subject.SendAsync(Packet);
            }

            return Result;
        }

        /// <summary>
        /// Raise the `<see cref="OnPush"/>` event.
        /// </summary>
        /// <param name="Entry"></param>
        internal virtual void RaisePushEvent(BucketItemEntry Entry)
        {
            OnPush?.Invoke(this, GetItemFromEntry(Entry));
        }

        /// <summary>
        /// Raise the `<see cref="OnNetworkPush"/>` event.
        /// </summary>
        /// <param name="Entry"></param>
        internal virtual void RaiseNetworkPushEvent(BucketItemEntry Entry)
        {
            OnNetworkPush?.Invoke(this, GetItemFromEntry(Entry));
        }

        /// <summary>
        /// Consume an item from the bucket.
        /// </summary>
        /// <param name="Item"></param>
        /// <returns></returns>
        public bool Consume(IBucketItem Item) => Invoke(() => ConsumeAsync(Item));

        /// <summary>
        /// Consume an item from the bucket.
        /// </summary>
        /// <param name="Item"></param>
        /// <param name="Token"></param>
        /// <returns></returns>
        public async Task<bool> ConsumeAsync(IBucketItem Item, CancellationToken Token = default)
        {
            Token.ThrowIfCancellationRequested();

            var ItemId = BKT_ITEM.MakeItemId(SerializeItem(Item));
            return await InvokeAsync(async () =>
            {
                Token.ThrowIfCancellationRequested();
                int Index = m_Entries.FindIndex(X => X.ItemId == ItemId);

                // --> find an entry for the item.
                if (Index < 0)
                    return false;

                m_Entries.RemoveAt(Index);
                return true;
            });
        }

        /// <summary>
        /// Serialize an item to byte array.
        /// </summary>
        /// <param name="Item"></param>
        /// <returns></returns>
        private static byte[] SerializeItem(IBucketItem Item)
        {
            using var Stream = new MemoryStream();
            using (var Writer = new BinaryWriter(Stream, Encoding.UTF8, true))
                Item.Serialize(Writer);

            return Stream.ToArray();
        }

        /// <summary>
        /// Called to create an empty item.
        /// </summary>
        /// <returns></returns>
        protected abstract IBucketItem CreateItem();

        /// <summary>
        /// Called to test the specified item is supported or not.
        /// </summary>
        /// <param name="Item"></param>
        /// <returns></returns>
        protected abstract bool CanSupport(IBucketItem Item);

        /// <summary>
        /// Called before the item is added,
        /// If this return false, the item will be discarded.
        /// </summary>
        /// <param name="Entry"></param>
        /// <returns></returns>
        protected abstract ValueTask<bool> OnBeforePush(BucketItemEntry Entry);

        /// <summary>
        /// Called after the item is added,
        /// </summary>
        /// <param name="Entry"></param>
        /// <returns></returns>
        protected abstract ValueTask OnAfterPush(BucketItemEntry Entry);
    }

    /// <summary>
    /// Bucket for <typeparamref name="TItem"/>. 
    /// </summary>
    /// <typeparam name="TItem"></typeparam>
    public class Bucket<TItem> : Bucket where TItem : IBucketItem, new()
    {
        /// <summary>
        /// Initialize a new <see cref="Bucket"/> instance.
        /// </summary>
        /// <param name="BucketManager"></param>
        /// <param name="BucketName"></param>
        public Bucket(IBucketManager BucketManager, string BucketName)
            : base(BucketManager, typeof(TItem), BucketName)
        {
        }

        /// <summary>
        /// Called when an item is pushed to the bucket.
        /// </summary>
        public new event Action<Bucket<TItem>, TItem> OnPush;

        /// <summary>
        /// Called when an item is pushed from the network to the bucket.
        /// </summary>
        public new event Action<Bucket<TItem>, TItem> OnNetworkPush;

        /// <inheritdoc/>
        protected override bool CanSupport(IBucketItem Item) => Item is TItem;

        /// <inheritdoc/>
        protected override IBucketItem CreateItem() => new TItem();

        /// <inheritdoc/>
        protected override ValueTask<bool> OnBeforePush(BucketItemEntry Entry) => ValueTask.FromResult(true);

        /// <inheritdoc/>
        protected override ValueTask OnAfterPush(BucketItemEntry Entry) => ValueTask.CompletedTask;

        /// <summary>
        /// Raise the `<see cref="OnPush"/>` event.
        /// </summary>
        /// <param name="Entry"></param>
        internal override void RaisePushEvent(BucketItemEntry Entry)
        {
            base.RaisePushEvent(Entry);
            OnPush?.Invoke(this, (TItem) GetItemFromEntry(Entry));
        }

        /// <summary>
        /// Raise the `<see cref="OnNetworkPush"/>` event.
        /// </summary>
        /// <param name="Entry"></param>
        internal override void RaiseNetworkPushEvent(BucketItemEntry Entry)
        {
            base.RaiseNetworkPushEvent(Entry);
            OnNetworkPush?.Invoke(this, (TItem) GetItemFromEntry(Entry));
        }
    }
}
