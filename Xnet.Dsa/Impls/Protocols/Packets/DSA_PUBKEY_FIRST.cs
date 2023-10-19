using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XnetDsa.Impls.Protocols.Packets
{
    /// <summary>
    /// DSA layer 1st packet. (by client)
    /// </summary>
    [Xnet.BasicPacket(Name = "xnet.dsa.pk_set", Kind = "xnet.dsa")]
    internal class DSA_PUBKEY_FIRST : Xnet.BasicPacket
    {
        /// <summary>
        /// Client's Public Key.
        /// </summary>
        public DsaPubKey ClientKey { get; set; }

        /// <summary>
        /// Client's Random digest.
        /// </summary>
        public DsaDigest ClientDigest { get; set; }

        /// <summary>
        /// Create a new <see cref="DSA_PUBKEY_FIRST"/>.
        /// </summary>
        /// <param name="Conn"></param>
        /// <returns></returns>
        public static DSA_PUBKEY_FIRST Create(Xnet Connection)
        {
            var Dsa = Connection.GetExtender<DsaExtender>();
            var State = DsaRemoteState.FromXnet(Connection);
            var Temp = BitConverter
                .GetBytes(DateTime.Now.Ticks)
                .Concat(Guid.NewGuid().ToByteArray())
                .ToArray();

            State.RequiredDigest = DsaDigest.Make(Temp, Dsa.Key.Algorithm);
            State.Initiated();

            return new DSA_PUBKEY_FIRST
            {
                ClientKey = Dsa != null ? Dsa.PubKey : default,
                ClientDigest = State.RequiredDigest
            };
        }

        /// <inheritdoc/>
        public override Task ExecuteAsync(Xnet Connection)
        {
            var Dsa = Connection.GetExtender<DsaExtender>();
            if (Dsa is null || Dsa.WithLayer == false)
                return Task.CompletedTask;

            if (Connection.IsServerMode == false)
            {
                Connection.Dispose();
                return Task.CompletedTask;
            }

            if (Dsa.Key.Validity == false ||
                ClientKey.Validity == false ||
                ClientDigest.Validity == false)
            {
                // --> emit failure.
                return Connection.EmitAsync(new DSA_PUBKEY_SECOND
                {
                    ServerResult = false,
                });
            }

            var State = DsaRemoteState.FromXnet(Connection);
            State.RequestedPubKey = ClientKey;

            var Temp = BitConverter
                .GetBytes(DateTime.Now.Ticks)
                .Concat(Guid.NewGuid().ToByteArray())
                .ToArray();

            State.RequiredDigest = DsaDigest.Make(Temp, Dsa.Key.Algorithm);
            return Connection.EmitAsync(new DSA_PUBKEY_SECOND
            {
                ServerSign = Dsa.Key.Sign(ClientDigest),
                ServerDigest = State.RequiredDigest,
                ServerKey = Dsa.PubKey,
                ServerResult = false,
            });
        }

        /// <inheritdoc/>
        protected override void Decode(BinaryReader Reader)
        {
            ClientKey = DsaPubKey.Decode(Reader);
            ClientDigest = DsaDigest.Decode(Reader);
        }

        /// <inheritdoc/>
        protected override void Encode(BinaryWriter Writer)
        {
            ClientKey.Encode(Writer);
            ClientDigest.Encode(Writer);
        }
    }
}
