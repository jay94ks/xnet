namespace XnetBuckets
{
    /// <summary>
    /// Bucket item entry.
    /// </summary>
    public class BucketItemEntry
    {
        /// <summary>
        /// Item Id.
        /// </summary>
        public Guid ItemId { get; internal set; }

        /// <summary>
        /// Data bytes.
        /// </summary>
        public byte[] Data { get; internal set; }

        /// <summary>
        /// Cached item.
        /// </summary>
        public IBucketItem CachedItem { get; internal set; }
    }
}
