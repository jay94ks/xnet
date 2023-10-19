using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XnetDsa.Impls.RawBytes
{
    /// <summary>
    /// Basic DSA digest.
    /// </summary>
    /// <typeparam name="TAlgorithm"></typeparam>
    internal class BasicDsaDigest<TAlgorithm> : BaseRawBytes, IDsaDigest where TAlgorithm : DsaAlgorithm
    {
        /// <inheritdoc/>
        public BasicDsaDigest(DsaAlgorithm Algorithm) : base(Algorithm, Algorithm.SizeOfDigest)
        {
        }

        /// <inheritdoc/>
        public BasicDsaDigest(DsaAlgorithm Algorithm, byte[] Data) : base(Algorithm, Data)
        {
        }

        /// <inheritdoc/>
        public bool Equals(IDsaDigest Other)
        {
            if (ReferenceEquals(Other, null))
                return false;

            return ToHex() == Other.ToHex();
        }

        /// <inheritdoc/>
        public override bool Validity => Algorithm != null && Algorithm.SizeOfDigest == Length;
    }
}
