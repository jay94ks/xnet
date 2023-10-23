namespace XnetBuckets
{
    /// <summary>
    /// Bucket manager.
    /// </summary>
    public interface IBucketManager : Xnet.Extender
    {
        /// <summary>
        /// Activate the specified bucket asynchronously.
        /// </summary>
        /// <param name="Bucket"></param>
        /// <returns></returns>
        ValueTask<bool> ActivateAsync(Bucket Bucket);

        /// <summary>
        /// Deactivate the specified bucket asynchronously.
        /// </summary>
        /// <param name="Bucket"></param>
        /// <returns></returns>
        ValueTask<bool> DeactivateAsync(Bucket Bucket);
    }
}
