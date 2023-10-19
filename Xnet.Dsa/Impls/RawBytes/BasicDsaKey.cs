
namespace XnetDsa.Impls.RawBytes
{
    /// <summary>
    /// Basic DSA Private Key.
    /// </summary>
    /// <typeparam name="TAlgorithm"></typeparam>
    internal class BasicDsaKey<TAlgorithm> : BaseRawBytes, IDsaKey where TAlgorithm : DsaAlgorithm
    {
        /// <inheritdoc/>
        public BasicDsaKey(DsaAlgorithm Algorithm) : base(Algorithm, Algorithm.SizeOfKey)
        {
        }

        /// <inheritdoc/>
        public BasicDsaKey(DsaAlgorithm Algorithm, byte[] Data) : base(Algorithm, Data)
        {
        }

        /// <inheritdoc/>
        public bool Equals(IDsaKey Other)
        {
            if (ReferenceEquals(Other, null))
                return false;

            return ToHex() == Other.ToHex();
        }

        /// <inheritdoc/>
        public override bool Validity 
            => Validity && Algorithm != null 
            && Algorithm.SizeOfKey == Length;
    }
}
