namespace XnetStreams.Internals.Packets
{
    /// <summary>
    /// Set the write-timeout result.
    /// </summary>
    internal class PKT_WRITETIMEOUT_RESULT : PKT_BASE_RESULT
    {
        /// <summary>
        /// Timeout value.
        /// </summary>
        public int Timeout { get; set; }

        /// <inheritdoc/>
        protected override void Decode(BinaryReader Reader)
        {
            base.Decode(Reader);
            Timeout = Reader.Read7BitEncodedInt();
        }

        /// <inheritdoc/>
        protected override void Encode(BinaryWriter Writer)
        {
            base.Encode(Writer);
            Writer.Write7BitEncodedInt(Timeout);
        }
    }
}
