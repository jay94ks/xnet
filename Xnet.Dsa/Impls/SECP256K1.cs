using Secp256k1Net;
using System.Security.Cryptography;
using XnetDsa.Impls.RawBytes;
using XnetDsa.Impls.Shared;

namespace XnetDsa.Impls
{
    internal class SECP256K1 : DsaAlgorithm
    {
        private static readonly string[] ALIASES = new string[]
        {
            "p256k1", "ansip256k1", "secp-256k1", 
        };

        /// <inheritdoc/>
        public override string Name => "secp256k1";

        /// <inheritdoc/>
        public override IEnumerable<string> Aliases => ALIASES;

        /// <inheritdoc/>
        public override int SizeOfKey => Secp256k1.PRIVKEY_LENGTH;

        /// <inheritdoc/>
        public override int SizeOfPubKey => Secp256k1.SERIALIZED_COMPRESSED_PUBKEY_LENGTH;

        /// <inheritdoc/>
        public override int SizeOfDigest => Secp256k1.HASH_LENGTH;

        /// <inheritdoc/>
        public override int SizeOfSign => Secp256k1.SIGNATURE_LENGTH;

        /// <inheritdoc/>
        public override IDsaDigest MakeDigest(Stream Stream)
        {
            var Data = SharedSha256.Exec(X => X.ComputeHash(Stream));
            return new BasicDsaDigest<SECP256K1>(this, Data);
        }

        /// <inheritdoc/>
        public override IDsaDigest MakeDigest(ArraySegment<byte> Buffer)
        {
            if (Buffer.Array is null)
                Buffer = Array.Empty<byte>();

            var Data = SharedSha256.Exec(
                X => X.ComputeHash(Buffer.Array, Buffer.Offset, Buffer.Count));

            return new BasicDsaDigest<SECP256K1>(this, Data);
        }

        /// <inheritdoc/>
        public override IDsaKey NewKey() => SharedSecp256k1.Exec(Secp =>
        {
            using var Rng = RandomNumberGenerator.Create();
            Span<byte> Key = stackalloc byte[SizeOfKey];
            Span<byte> Pub = stackalloc byte[Secp256k1.PUBKEY_LENGTH];

            while (true)
            {
                Rng.GetNonZeroBytes(Key);

                if (Secp.SecretKeyVerify(Key) == false)
                    continue;

                if (Secp.PublicKeyCreate(Pub, Key) == false)
                    continue;

                break;
            }

            return new BasicDsaKey<SECP256K1>(this, Key.ToArray());
        });

        /// <summary>
        /// Throw an exception if the key is invalid.
        /// </summary>
        /// <param name="Key"></param>
        /// <exception cref="ArgumentException"></exception>
        private static void ThrowIfKeyInvalid(IDsaKey Key)
        {
            if (Key is not BasicDsaKey<SECP256K1> || Key.Validity == false)
                throw new ArgumentException("the specified key is not supported.");
        }

        /// <summary>
        /// Throw an exception if the key is invalid.
        /// </summary>
        /// <param name="Key"></param>
        /// <exception cref="ArgumentException"></exception>
        private static void ThrowIfKeyInvalid(IDsaPubKey Key)
        {
            if (Key is not BasicDsaPubKey<SECP256K1> || Key.Validity == false)
                throw new ArgumentException("the specified public key is not supported.");
        }

        /// <summary>
        /// Throw an exception if the digest is invalid.
        /// </summary>
        /// <param name="Digest"></param>
        /// <exception cref="ArgumentException"></exception>
        private static void ThrowIfDigestInvalid(IDsaDigest Digest)
        {
            if (Digest is null || Digest.Validity == false)
                throw new ArgumentException("the digest is not supported for this algorithm.");
        }

        /// <summary>
        /// Throw an exception if signature is invalid.
        /// </summary>
        /// <param name="Sign"></param>
        /// <exception cref="ArgumentException"></exception>
        private static void ThrowIfSignInvalid(IDsaSign Sign)
        {
            if (Sign is not BasicDsaSign<SECP256K1> || Sign.Validity == false)
                throw new ArgumentException("the specified signature is not supported.");
        }

        /// <summary>
        /// Throw an exception about key corrupted.
        /// </summary>
        /// <exception cref="ArgumentException"></exception>
        private static void ThrowCorrupted()
        {
            throw new ArgumentException("the specified key is corrupted.");
        }

