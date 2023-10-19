using System.ComponentModel.DataAnnotations;
using System.Text;
using XnetDsa.Impls.Protocols;

namespace XnetDsa
{
    /// <summary>
    /// Base class for secure packet
    /// that protected by digital signing algorithm.
    /// Note that, packets which derived from this are only valid between sender and receiver.
    /// Naturally, if this packet send to over any other connection, 
    /// the signature will be inconsistent and will no longer be valid.
    /// </summary>
    public abstract class DsaPacket : Xnet.BasicPacket
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
        /// Sender's Key.
        /// </summary>
        public DsaSign SenderSign { get; private set; }

        /// <summary>
        /// Make a digest.
        /// </summary>
        private DsaDigest MakeDigestForSender()
        {
            var Dsa = Xnet.Current.GetExtender<DsaExtender>();
            if (Dsa.Key.Validity == false)
                throw new InvalidOperationException("no DSA private key configured.");

            var Algorithm = Dsa.Key.Algorithm ?? DsaAlgorithm.Default;
            using var NestedStream = new MemoryStream();
            using (var NestedWriter = new BinaryWriter(NestedStream, Encoding.UTF8, true))
            {
                Dsa.PubKey.Encode(NestedWriter);
                NestedWriter.Write7BitEncodedInt64(Timestamp);
                Encode(NestedWriter, Dsa.PubKey);
            }

            NestedStream.Position = 0;
            return DsaDigest.Make(NestedStream, Algorithm);
        }

        /// <summary>
        /// Make a digest.
        /// </summary>
        private DsaDigest MakeDigestForReceiver()
        {
            var Remote = DsaRemoteState.FromXnet(Xnet.Current);
            if (Remote.PubKey.Validity == false)
                throw new InvalidOperationException("no DSA handshake completed.");

            var Algorithm = Remote.PubKey.Algorithm ?? DsaAlgorithm.Default;
            using var NestedStream = new MemoryStream();
            using (var NestedWriter = new BinaryWriter(NestedStream, Encoding.UTF8, true))
            {
                Remote.PubKey.Encode(NestedWriter);
                NestedWriter.Write7BitEncodedInt64(Timestamp);
                Encode(NestedWriter, Remote.PubKey);
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

            if (SenderSign.Validity == false)
            {
                var Digest = MakeDigestForSender();
                SenderSign = Dsa.Key.Sign(Digest);
            }

            Writer.Write7BitEncodedInt64(Timestamp);
            Writer.Write(SenderSign.ToRawBytes());
            Encode(Writer, Dsa.PubKey);
        }

        /// <inheritdoc/>
        protected sealed override void Decode(BinaryReader Reader)
        {
            var Remote = DsaRemoteState.FromXnet(Xnet.Current);
            if (Remote.PubKey.Validity == false)
                return;

            // --> restore remote timestamp.
            Timestamp = Reader.Read7BitEncodedInt64();

            // --> restore key, sign and decode remained bytes.
            var Algorithm = Remote.PubKey.Algorithm;
            var SignBytes = Reader.ReadBytes(Algorithm.SizeOfSign);
            SenderSign = new DsaSign(Algorithm.RestoreSign(SignBytes));
            Decode(Reader, Remote.PubKey);
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
            var Remote = DsaRemoteState.FromXnet(Xnet.Current);
            var Validation =
                Remote.PubKey.Validity == false || SenderSign.Validity == false ||
                Remote.PubKey.Verify(SenderSign, MakeDigestForReceiver()) == false;

            return ExecuteAsync(Connection, Validation == false);
        }

        /// <summary>
        /// Execute the packet asynchronously.
        /// <paramref name="Validation"/> means that 
        /// <see cref="SenderSign"/> is valid and correctly set or not.
        /// </summary>
        /// <param name="Connection"></param>
        /// <param name="Validation"></param>
        /// <returns></returns>
        protected abstract Task ExecuteAsync(Xnet Connection, bool Validation);

    }
}
