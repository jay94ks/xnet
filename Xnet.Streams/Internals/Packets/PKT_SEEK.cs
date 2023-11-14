namespace XnetStreams.Internals.Packets
{
    /// <summary>
    /// Seek request.
    /// </summary>
    internal class PKT_SEEK : PKT_BASE
    {
        /// <summary>
        /// Origin.
        /// </summary>
        public SeekOrigin Origin { get; set; }

        /// <summary>
        /// Cursor.
        /// </summary>
        public long Cursor { get; set; }

        /// <inheritdoc/>
        public override Task ExecuteAsync(Xnet Connection)
            => StreamExtender.Get(Connection).SeekAsync(Connection, this);

        /// <inheritdoc/>
        protected override void Decode(BinaryReader Reader)
        {
            base.Decode(Reader);
            Origin = (SeekOrigin) Reader.ReadByte();
            Cursor = Reader.Read7BitEncodedInt64();
        }

        /// <inheritdoc/>
        protected override void Encode(BinaryWriter Writer)
        {
            base.Encode(Writer);
            Writer.Write((byte)Origin);
            Writer.Write7BitEncodedInt64(Cursor);
        }
    }
}
