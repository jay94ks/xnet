namespace XnetStreams.Internals.Packets
{
    /// <summary>
    /// Close request.
    /// </summary>
    internal class PKT_CLOSE : PKT_BASE
    {
        /// <summary>
        /// Flush required or not.
        /// </summary>
        public bool FlushRequired { get; set; }

        public override Task ExecuteAsync(Xnet Connection)
            => StreamExtender.Get(Connection).CloseAsync(Connection, this);

        /// <inheritdoc/>
        protected override void Decode(BinaryReader Reader)
        {
            base.Decode(Reader);
            FlushRequired = Reader.ReadByte() != byte.MinValue;
        }

        /// <inheritdoc/>
        protected override void Encode(BinaryWriter Writer)
        {
            base.Encode(Writer);
            Writer.Write(FlushRequired
                ? byte.MaxValue 
                : byte.MinValue);
        }
    }
}
