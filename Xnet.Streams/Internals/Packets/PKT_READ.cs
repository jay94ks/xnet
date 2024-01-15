using System.Drawing;

namespace XnetStreams.Internals.Packets
{
    /// <summary>
    /// Read request.
    /// </summary>
    internal class PKT_READ : PKT_BASE
    {
        /// <summary>
        /// Required size.
        /// </summary>
        public int Size { get; set; }

        /// <inheritdoc/>
        public override Task ExecuteAsync(Xnet Connection)
            => StreamExtender.Get(Connection).ReadAsync(Connection, this);

        /// <inheritdoc/>
        protected override void Decode(BinaryReader Reader)
        {
            base.Decode(Reader);
            Size = Reader.ReadByte();
            Size |= (Reader.ReadByte() << 8);
            Size |= (Reader.ReadByte() << 16);
            Size |= (Reader.ReadByte() << 24);
        }

        /// <inheritdoc/>
        protected override void Encode(BinaryWriter Writer)
        {
            base.Encode(Writer);
            Writer.Write((byte)(Size & 0xff));
            Writer.Write((byte)(Size >> 8));
            Writer.Write((byte)(Size >> 16));
            Writer.Write((byte)(Size >> 24));
        }
    }
}
