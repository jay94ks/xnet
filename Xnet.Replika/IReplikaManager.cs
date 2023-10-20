using XnetDsa;

namespace XnetReplika
{
    /// <summary>
    /// Replika manager interface.
    /// </summary>
    public interface IReplikaManager
    {
        /// <summary>
        /// DSA public key.
        /// </summary>
        DsaPubKey PubKey { get; }

        /// <summary>
        /// Local repository.
        /// </summary>
        IReplikaRepository Local { get; }

        /// <summary>
        /// Overlay repository.
        /// </summary>
        IReplikaRepository Overlay { get; }

        /// <summary>
        /// Get owner keys.
        /// </summary>
        /// <returns></returns>
        DsaPubKey[] GetOwnerKeys(int Offset = 0, int Count = 1024);

        /// <summary>
        /// Get the repository for the owner.
        /// </summary>
        /// <param name="OwnerKey"></param>
        /// <returns></returns>
        IReplikaRepository Get(DsaPubKey OwnerKey);

        /// <summary>
        /// Get the dictionary for the owner with dictionary key.
        /// </summary>
        /// <param name="OwnerKey"></param>
        /// <param name="Key"></param>
        /// <returns></returns>
        IReplikaDictionary Get(DsaPubKey OwnerKey, Guid Key);
    }
}
