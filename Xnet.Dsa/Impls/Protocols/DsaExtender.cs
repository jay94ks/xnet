using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XnetDsa.Impls.Protocols.Packets;

namespace XnetDsa.Impls.Protocols
{
    /// <summary>
    /// DSA extender.
    /// </summary>
    internal class DsaExtender : Xnet.BasicPacketProvider<DsaExtender>, Xnet.PacketExtender, Xnet.ConnectionExtender
    {
        /// <summary>
        /// DSA key.
        /// </summary>
        public DsaKey Key { get; set; }

        /// <summary>
        /// DSA public key.
        /// </summary>
        public DsaPubKey PubKey { get; set; }

        /// <summary>
        /// Determines whether the DSA layer should be initiated or not.
        /// </summary>
        public bool WithLayer { get; set; }

        /// <inheritdoc/>
        public async ValueTask OnConnectedAsync(Xnet Connection)
        {
            if (Connection.IsServerMode == true)
                return;

            // --> if no key configured, skip initiator.
            if (Key.Validity == false || PubKey.Validity == false || WithLayer == false)
                return;

            // --> emit the DSA layer initiator packet if client-mode.
            await Connection.EmitAsync(DSA_PUBKEY_FIRST.Create(Connection));
        }

        /// <inheritdoc/>
        public ValueTask OnDisconnectedAsync(Xnet Connection)
        {
            return ValueTask.CompletedTask;
        }

        /// <inheritdoc/>
        public Task ExecuteAsync(Xnet Connection, Xnet.Packet Packet, Func<Task> Next)
        {
            if (Packet is DsaPacket)
            {
                var State = DsaRemoteState.FromXnet(Connection);
                if (State.PubKey.Validity == true)
                    return Next.Invoke();

                // --> DSA layer is not initiated: disconnect immediately.
                Connection.Dispose();
                return Task.CompletedTask;
            }

            return Next.Invoke();
        }

        /// <inheritdoc/>
        protected override void MapTypes()
        {
            MapFrom(
                typeof(DSA_PUBKEY_FIRST), 
                typeof(DSA_PUBKEY_SECOND),
                typeof(DSA_PUBKEY_THIRD), 
                typeof(DSA_PUBKEY_LAST));
        }

    }
}
