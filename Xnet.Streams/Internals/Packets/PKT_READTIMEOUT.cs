using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XnetStreams.Internals.Packets
{
    /// <summary>
    /// Set the read-timeout request.
    /// </summary>
    internal class PKT_READTIMEOUT : PKT_BASE
    {
        /// <summary>
        /// Timeout value.
        /// </summary>
        public int Timeout { get; set; }

        /// <inheritdoc/>
        public override Task ExecuteAsync(Xnet Connection)
        {
            return StreamExtender.Get(Connection).SetReadTimeoutAsync(Connection, this);
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
