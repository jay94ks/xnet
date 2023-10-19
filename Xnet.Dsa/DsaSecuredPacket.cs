using System.Text;
using XnetDsa.Impls.Protocols;

namespace XnetDsa
{
    /// <summary>
    /// Base class for secure packet 
    /// that protected by digital signing algorithm.
    /// Note that, packets which derived from this can be passed to other connection
    /// by broadcast or emitting to other once.
    /// </summary>
    public abstract class DsaSecuredPacket : Xnet.BasicPacket
    {
        /// <summary>
        /// Get the timestamp that represents now.
        /// </summary>
        /// <returns></returns>
        private static long Now() => (long)Math.Max((DateTime.Now - DateTime.UnixEpoch).TotalSeconds, 0);

        /// <summary>
        /// Timestamp.
        /// </summary>
        private long Timestamp = Now();

        /// <summary>
        /// Sender.
        /// </summary>
        public DsaPubKey SenderKey { get; private set; }

        /// <summary>
        /// Sender's Key.
        /// </summary>
        public DsaSign SenderSign { get; private set; }

        /// <summary>
        /// Make a digest.
        /// </summary>
        /// <param name="Dsa"></param>
        private DsaDigest MakeDigest()
        {
            var Algorithm = SenderKey.Algorithm ?? DsaAlgorithm.Default;

            using var NestedStream = new MemoryStream();
            using (var NestedWriter = new BinaryWriter(NestedStream, Encoding.UTF8, true))
            {
                SenderKey.Encode(NestedWriter);
                NestedWriter.Write7BitEncodedInt64(Timestamp);
                Encode(NestedWriter, SenderKey);
            }

            NestedStream.Position = 0;
            return DsaDigest.Make(NestedStream, Algorithm);
        }

        /// <inheritdoc/>
        protected sealed override void Encode(BinaryWriter Writer)
        {
            var Dsa = Xnet.Current.GetExtender<DsaExtender>();
            if (Dsa.Key.Validity == false)
                throw new InvalidOperationException("no DSA private key configured.");

            if (SenderKey.Validity == false || SenderSign.Validity == false)
            {
                SenderKey = Dsa.PubKey;

                var Digest = MakeDigest();
                SenderSign = Dsa.Key.Sign(Digest);
            }

            SenderKey.Encode(Writer);
            Writer.Write7BitEncodedInt64(Timestamp);
            Writer.Write(SenderSign.ToRawBytes());
            Encode(Writer, SenderKey);
        }

        /// <inheritdoc/>
        protected sealed override void Decode(BinaryReader Reader)
        {
            if ((SenderKey = DsaPubKey.Decode(Reader)).Validity == false)
                return;

            // --> restore remote timestamp.
            Timestamp = Reader.Read7BitEncodedInt64();

            // --> restore key, sign and decode remained bytes.
            var Algorithm = SenderKey.Algorithm;
            var SignBytes = Reader.ReadBytes(Algorithm.SizeOfSign);
            SenderSign = new DsaSign(Algorithm.RestoreSign(SignBytes));
            Decode(Reader, SenderKey);
        }

        /// <summary>
        /// Encode the packet with DSA public key.
        /// </summary>
        /// <param name="Writer"></param>
        /// <param name="PubKey"></param>
        protected abstract void Encode(BinaryWriter Writer, DsaPubKey PubKey);

        /// <summary>
        /// Decode the packet with DSA public key.
        /// </summary>
        /// <param name="Reader"></param>
        /// <param name="PubKey"></param>
        protected abstract void Decode(BinaryReader Reader, DsaPubKey PubKey);

        /// <inheritdoc/>
        public sealed override Task ExecuteAsync(Xnet Connection)
        {
            var Validation = 
                SenderKey.Validity == false || SenderSign.Validity == false ||
                SenderKey.Verify(SenderSign, MakeDigest()) == false;

            return ExecuteAsync(Connection, Validation == false);
        }

        /// <summary>
        /// Execute the packet asynchronously.
        /// <paramref name="Validation"/> means that <see cref="SenderKey"/> and 
        /// <see cref="SenderSign"/> is valid and correctly set or not.
        /// </summary>
        /// <param name="Connection"></param>
        /// <param name="Validation"></param>
        /// <returns></returns>
        protected abstract Task ExecuteAsync(Xnet Connection, bool Validation);
    }
}
