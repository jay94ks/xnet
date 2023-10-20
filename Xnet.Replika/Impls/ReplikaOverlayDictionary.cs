namespace XnetReplika.Impls
{
    /// <summary>
    /// Overlay Dictionary.
    /// </summary>
    internal class ReplikaOverlayDictionary : IReplikaDictionary
    {
        private readonly ReplikaRepository m_Repository;
        private readonly Guid m_Key;
        private int m_Counter;

        /// <summary>
        /// Initialize a new <see cref="ReplikaOverlayDictionary"/>
        /// </summary>
        /// <param name="Repository"></param>
        /// <param name="Key"></param>
        public ReplikaOverlayDictionary(ReplikaRepository Repository, Guid Key)
        {
            m_Repository = Repository;
            m_Counter = 0;
            m_Key = Key;
        }

        /// <summary>
        /// Destructor.
        /// </summary>
        ~ReplikaOverlayDictionary()
        {
            if (Interlocked.Exchange(ref m_Counter, 0) != 0)
                m_Repository.Changed -= OnRepositoryChanged;
        }

        /// <inheritdoc/>
        public IEnumerable<Guid> Keys => m_Repository.GetItemKeys(m_Key);

        /// <inheritdoc/>
        public bool HasAuthority => true;

        /// <summary>
        /// Internal event for <see cref="Changed"/>.
        /// </summary>
        private event Action<IReplikaDictionary, Guid> ChangedInternal;

        /// <inheritdoc/>
        public event Action<IReplikaDictionary, Guid> Changed
        {
            add
            {
                ChangedInternal += value;

                if (Interlocked.Increment(ref m_Counter) == 1)
                    m_Repository.Changed += OnRepositoryChanged;
            }

            remove
            {
                ChangedInternal -= value;

                if (Interlocked.Decrement(ref m_Counter) == 0)
                    m_Repository.Changed -= OnRepositoryChanged;
            }
        }

        /// <summary>
        /// Proxy repository event to <see cref="Changed"/> event.
        /// </summary>
        /// <param name="Repository"></param>
        /// <param name="Guid"></param>
        private void OnRepositoryChanged(IReplikaRepository Repository, Guid Guid)
        {
            ChangedInternal?.Invoke(this, Guid);
        }

        /// <inheritdoc/>
        public bool TryGet(Guid Key, out byte[] Data) => m_Repository.TryGet(m_Key, Key, out Data);

        /// <inheritdoc/>
        public bool Set(Guid Key, byte[] Data) => m_Repository.Set(m_Key, Key, Data);

        /// <inheritdoc/>
        public Task<bool> SetAsync(Guid Key, byte[] Data, CancellationToken Token = default)
            => m_Repository.SetAsync(m_Key, Key, Data, Token);
    }
}
