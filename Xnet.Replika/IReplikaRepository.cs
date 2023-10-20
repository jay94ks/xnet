namespace XnetReplika
{
    /// <summary>
    /// Replika repository interface.
    /// </summary>
    public interface IReplikaRepository
    {
        /// <summary>
        /// Keys.
        /// </summary>
        IEnumerable<Guid> Keys { get; }

        /// <summary>
        /// Access to the replika dictionary.
        /// </summary>
        /// <param name="Key"></param>
        /// <returns></returns>
        IReplikaDictionary this[Guid Key] { get; }

        /// <summary>
        /// Try to get item from repository.
        /// </summary>
        /// <param name="Key"></param>
        /// <param name="ItemKey"></param>
        /// <param name="Data"></param>
        /// <returns></returns>
        bool TryGet(Guid Key, Guid ItemKey, out byte[] Data);

        /// <summary>
        /// Set the item into repository.
        /// </summary>
        /// <param name="Key"></param>
        /// <param name="ItemKey"></param>
        /// <param name="Data"></param>
        /// <returns></returns>
        bool Set(Guid Key, Guid ItemKey, byte[] Data);

        /// <summary>
        /// Set the item into repository asynchronously.
        /// </summary>
        /// <param name="Key"></param>
        /// <param name="ItemKey"></param>
        /// <param name="Data"></param>
        /// <param name="Token"></param>
        /// <returns></returns>
        Task<bool> SetAsync(Guid Key, Guid ItemKey, byte[] Data, CancellationToken Token = default);
    }
}
