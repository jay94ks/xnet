namespace XnetReplika
{
    /// <summary>
    /// Replika dictionary interface.
    /// </summary>
    public interface IReplikaDictionary
    {
        /// <summary>
        /// Indicates whether the instance has authority or not.
        /// </summary>
        bool HasAuthority { get; }

        /// <summary>
        /// Item Keys.
        /// </summary>
        IEnumerable<Guid> Keys { get; }

        /// <summary>
        /// Triggered when value changed.
        /// </summary>
        event Action<IReplikaDictionary, Guid> Changed;

        /// <summary>
        /// Try to get item from the dictionary.
        /// </summary>
        /// <param name="Key"></param>
        /// <param name="Data"></param>
        /// <returns></returns>
        bool TryGet(Guid Key, out byte[] Data);

        /// <summary>
        /// Set the item into dictionary.
        /// </summary>
        /// <param name="Key"></param>
        /// <param name="Data"></param>
        /// <returns></returns>
        bool Set(Guid Key, byte[] Data);

        /// <summary>
        /// Set the item into the dictionary asynchronously.
        /// </summary>
        /// <param name="Key"></param>
        /// <param name="Data"></param>
        /// <param name="Token"></param>
        /// <returns></returns>
        Task<bool> SetAsync(Guid Key, byte[] Data, CancellationToken Token = default);
    }
}
