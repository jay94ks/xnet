namespace XnetStreams.Internals.Packets
{
    /// <summary>
    /// Seek result.
    /// </summary>
    internal class PKT_SEEK_RESULT : PKT_BASE_RESULT
    {
        /// <summary>
        /// Cursor where the stream points.
        /// </summary>
        public long Cursor { get; set; }

        /// <inheritdoc/>
        protected override void Decode(BinaryReader Reader)
        {
            base.Decode(Reader);
            Cursor = Reader.Read7BitEncodedInt64();
        }

        /// <inheritdoc/>
        protected override void Encode(BinaryWriter Writer)
        {
            base.Encode(Writer);
            Writer.Write7BitEncodedInt64(Cursor);
        }
    }
}
