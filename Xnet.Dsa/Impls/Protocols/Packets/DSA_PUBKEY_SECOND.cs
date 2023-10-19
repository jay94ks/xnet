namespace XnetDsa.Impls.Protocols.Packets
{
    /// <summary>
    /// DSA layer 2nd packet. (by server)
    /// </summary>
    [Xnet.BasicPacket(Name = "xnet.dsa.pk_val", Kind = "xnet.dsa")]
    internal class DSA_PUBKEY_SECOND : Xnet.BasicPacket
    {
        /// <summary>
        /// Public Key.
        /// </summary>
        public DsaPubKey ServerKey { get; set; }

        /// <summary>
        /// Random digest.
        /// </summary>
        public DsaDigest ServerDigest { get; set; }

        /// <summary>
        /// Signature, Signed, <see cref="DSA_PUBKEY_FIRST.ClientDigest"/>.
        /// </summary>
        public DsaSign ServerSign { get; set; }

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

            if (Dsa.Key.Validity == false ||
                ServerKey.Validity == false || 
                ServerDigest.Validity == false ||
                ServerSign.Validity == false ||
                ServerResult == false)
            {
                // --> emit failure.
                return Connection.EmitAsync(new DSA_PUBKEY_THIRD
                {
                    ClientResult = false,
                });
            }

            var State = DsaRemoteState.FromXnet(Connection);
            if (ServerSign.Verify(Dsa.PubKey, State.RequiredDigest) == false)
            {
                // --> emit failure.
                return Connection.EmitAsync(new DSA_PUBKEY_THIRD
                {
                    ClientResult = false,
                });
            }

            State.RequestedPubKey = ServerKey;

            // --> emit failure.
            return Connection.EmitAsync(new DSA_PUBKEY_THIRD
            {
                ClientSign = Dsa.Key.Sign(ServerDigest),
                ClientResult = true,
            });
        }

        /// <inheritdoc/>
        protected override void Encode(BinaryWriter Writer)
        {
            ServerKey.Encode(Writer);
            ServerDigest.Encode(Writer);
            ServerSign.Encode(Writer);
            Writer.Write(ServerResult ? byte.MaxValue : byte.MinValue);
        }

        /// <inheritdoc/>
        protected override void Decode(BinaryReader Reader)
        {
            ServerKey = DsaPubKey.Decode(Reader);
            ServerDigest = DsaDigest.Decode(Reader);
            ServerSign = DsaSign.Decode(Reader);
            ServerResult = Reader.ReadByte() != byte.MinValue;
        }

    }
}
