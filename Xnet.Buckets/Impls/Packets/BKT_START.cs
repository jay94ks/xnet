namespace XnetBuckets.Impls.Packets
{
    /// <summary>
    /// A packet to notify that the bucket is ready and started.
    /// </summary>
    [Xnet.BasicPacket(Name = "xnet.bucket.start", Kind = "xnet.bucket")]
    internal class BKT_START : Xnet.BasicPacket
    {
        /// <summary>
        /// Bucket Id.
        /// </summary>
        public Guid BucketId { get; set; }

        /// <inheritdoc/>
        public override Task ExecuteAsync(Xnet Connection)
            => BucketManager.Get(Connection).BucketStart(Connection, this);

        /// <inheritdoc/>
        protected override void Encode(BinaryWriter Writer)
        {
            Writer.Write(BucketId.ToByteArray());
        }

        /// <inheritdoc/>
        protected override void Decode(BinaryReader Reader)
        {
            BucketId = new Guid(Reader.ReadBytes(16));
        }
    }
}
