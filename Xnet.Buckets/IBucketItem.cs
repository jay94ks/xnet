namespace XnetBuckets
{
    /// <summary>
    /// Bucket item interface.
    /// </summary>
    public interface IBucketItem
    {
        /// <summary>
        /// Serialize <see cref="IBucketItem"/> into <see cref="BinaryWriter"/>.
        /// </summary>
        /// <param name="Writer"></param>
        public abstract void Serialize(BinaryWriter Writer);

        /// <summary>
        /// Serialize <see cref="IBucketItem"/> into <see cref="BinaryReader"/>.
        /// </summary>
        /// <param name="Reader"></param>
        public abstract void Deserialize(BinaryReader Reader);
    }
}
