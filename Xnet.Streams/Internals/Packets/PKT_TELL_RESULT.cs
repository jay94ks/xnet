namespace XnetStreams.Internals.Packets
{
    /// <summary>
    /// Tell request.
    /// </summary>
    internal class PKT_TELL_RESULT : PKT_BASE_RESULT
    {
        /// <summary>
        /// Cursor.
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
