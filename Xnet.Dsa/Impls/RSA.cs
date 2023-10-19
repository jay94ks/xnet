using System.Security.Cryptography;
using XnetDsa.Impls.RawBytes;
using XnetDsa.Impls.Shared;
using NetRSA = System.Security.Cryptography.RSA;

namespace XnetDsa.Impls
{
    internal class RSA : DsaAlgorithm
    {
        private readonly int m_BitWidth;
        private readonly string[] m_Aliases;
        private readonly HashAlgorithmName m_HAN;
        private readonly Func<Func<HashAlgorithm, byte[]>, byte[]> m_HashFn;

        /// <summary>
        /// Initialize a new <see cref="RSA"/> algorithm.
        /// </summary>
        /// <param name="BitWidth"></param>
        /// <param name="HAN"></param>
        public RSA(int BitWidth, HashAlgorithmName HAN)
        {
            m_HAN = HAN;
            m_BitWidth = BitWidth;

            if (m_HAN == HashAlgorithmName.SHA256)
            {
                SizeOfDigest = 32;
                m_HashFn = SharedSha256.Exec;
            }
            else if (m_HAN == HashAlgorithmName.SHA384)
            {
                SizeOfDigest = 48;
                m_HashFn = SharedSha384.Exec;
            }
            else if (m_HAN == HashAlgorithmName.SHA512)
            {
                SizeOfDigest = 64;
                m_HashFn = SharedSha512.Exec;
            }
            else throw new NotSupportedException($"incompatible hash algorithm: {HAN.Name}.");

            var Postfix 
                = HAN != HashAlgorithmName.SHA256
                ? "-" + HAN.Name.ToLower() : "";

            m_Aliases = new string[]
            {
                "rsa" + BitWidth + Postfix,
                "rsa-" + BitWidth + Postfix,
            };

            if (HAN != HashAlgorithmName.SHA256)
                Name = "rsa" + BitWidth + Postfix;

            else
                Name = "rsa" + BitWidth;
        }

        /// <inheritdoc/>
        public override string Name { get; }

        /// <inheritdoc/>
        public override IEnumerable<string> Aliases => m_Aliases;

        /// <inheritdoc/>
        public override int SizeOfKey => (m_BitWidth / 4) + (m_BitWidth / 16) * 5 + 3;

        /// <inheritdoc/>
        public override int SizeOfPubKey => 3 + (m_BitWidth / 8);

        /// <inheritdoc/>
        public override int SizeOfDigest { get; }

        /// <inheritdoc/>
        public override int SizeOfSign => 32;

        /// <inheritdoc/>
        public override IDsaDigest MakeDigest(Stream Stream)
        {
            if (m_HashFn != null)
            {
                var Result = m_HashFn.Invoke(
                    Hash => Hash.ComputeHash(Stream));

                return new BasicDsaDigest<RSA>(this, Result);
            }

            return null;
        }

        /// <inheritdoc/>
        public override IDsaDigest MakeDigest(ArraySegment<byte> Buffer)
        {
            if (m_HashFn != null)
            {
                if (Buffer.Array is null)
                    Buffer = Array.Empty<byte>();

                var Result = m_HashFn.Invoke(
                    Hash => Hash.ComputeHash(Buffer.Array, Buffer.Offset, Buffer.Count));

                return new BasicDsaDigest<RSA>(this, Result);
            }

            return null;
        }

        /// <inheritdoc/>
        public override IDsaKey NewKey()
        {
            using var Rsa = NetRSA.Create(m_BitWidth);

            var Params = Rsa.ExportParameters(true);
            using var Stream = new MemoryStream();

            Stream.Write(Params.D);
            Stream.Write(Params.DP);
            Stream.Write(Params.DQ);
            Stream.Write(Params.Exponent);
            Stream.Write(Params.InverseQ);
            Stream.Write(Params.Modulus);
            Stream.Write(Params.P);
            Stream.Write(Params.Q);

            return new BasicDsaKey<RSA>(this, Stream.ToArray());
        }

        /// <summary>
        /// Throw an exception if key is invalid.
        /// </summary>
        /// <param name="Key"></param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        private void ThrowIfKeyInvalid(IDsaKey Key)
        {
            if (Key is null)
                throw new ArgumentNullException(nameof(Key));

            if (Key.Algorithm != this)
                throw new InvalidOperationException("the specified key is not compatible.");
        }

        /// <summary>
        /// Throw an exception if public key is invalid.
        /// </summary>
        /// <param name="Key"></param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        private void ThrowIfKeyInvalid(IDsaPubKey Key)
        {
            if (Key is null)
                throw new ArgumentNullException(nameof(Key));

            if (Key.Algorithm != this)
                throw new InvalidOperationException("the specified public key is not compatible.");
        }

        /// <summary>
        /// Throw an exception if digest is invalid.
        /// </summary>
        /// <param name="Digest"></param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        private void ThrowIfDigestInvalid(IDsaDigest Digest)
        {
            if (Digest is null)
                throw new ArgumentNullException(nameof(Digest));

            if (Digest.Algorithm != this)
                throw new InvalidOperationException("the specified digest is not compatible.");
        }

