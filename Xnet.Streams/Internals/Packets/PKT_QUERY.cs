using Newtonsoft.Json.Linq;

namespace XnetStreams.Internals.Packets
{
    /// <summary>
    /// Stream Query request.
    /// </summary>
    internal class PKT_QUERY : Xnet.BasicPacket
    {
        /// <summary>
        /// Trace Id.
        /// </summary>
        public Guid TraceId { get; set; }

        /// <summary>
        /// Query timeout.
        /// </summary>
        public int Timeout { get; set; }

        /// <summary>
        /// Path to the resource.
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// Extras to pass if required.
        /// </summary>
        public JObject Extras { get; set; }

        /// <inheritdoc/>
        public override Task ExecuteAsync(Xnet Connection)
            => StreamExtender.Get(Connection).QueryAsync(Connection, this);

        protected override void Encode(BinaryWriter Writer)
        {
            Writer.Write(TraceId.ToByteArray());
            Writer.Write7BitEncodedInt(Timeout);
            Writer.Write(Path ?? string.Empty);

            if (Extras is null)
            {
                Writer.Write7BitEncodedInt(0);
                return;
            }

            var Bson = PKT_OPEN.EncodeOptions(Extras);
            Writer.Write7BitEncodedInt(Bson.Length);
            Writer.Write(Bson);
        }

        protected override void Decode(BinaryReader Reader)
        {
            TraceId = new Guid(Reader.ReadBytes(16));
            Timeout = Reader.Read7BitEncodedInt();
            Path = Reader.ReadString();
            Extras = PKT_OPEN.DecodeOptions(Reader);
        }

    }
}
