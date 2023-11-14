namespace XnetStreams.Internals.Packets
{
    /// <summary>
    /// Set the write-timeout request.
    /// </summary>
    internal class PKT_WRITETIMEOUT : PKT_BASE
    {
        /// <summary>
        /// Timeout value.
        /// </summary>
        public int Timeout { get; set; }

        /// <inheritdoc/>
        public override Task ExecuteAsync(Xnet Connection)
        {
            return StreamExtender.Get(Connection).SetWriteTimeoutAsync(Connection, this);
        }

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