        /// <summary>
        /// Throw an exception if sign is invalid.
        /// </summary>
        /// <param name="Sign"></param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        private void ThrowIfSignInvalid(IDsaSign Sign)
        {
            if (Sign is null)
                throw new ArgumentNullException(nameof(Sign));

            if (Sign.Algorithm != this)
                throw new InvalidOperationException("the specified signature is not compatible.");
        }

        /// <inheritdoc/>
        public override IDsaPubKey NewPubKey(IDsaKey Key)
        {
            ThrowIfKeyInvalid(Key);

            using var Rsa = NetRSA.Create(m_BitWidth);
            var Params = MakeParameterFromPrivateKey(Key);

            return new BasicDsaPubKey<RSA>(this,
                Params.Exponent.Concat(Params.Modulus).ToArray());
        }

        /// <summary>
        /// Make <see cref="RSAParameters"/> from <see cref="IDsaKey"/>.
        /// </summary>
        /// <param name="Key"></param>
        /// <returns></returns>
        private RSAParameters MakeParameterFromPrivateKey(IDsaKey Key)
        {
            var RawBytes = Key.ToRawBytes();
            byte[] TakeAndNext(int Length)
            {
                var Ret = RawBytes.Take(Length).ToArray();
                RawBytes = RawBytes.Skip(Length).ToArray();
                return Ret;
            }

            var Params = new RSAParameters
            {
                D = TakeAndNext(m_BitWidth / 8),
                DP = TakeAndNext(m_BitWidth / 16),
                DQ = TakeAndNext(m_BitWidth / 16),
                Exponent = TakeAndNext(3),
                InverseQ = TakeAndNext(m_BitWidth / 16),
                Modulus = TakeAndNext(m_BitWidth / 8),
                P = TakeAndNext(m_BitWidth / 16),
                Q = TakeAndNext(m_BitWidth / 16),
            };

            return Params;
        }

        /// <summary>
        /// Make <see cref="RSAParameters"/> from <see cref="IDsaPubKey"/>.
        /// </summary>
        /// <param name="Key"></param>
        /// <returns></returns>
        private RSAParameters MakeParameterFromPublicKey(IDsaPubKey Key)
        {
            var RawBytes = Key.ToRawBytes();
            byte[] TakeAndNext(int Length)
            {
                var Ret = RawBytes.Take(Length).ToArray();
                RawBytes = RawBytes.Skip(Length).ToArray();
                return Ret;
            }

            var Params = new RSAParameters
            {
                Exponent = TakeAndNext(3),
                Modulus = TakeAndNext(m_BitWidth / 8)
            };

            return Params;
        }

        /// <inheritdoc/>
        public override IDsaSign Sign(IDsaKey Key, IDsaDigest Digest)
        {
            ThrowIfKeyInvalid(Key);
            ThrowIfDigestInvalid(Digest);

            using var Rsa = NetRSA.Create(m_BitWidth);
            Rsa.ImportParameters(MakeParameterFromPrivateKey(Key));
            

            var Raw = Rsa.SignHash(Digest.ToRawBytes(), m_HAN, RSASignaturePadding.Pss);
            return new BasicDsaSign<RSA>(this, Raw);
        }

        /// <inheritdoc/>
        public override bool Validate(IDsaKey Key)
        {
            try
            {
                if (Key is null || Key.Algorithm != this)
                    return false;

                using var Rsa = NetRSA.Create(m_BitWidth);
                Rsa.ImportParameters(MakeParameterFromPrivateKey(Key));
                return true;
            }

            catch { }
            return false;
        }

        /// <inheritdoc/>
        public override bool ValidatePubKey(IDsaPubKey Key)
        {
            try
            {
                if (Key is null || Key.Algorithm != this)
                    return false;

                using var Rsa = NetRSA.Create(m_BitWidth);
                Rsa.ImportParameters(MakeParameterFromPublicKey(Key));
                return true;
            }

            catch { }
            return false;
        }

        /// <inheritdoc/>
        public override bool Verify(IDsaPubKey Key, IDsaSign Sign, IDsaDigest Digest)
        {
            ThrowIfKeyInvalid(Key);
            ThrowIfSignInvalid(Sign);
            ThrowIfDigestInvalid(Digest);

            using var Rsa = NetRSA.Create(m_BitWidth);
            Rsa.ImportParameters(MakeParameterFromPublicKey(Key));
            return Rsa.VerifyHash(Digest.ToRawBytes(), Sign.ToRawBytes(), m_HAN, RSASignaturePadding.Pss);
        }

        /// <inheritdoc/>
        public override IDsaDigest RestoreDigest(Span<byte> Buffer)
        {
            if (Buffer.Length != SizeOfDigest)
                return null;

            return new BasicDsaDigest<RSA>(this, Buffer.ToArray());
        }

        public override IDsaKey RestoreKey(Span<byte> Buffer)
        {
            if (Buffer.Length != SizeOfKey)
                return null;

            return new BasicDsaKey<RSA>(this, Buffer.ToArray());
        }

        public override IDsaPubKey RestorePubKey(Span<byte> Buffer)
        {
            if (Buffer.Length != SizeOfPubKey)
                return null;

            return new BasicDsaPubKey<RSA>(this, Buffer.ToArray());
        }

        public override IDsaSign RestoreSign(Span<byte> Buffer)
        {
            if (Buffer.Length != SizeOfSign)
                return null;

            return new BasicDsaSign<RSA>(this, Buffer.ToArray());
        }

    }
}
