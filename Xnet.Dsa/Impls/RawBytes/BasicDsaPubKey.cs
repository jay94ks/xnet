
namespace XnetDsa.Impls.RawBytes
{
    /// <summary>
    /// Basic DSA Public Key.
    /// </summary>
    /// <typeparam name="TAlgorithm"></typeparam>
    internal class BasicDsaPubKey<TAlgorithm> : BaseRawBytes, IDsaPubKey where TAlgorithm : DsaAlgorithm
    {
        /// <inheritdoc/>
        public BasicDsaPubKey(DsaAlgorithm Algorithm) : base(Algorithm, Algorithm.SizeOfPubKey)
        {
        }

        /// <inheritdoc/>
        public BasicDsaPubKey(DsaAlgorithm Algorithm, byte[] Data) : base(Algorithm, Data)
        {
        }

        /// <inheritdoc/>
        public bool Equals(IDsaPubKey Other)
        {
            if (ReferenceEquals(Other, null))
                return false;

            return ToHex() == Other.ToHex();
        }


        /// <inheritdoc/>
        public override bool Validity
            => Validity && Algorithm != null
            && Algorithm.SizeOfPubKey == Length;
    }
}
