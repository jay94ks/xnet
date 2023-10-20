using XnetDsa;

namespace XnetReplika.Impls.Packets
{
    /// <summary>
    /// Replika record packet.
    /// </summary>
    [Xnet.BasicPacket(Name = "xnet.rpk.record", Kind = "xnet.rpk")]
    internal class RPK_RECORD : DsaSecuredPacket
    {
        /// <summary>
        /// Timeline.
        /// </summary>
        public long Timeline { get; set; }

        /// <summary>
        /// Key Id.
        /// </summary>
        public Guid Key { get; set; }

        /// <summary>
        /// Item Id.
        /// </summary>
        public Guid ItemId { get; set; }

        /// <summary>
        /// Value.
        /// </summary>
        public byte[] Value { get; set; }

        /// <summary>
        /// Local time when this packet received.
        /// </summary>
        public DateTime LocalTime { get; set; }

        /// <inheritdoc/>
        protected override void Encode(BinaryWriter Writer, DsaPubKey PubKey)
        {
            Writer.Write7BitEncodedInt64(Timeline);
            Writer.Write(Key.ToByteArray());
            Writer.Write(ItemId.ToByteArray());

            if (Value is null)
                Writer.Write(byte.MinValue);

            else
            {
                Writer.Write(byte.MaxValue);
                Writer.Write7BitEncodedInt(Value.Length);
                Writer.Write(Value);
            }

            LocalTime = DateTime.Now;
        }

        /// <inheritdoc/>
        protected override void Decode(BinaryReader Reader, DsaPubKey PubKey)
        {
            Timeline = Reader.Read7BitEncodedInt64();
            Key = new Guid(Reader.ReadBytes(16));
            ItemId = new Guid(Reader.ReadBytes(16));

            if (Reader.ReadByte() == byte.MinValue)
                Value = null;

            else
            {
                var Length = Reader.Read7BitEncodedInt();
                if (Length <= 0)
                    Value = Array.Empty<byte>();

                else
                    Value = Reader.ReadBytes(Length);
            }

            LocalTime = DateTime.Now;
        }

        /// <inheritdoc/>
        protected override Task ExecuteAsync(Xnet Connection, bool Validation)
        {
            var Rep = ReplikaExtender.GetReplikaManager(Connection);
            if (Rep != null && Validation == true)
                return Rep.OnRecord(Connection, this);

            return Task.CompletedTask;
        }
    }
}
