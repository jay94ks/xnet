namespace XnetStreams.Internals.Packets
{
    /// <summary>
    /// Base class for results.
    /// </summary>
    internal class PKT_BASE_RESULT : PKT_BASE
    {
        /// <summary>
        /// Status.
        /// </summary>
        public StreamStatus Status { get; set; } = StreamStatus.Ok;

        /// <inheritdoc/>
        public override Task ExecuteAsync(Xnet Connection)
            => StreamExtender.Get(Connection).HandleResultAsync(Connection, this);

        /// <inheritdoc/>
        protected override void Decode(BinaryReader Reader)
        {
            base.Decode(Reader);
            Status = (StreamStatus)Reader.ReadByte();
        }

        /// <inheritdoc/>
        protected override void Encode(BinaryWriter Writer)
        {
            base.Encode(Writer);
            Writer.Write((byte)Status);
        }
    }
}
