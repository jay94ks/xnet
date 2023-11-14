namespace XnetStreams.Internals.Packets
{
    /// <summary>
    /// Set-Length request.
    /// </summary>
    internal class PKT_SETLENGTH_RESULT : PKT_BASE_RESULT
    {
        /// <summary>
        /// Length in bytes.
        /// </summary>
        public long Length { get; set; }

        /// <inheritdoc/>
        protected override void Decode(BinaryReader Reader)
        {
            base.Decode(Reader);
            Length = Reader.Read7BitEncodedInt64();
        }

        /// <inheritdoc/>
        protected override void Encode(BinaryWriter Writer)
        {
            base.Encode(Writer);
            Writer.Write7BitEncodedInt64(Length);
        }
    }
}