        /// <inheritdoc/>
        public override IDsaPubKey NewPubKey(IDsaKey Key) => SharedSecp256k1.Exec(Secp =>
        {
            ThrowIfKeyInvalid(Key);

            Span<byte> RawPub = stackalloc byte[Secp256k1.PUBKEY_LENGTH];
            Span<byte> RawKey = stackalloc byte[Key.Length];
            Key.CopyTo(RawKey);

            if (Secp.PublicKeyCreate(RawPub, RawKey) == false)
            {
                ThrowCorrupted();
                return null;
            }

            Span<byte> PubKey = stackalloc byte[SizeOfPubKey];
            if (Secp.PublicKeySerialize(PubKey, RawKey, Flags.SECP256K1_EC_COMPRESSED) == false)
            {
                ThrowCorrupted();
                return null;
            }

            return new BasicDsaPubKey<SECP256K1>(this, PubKey.ToArray());
        });

        /// <inheritdoc/>
        public override IDsaSign Sign(IDsaKey Key, IDsaDigest Digest)
        {
            ThrowIfKeyInvalid(Key);
            ThrowIfDigestInvalid(Digest);

            return SharedSecp256k1.Exec(Secp =>
            {
                Span<byte> PvtKey = stackalloc byte[SizeOfKey];
                Key.CopyTo(PvtKey);

                Span<byte> RawDig = stackalloc byte[SizeOfDigest];
                Digest.CopyTo(RawDig);

                Span<byte> Sign = stackalloc byte[SizeOfSign];
                if (Secp.Sign(Sign, RawDig, PvtKey) == false)
                    ThrowCorrupted();

                return new BasicDsaSign<SECP256K1>(this, Sign.ToArray());
            });
        }


        /// <inheritdoc/>
        public override bool Validate(IDsaKey Key)
        {
            ThrowIfKeyInvalid(Key);
            return SharedSecp256k1.Exec(Secp =>
            {
                Span<byte> PvtKey = stackalloc byte[SizeOfKey];
                Key.CopyTo(PvtKey);

                if (Secp.SecretKeyVerify(PvtKey) == false)
                    return false;

                return true;
            });
        }

        /// <inheritdoc/>
        public override bool ValidatePubKey(IDsaPubKey Key)
        {
            ThrowIfKeyInvalid(Key);
            return SharedSecp256k1.Exec(Secp =>
            {
                Span<byte> PubKey = stackalloc byte[SizeOfPubKey];
                Span<byte> RawPub = stackalloc byte[Secp256k1.PUBKEY_LENGTH];
                Key.CopyTo(PubKey);

                if (Secp.PublicKeyParse(RawPub, PubKey) == false)
                    return false;

                return true;
            });
        }

        /// <inheritdoc/>
        public override bool Verify(IDsaPubKey Key, IDsaSign Sign, IDsaDigest Digest)
        {
            ThrowIfKeyInvalid(Key);
            ThrowIfSignInvalid(Sign);
            ThrowIfDigestInvalid(Digest);

            return SharedSecp256k1.Exec(Secp =>
            {
                Span<byte> PubKey = stackalloc byte[SizeOfPubKey];
                Span<byte> RawPub = stackalloc byte[Secp256k1.PUBKEY_LENGTH];
                Key.CopyTo(PubKey);

                if (Secp.PublicKeyParse(RawPub, PubKey) == false)
                    return false;

                Span<byte> RawDig = stackalloc byte[SizeOfDigest];
                Digest.CopyTo(RawDig);

                Span<byte> Sign = stackalloc byte[SizeOfSign];
                Sign.CopyTo(Sign);

                return Secp.Verify(Sign, RawDig, RawPub);
            });
        }

        /// <inheritdoc/>
        public override IDsaDigest RestoreDigest(Span<byte> Buffer)
        {
            if (Buffer.Length != SizeOfDigest)
                return null;

            return new BasicDsaDigest<SECP256K1>(this, Buffer.ToArray());
        }

        /// <inheritdoc/>
        public override IDsaKey RestoreKey(Span<byte> Buffer)
        {
            if (Buffer.Length != SizeOfKey)
                return null;

            return new BasicDsaKey<SECP256K1>(this, Buffer.ToArray());
        }

        /// <inheritdoc/>
        public override IDsaPubKey RestorePubKey(Span<byte> Buffer)
        {
            if (Buffer.Length != SizeOfPubKey)
                return null;

            return new BasicDsaPubKey<SECP256K1>(this, Buffer.ToArray());
        }

        /// <inheritdoc/>
        public override IDsaSign RestoreSign(Span<byte> Buffer)
        {
            if (Buffer.Length != SizeOfSign)
                return null;

            return new BasicDsaSign<SECP256K1>(this, Buffer.ToArray());
        }
    }
}
