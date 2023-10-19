namespace XnetDsa.Impls.Protocols.Packets
{
    /// <summary>
    /// DSA layer 3rd packet. (by client)
    /// </summary>

    [Xnet.BasicPacket(Name = "xnet.dsa.pk_res", Kind = "xnet.dsa")]
    internal class DSA_PUBKEY_THIRD : Xnet.BasicPacket
    {
        /// <summary>
        /// Signature, Signed, <see cref="DSA_PUBKEY_SECOND.ServerDigest"/>.
        /// </summary>
        public DsaSign ClientSign { get; set; }

        /// <summary>
        /// Result.
        /// </summary>
        public bool ClientResult { get; set; }

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

            var State = DsaRemoteState.FromXnet(Connection);
            var Result = ClientResult && ClientSign.Validity &&
                State.RequestedPubKey.Validity && State.RequiredDigest.Validity &&
                State.RequestedPubKey.Verify(ClientSign, State.RequiredDigest);

            State.PubKey = default;
            if (Result)
            {
                State.PubKey = State.RequestedPubKey;
            }

            State.Completed(State.PubKey.Validity);
            return Connection.EmitAsync(new DSA_PUBKEY_LAST
            {
                ServerResult = Result
            });
        }

        /// <inheritdoc/>
        protected override void Decode(BinaryReader Reader)
        {
            ClientSign = DsaSign.Decode(Reader);
            ClientResult = Reader.ReadByte() != byte.MinValue;
        }

        /// <inheritdoc/>
        protected override void Encode(BinaryWriter Writer)
        {
            ClientSign.Encode(Writer);
            Writer.Write(ClientResult ? byte.MaxValue : byte.MinValue);
        }
    }
}
