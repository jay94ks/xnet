using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XnetDsa.Impls.Protocols.Packets
{
    /// <summary>
    /// DSA layer last packet. (by server)
    /// </summary>

    [Xnet.BasicPacket(Name = "xnet.dsa.pk_fin", Kind = "xnet.dsa")]
    internal class DSA_PUBKEY_LAST : Xnet.BasicPacket
    {
        /// <summary>
        /// Result.
        /// </summary>
        public bool ServerResult { get; set; }

        /// <inheritdoc/>
        public override Task ExecuteAsync(Xnet Connection)
        {
            var Dsa = Connection.GetExtender<DsaExtender>();
            if (Dsa is null || Dsa.WithLayer == false)
                return Task.CompletedTask;

            if (Connection.IsServerMode == true)
            {
                Connection.Dispose();
                return Task.CompletedTask;
            }

            var State = DsaRemoteState.FromXnet(Connection);

            State.PubKey = default;
            if (ServerResult)
            {
                State.PubKey = State.RequestedPubKey;
            }

            State.Completed(State.PubKey.Validity);
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        protected override void Decode(BinaryReader Reader)
        {
            ServerResult = Reader.ReadByte() != byte.MinValue;
        }

        /// <inheritdoc/>
        protected override void Encode(BinaryWriter Writer)
        {
            Writer.Write(ServerResult ? byte.MaxValue : byte.MinValue);
        }
    }
}
