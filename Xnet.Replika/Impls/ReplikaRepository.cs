using XnetDsa;
using XnetReplika.Impls.Packets;

namespace XnetReplika.Impls
{
    /// <summary>
    /// Replika repository.
    /// </summary>
    internal class ReplikaRepository : IReplikaRepository
    {
        // --
        private readonly Dictionary<DsaPubKey, ReplikaDictionarySet> m_Dictionaries = new();
        private Guid[] m_CachedKeys;

        /// <summary>
        /// Initialize a new <see cref="ReplikaRepository"/> instance.
        /// </summary>
        /// <param name="Manager"></param>
        public ReplikaRepository(ReplikaManager Manager)
        {
            this.Manager = Manager;
        }

        /// <summary>
        /// Replika Manager.
        /// </summary>
        public ReplikaManager Manager { get; }

        /// <summary>
        /// Triggered when value changed.
        /// </summary>
        public event Action<IReplikaRepository, Guid> Changed;

        /// <summary>
        /// Get owner keys.
        /// </summary>
        /// <param name="Offset"></param>
        /// <param name="Count"></param>
        /// <returns></returns>
        public DsaPubKey[] GetOwnerKeys(int Offset = 0, int Count = 1024)
        {
            lock (m_Dictionaries)
            {
                return m_Dictionaries.Keys.Skip(Offset).Take(Count).ToArray();
            }
        }

        /// <summary>
        /// Get the dictionary.
        /// </summary>
        /// <param name="Owner"></param>
        /// <param name="Key"></param>
        /// <returns></returns>
        public ReplikaDictionarySet GetDictionarySet(DsaPubKey Owner)
        {
            lock (m_Dictionaries)
            {
                if (m_Dictionaries.TryGetValue(Owner, out var Set) == false)
                    m_Dictionaries[Owner] = Set = new ReplikaDictionarySet(this, Owner);

                return Set;
            }
        }

        /// <summary>
        /// Get the dictionary.
        /// </summary>
        /// <param name="Owner"></param>
        /// <param name="Key"></param>
        /// <returns></returns>
        public ReplikaDictionary GetDictionary(DsaPubKey Owner, Guid Key) => Get(Owner, Key, true);

        /// <summary>
        /// Get or new the replika dictionary instance.
        /// </summary>
        /// <param name="Key"></param>
        /// <returns></returns>
        private ReplikaDictionary Get(DsaPubKey Owner, Guid Key, bool AllowNew = true)
        {
            ReplikaDictionarySet Set;
            lock (m_Dictionaries)
            {
                if (m_Dictionaries.TryGetValue(Owner, out Set) == false)
                {
                    m_Dictionaries[Owner] = Set = new ReplikaDictionarySet(this, Owner);
                }
            }

            lock (Set)
            {
                if (Set.TryGetValue(Key, out var Instance) == false && AllowNew)
                {
                    Set[Key] = Instance = new ReplikaDictionary(this, Owner, Key);
                    m_CachedKeys = null;
                }

                return Instance;
            }
        }

        /// <inheritdoc/>
        public IEnumerable<Guid> Keys => GetKeys();

        /// <inheritdoc/>
        public IReplikaDictionary this[Guid Key] => new ReplikaOverlayDictionary(this, Key);

        /// <summary>
        /// Get all keys.
        /// </summary>
        /// <param name="Keys"></param>
        public Guid[] GetKeys()
        {
            lock (m_Dictionaries)
            {
                if (m_CachedKeys != null)
                    return m_CachedKeys;

                var HashSet = new HashSet<Guid>();
                foreach (var Each in m_Dictionaries.Values)
                {
                    lock (Each)
                    {
                        foreach (var Key in Each.Keys)
                            HashSet.Add(Key);
                    }
                }

                m_CachedKeys = HashSet.ToArray();
                return m_CachedKeys;
            }
        }

