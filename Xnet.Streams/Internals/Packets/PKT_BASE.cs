namespace XnetStreams.Internals.Packets
{
    /// <summary>
    /// Base class for requests.
    /// </summary>
    internal abstract class PKT_BASE : Xnet.BasicPacket
    {
        /// <summary>
        /// Stream Id.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Trace Id.
        /// </summary>
        public Guid TraceId { get; set; }

        /// <inheritdoc/>
        protected override void Decode(BinaryReader Reader)
        {
            Id = new Guid(Reader.ReadBytes(16));
            TraceId = new Guid(Reader.ReadBytes(16));
        }

        /// <inheritdoc/>
        protected override void Encode(BinaryWriter Writer)
        {
            Writer.Write(Id.ToByteArray());
            Writer.Write(TraceId.ToByteArray());
        }
    }
}
