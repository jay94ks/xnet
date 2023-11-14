namespace XnetStreams.Internals.Packets
{
    /// <summary>
    /// Write request.
    /// </summary>
    internal class PKT_WRITE : PKT_BASE
    {
        /// <summary>
        /// Data bytes.
        /// </summary>
        public byte[] Data { get; set; }

        /// <inheritdoc/>
        public override Task ExecuteAsync(Xnet Connection)
            => StreamExtender.Get(Connection).WriteAsync(Connection, this);

        /// <inheritdoc/>
        protected override void Decode(BinaryReader Reader)
        {
            base.Decode(Reader);

            ushort Size = Reader.ReadByte();
            Size |= (ushort)(Reader.ReadByte() << 8);

            if (Size <= 0)
                Data = Array.Empty<byte>();

            else
                Data = Reader.ReadBytes(Size);
        }

        /// <inheritdoc/>
        protected override void Encode(BinaryWriter Writer)
        {
            base.Encode(Writer);

            if (Data is null || Data.Length <= 0)
            {
                Writer.Write(byte.MinValue);
                Writer.Write(byte.MinValue);
                return;
            }

            Writer.Write((byte)(Data.Length & 0xff));
            Writer.Write((byte)(Data.Length >> 8));
            Writer.Write(Data);
        }
    }
}
