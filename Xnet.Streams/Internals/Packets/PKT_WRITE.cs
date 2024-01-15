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

            int Size = Reader.ReadByte();
            Size |= (int)(Reader.ReadByte() << 8);
            Size |= (int)(Reader.ReadByte() << 16);
            Size |= (int)(Reader.ReadByte() << 24);

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
                Writer.Write(byte.MinValue);
                Writer.Write(byte.MinValue);
                return;
            }

            Writer.Write((byte)(Data.Length & 0xff));
            Writer.Write((byte)(Data.Length >> 8));
            Writer.Write((byte)(Data.Length >> 16));
            Writer.Write((byte)(Data.Length >> 24));
            Writer.Write(Data);
        }
    }
}
