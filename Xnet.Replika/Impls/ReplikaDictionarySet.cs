using XnetDsa;

namespace XnetReplika.Impls
{
    /// <summary>
    /// Dictionary Set.
    /// Key:Dict set.
    /// </summary>
    internal class ReplikaDictionarySet : Dictionary<Guid, ReplikaDictionary>, IReplikaRepository
    {
        private readonly ReplikaRepository m_Repository;
        private readonly DsaPubKey m_Owner;

        /// <summary>
        /// Initialize a new <see cref="ReplikaDictionarySet"/> instance.
        /// </summary>
        /// <param name="Repository"></param>
        public ReplikaDictionarySet(ReplikaRepository Repository, DsaPubKey Owner)
        {
            m_Repository = Repository;
            m_Owner = Owner;
        }

        /// <inheritdoc/>
        IEnumerable<Guid> IReplikaRepository.Keys { get { lock (this) { return Keys.ToArray(); } } }

        /// <inheritdoc/>
        IReplikaDictionary IReplikaRepository.this[Guid Key] => m_Repository.GetDictionary(m_Owner, Key);

        /// <inheritdoc/>
        public bool Set(Guid Key, Guid ItemKey, byte[] Data)
        {
            if (m_Repository.Manager.PubKey != m_Owner)
                return false;

            return m_Repository.Set(Key, ItemKey, Data);
        }

        /// <inheritdoc/>
        public Task<bool> SetAsync(Guid Key, Guid ItemKey, byte[] Data, CancellationToken Token = default)
        {
            if (m_Repository.Manager.PubKey != m_Owner)
                return Task.FromResult(false);

            return m_Repository.SetAsync(Key, ItemKey, Data, Token);
        }

        /// <inheritdoc/>
        public bool TryGet(Guid Key, Guid ItemKey, out byte[] Data)
        {
            ReplikaDictionary Dict;
            lock (this)
            {
                Data = null;

                if (TryGetValue(Key, out Dict) == false)
                    return false;

                if (Dict is null)
                    return false;
            }

            if (Dict.TryGet(ItemKey, out var Value) == false)
                return false;

            Data = Value.Value;
            return true;
        }
    }
}
