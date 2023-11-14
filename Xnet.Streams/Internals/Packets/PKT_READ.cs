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
        public ushort Size { get; set; }

        /// <inheritdoc/>
        public override Task ExecuteAsync(Xnet Connection)
            => StreamExtender.Get(Connection).ReadAsync(Connection, this);

        /// <inheritdoc/>
        protected override void Decode(BinaryReader Reader)
        {
            base.Decode(Reader);
            Size = Reader.ReadByte();
            Size |= (ushort)(Reader.ReadByte() << 8);
        }

        /// <inheritdoc/>
        protected override void Encode(BinaryWriter Writer)
        {
            base.Encode(Writer);
            Writer.Write((byte)(Size & 0xff));
            Writer.Write((byte)(Size >> 8));
        }
    }
}