        /// <summary>
        /// Get all item keys.
        /// </summary>
        /// <param name="Key"></param>
        /// <returns></returns>
        public Guid[] GetItemKeys(Guid Key)
        {
            var HashSet = new HashSet<Guid>();
            var Dicts = new Queue<ReplikaDictionary>();

            lock (m_Dictionaries)
            {
                foreach (var Each in m_Dictionaries.Values)
                {
                    lock (Each)
                    {
                        foreach (var Dict in Dicts)
                            Dicts.Enqueue(Dict);
                    }
                }
            }

            while (Dicts.TryDequeue(out var Each))
            {
                foreach (var Item in Each.Keys)
                    HashSet.Add(Item);
            }

            return HashSet.ToArray();
        }

        /// <inheritdoc/>
        public bool TryGet(Guid Key, Guid ItemId, out byte[] Data)
        {
            var Queue = new Queue<ReplikaDictionary>();
            lock (m_Dictionaries)
            {
                foreach(var Each in m_Dictionaries.Values)
                {
                    lock (Each)
                    {
                        if (Each.TryGetValue(Key, out var Dict) == false)
                            continue;

                        Queue.Enqueue(Dict);
                    }
                }
            }

            RPK_RECORD Candidate = null;
            while (Queue.TryDequeue(out var Each))
            {
                if (Each.TryGet(ItemId, out var Record) == false)
                    continue;

                if (Candidate != null && Candidate.LocalTime >= Record.LocalTime)
                    continue;

                Candidate = Record;
            }

            Data = Candidate != null
                ? Candidate.Value
                : null;

            return Candidate != null;
        }

        /// <inheritdoc/>
        public bool Set(Guid Key, Guid ItemKey, byte[] Data)
            => SetAsync(Key, ItemKey, Data).ConfigureAwait(false).GetAwaiter().GetResult();

        /// <inheritdoc/>
        public Task<bool> SetAsync(Guid Key, Guid ItemId, byte[] Value, CancellationToken Token = default)
        {
            var Dict = Get(Manager.PubKey, Key, Value != null);
            if (Dict != null)
                return Dict.SetAsync(ItemId, Value, Token);

            return Task.FromResult(true);
        }

        /// <summary>
        /// Replicate the repository to the connection.
        /// </summary>
        /// <param name="Connection"></param>
        /// <returns></returns>
        public async Task<bool> ReplicateAsync(Xnet Connection)
        {
            ReplikaDictionarySet[] Sets;
            lock (m_Dictionaries)
            {
                if (m_Dictionaries.Count <= 0)
                    return false;

                Sets = m_Dictionaries.Values.ToArray();
            }

            var Success = true;
            foreach (var Each in Sets)
            {
                ReplikaDictionary[] Dicts;
                lock (Each)
                {
                    if (Each.Count <= 0)
                        continue;

                    Dicts = Each.Values.ToArray();
                }

                foreach(var Dict in Dicts)
                {
                    if ((Success = await Dict.ReplicateAsync(Connection)) == false)
                        break;
                }

                if (Success == false)
                    break;
            }

            return Success;
        }

        /// <summary>
        /// Called when the connection is closed.
        /// </summary>
        public void OnDisconnected(Xnet Connection)
        {
            ReplikaDictionarySet[] Sets;
            lock (m_Dictionaries)
            {
                if (m_Dictionaries.Count <= 0)
                    return;

                Sets = m_Dictionaries.Values.ToArray();
            }

            foreach (var Each in Sets)
            {
                lock (Each)
                {
                    if (Each.Count <= 0)
                        continue;

                    foreach (var Dict in Each.Values)
                    {
                        var Empty = Dict.RemoveHolder(Connection);
                        if (Dict.HasAuthority)
                            continue;

                        if (Empty == true)
                            Dict.Reset();
                    }
                }
            }
        }

        /// <summary>
        /// Handle a <see cref="RPK_RECORD"/>.
        /// </summary>
        /// <param name="Record"></param>
        /// <returns></returns>
        public bool Handle(Xnet Sender, RPK_RECORD Record)
        {
            ReplikaDictionary Dictionary;

            if (Record.Value is null)
                Dictionary = Get(Record.SenderKey, Record.Key, false);

            else
                Dictionary = Get(Record.SenderKey, Record.Key, true);

            if (Dictionary is null)
                return false;

            if (Sender != null)
                Dictionary.AddHolder(Sender);

            if (Dictionary.Handle(Record))
            {
                Changed?.Invoke(this, Record.ItemId);
                return true;
            }

            return false;
        }

    }
}
