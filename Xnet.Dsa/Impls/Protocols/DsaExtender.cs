namespace XnetDsa.Impls.Protocols
{
    /// <summary>
    /// DSA extender.
    /// </summary>
    internal class DsaExtender : Xnet.PacketExtender
    {
        /// <summary>
        /// DSA key.
        /// </summary>
        public DsaKey Key { get; set; }

        /// <summary>
        /// DSA public key.
        /// </summary>
        public DsaPubKey PubKey { get; set; }

        /// <inheritdoc/>
        public async Task ExecuteAsync(Xnet Connection, Xnet.Packet Packet, Func<Task> Next)
        {
            var Prev = DsaAlgorithm.Default;
            DsaAlgorithm.Default = Key.Algorithm;

            try { await Next.Invoke(); }
            finally
            {
                DsaAlgorithm.Default = Prev;
            }
        }
    }
}
