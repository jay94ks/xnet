
namespace XnetDsa.Impls.RawBytes
{
    /// <summary>
    /// Basic DSA Sign.
    /// </summary>
    /// <typeparam name="TAlgorithm"></typeparam>
    internal class BasicDsaSign<TAlgorithm> : BaseRawBytes, IDsaSign where TAlgorithm : DsaAlgorithm
    {
        /// <inheritdoc/>
        public BasicDsaSign(DsaAlgorithm Algorithm) : base(Algorithm, Algorithm.SizeOfSign)
        {
        }

        /// <inheritdoc/>
        public BasicDsaSign(DsaAlgorithm Algorithm, byte[] Data) : base(Algorithm, Data)
        {
        }

        /// <inheritdoc/>
        public bool Equals(IDsaSign Other)
        {
            if (ReferenceEquals(Other, null))
                return false;

            return ToHex() == Other.ToHex();
        }


        /// <inheritdoc/>
        public override bool Validity
            => Validity && Algorithm != null
            && Algorithm.SizeOfSign == Length;
    }
}
