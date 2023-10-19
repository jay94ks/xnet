﻿using XnetDsa.Impls.Protocols;

namespace XnetDsa
{
    /// <summary>
    /// DSA extension methods.
    /// </summary>
    public static class DsaExtensions
    {
        /// <summary>
        /// Enable the DSA key pair. and if <paramref name="WithLayer"/> is true, the DSA layer for Xnet will be enabled.
        /// </summary>
        /// <typeparam name="TOptions"></typeparam>
        /// <param name="Options"></param>
        /// <param name="Key"></param>
        /// <param name="PubKey"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public static TOptions EnableDsaPacket<TOptions>(this TOptions Options, bool WithLayer, DsaKey Key, DsaPubKey PubKey = default) where TOptions : Xnet.Options
        {
            if (Key.Validity == false)
                throw new ArgumentException("the specified key is invalid.");

            if (PubKey.Validity == false)
                PubKey = Key.MakePubKey();

            var Check = DsaDigest.Make(Array.Empty<byte>(), Key.Algorithm);
            if (Key.Sign(Check).Verify(PubKey, Check) == false)
                throw new ArgumentException("the specified public key is invalid.");

            // --> add the DSA extender.
            Options.Extenders.Add(new DsaExtender()
            {
                Key = Key, 
                PubKey = PubKey,
                WithLayer = WithLayer
            });

            return Options;
        }

        /// <summary>
        /// Enable the DSA key pair without DSA layer.
        /// </summary>
        /// <typeparam name="TOptions"></typeparam>
        /// <param name="Options"></param>
        /// <param name="Key"></param>
        /// <param name="PubKey"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public static TOptions EnableDsaPacket<TOptions>(this TOptions Options, DsaKey Key, DsaPubKey PubKey = default) where TOptions : Xnet.Options => Options.EnableDsaPacket(false, Key, PubKey);

        /// <summary>
        /// Throw an exception if algorithm of specified objects are not same.
        /// </summary>
        /// <param name="RawBytes"></param>
        /// <exception cref="InvalidOperationException"></exception>
        private static void AssertAlgorithms(params IDsaRawBytes[] RawBytes)
        {
            var Counter = 0;
            for(var i = 0; i < RawBytes.Length - 1; ++i)
            {
                if (RawBytes[i] is null)
                    throw new ArgumentException("null argument specified.");

                if (RawBytes[i].Algorithm != RawBytes[i + 1].Algorithm)
                    break;

                Counter++;
            }

            if (RawBytes.Length > 0 && RawBytes[RawBytes.Length - 1] is null)
                throw new ArgumentException("null argument specified.");

            if (Counter == RawBytes.Length)
                return;

            throw new InvalidOperationException(
                "signature, public key and digest should be " +
                "generated by same algorithm for `verify method`.");
        }

        /// <summary>
        /// Make the public key as pair of the private key.
        /// </summary>
        /// <param name="Key"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static IDsaPubKey MakePubKey(this IDsaKey Key)
        {
            if (Key is null)
                throw new ArgumentNullException(nameof(Key));

            return Key.Algorithm.NewPubKey(Key);
        }

        /// <summary>
        /// Make the public key as pair of the private key.
        /// </summary>
        /// <param name="Key"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static DsaPubKey MakePubKey(this DsaKey Key) => new DsaPubKey(Key.ToRaw().MakePubKey());

        /// <summary>
        /// Verify the signature is valid or not.
        /// </summary>
        /// <param name="Sign"></param>
        /// <param name="PubKey"></param>
        /// <param name="Digest"></param>
        /// <returns></returns>
        public static bool Verify(this IDsaSign Sign, IDsaPubKey PubKey, IDsaDigest Digest)
        {
            AssertAlgorithms(Sign, PubKey, Digest);
            return Sign.Algorithm.Verify(PubKey, Sign, Digest);
        }

        /// <summary>
        /// Verify the signature is valid or not.
        /// </summary>
        /// <param name="Sign"></param>
        /// <param name="PubKey"></param>
        /// <param name="Digest"></param>
        /// <returns></returns>
        public static bool Verify(this DsaSign Sign, DsaPubKey PubKey, DsaDigest Digest) => Sign.ToRaw().Verify(PubKey.ToRaw(), Digest.ToRaw());

        /// <summary>
        /// Verify the signature is valid or not.
        /// </summary>
        /// <param name="Sign"></param>
        /// <param name="PubKey"></param>
        /// <param name="Digest"></param>
        /// <returns></returns>
        public static bool Verify(this DsaDigest Digest, DsaSign Sign, DsaPubKey PubKey) => Sign.ToRaw().Verify(PubKey.ToRaw(), Digest.ToRaw());

        /// <summary>
        /// Verify the signature is valid or not.
        /// </summary>
        /// <param name="Sign"></param>
        /// <param name="PubKey"></param>
        /// <param name="Digest"></param>
        /// <returns></returns>
        public static bool Verify(this DsaPubKey PubKey, DsaSign Sign, DsaDigest Digest) => Sign.ToRaw().Verify(PubKey.ToRaw(), Digest.ToRaw());

        /// <summary>
        /// Verify the signature is valid or not.
        /// </summary>
        /// <param name="Sign"></param>
        /// <param name="PubKey"></param>
        /// <param name="Digest"></param>
        /// <returns></returns>
        public static bool Verify(this IDsaDigest Digest, IDsaSign Sign, IDsaPubKey PubKey) => Sign.Verify(PubKey, Digest);

        /// <summary>
        /// Verify the signature is valid or not.
        /// </summary>
        /// <param name="Sign"></param>
        /// <param name="PubKey"></param>
        /// <param name="Digest"></param>
        /// <returns></returns>
        public static bool Verify(this IDsaPubKey PubKey, IDsaSign Sign, IDsaDigest Digest) => Sign.Verify(PubKey, Digest);

        /// <summary>
        /// Sign the digest using the private key.
        /// </summary>
        /// <param name="Key"></param>
        /// <param name="Digest"></param>
        /// <returns></returns>
        public static IDsaSign Sign(this IDsaKey Key, IDsaDigest Digest)
        {
            AssertAlgorithms(Key, Digest);
            return Key.Algorithm.Sign(Key, Digest);
        }

        /// <summary>
        /// Sign the digest using the private key.
        /// </summary>
        /// <param name="Key"></param>
        /// <param name="Digest"></param>
        /// <returns></returns>
        public static DsaSign Sign(this DsaKey Key, DsaDigest Digest) => new DsaSign(Key.ToRaw().Sign(Digest.ToRaw()));

        /// <summary>
        /// Sign the digest using the private key.
        /// </summary>
        /// <param name="Key"></param>
        /// <param name="Digest"></param>
        /// <returns></returns>
        public static DsaSign Sign(this DsaDigest Digest, DsaKey Key) => new DsaSign(Key.ToRaw().Sign(Digest.ToRaw()));

        /// <summary>
        /// Sign the digest using the private key.
        /// </summary>
        /// <param name="Key"></param>
        /// <param name="Digest"></param>
        /// <returns></returns>
        public static IDsaSign Sign(this IDsaDigest Digest, IDsaKey Key) => Key.Sign(Digest);

        /// <summary>
        /// Verify the digest with expected original data stream.
        /// </summary>
        /// <param name="Digest"></param>
        /// <param name="Stream"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static bool Verify(this IDsaDigest Digest, Stream Stream)
        {
            if (Digest is null)
                throw new ArgumentNullException(nameof(Digest));

            if (Stream is null)
                throw new ArgumentNullException(nameof(Stream));

            return Digest.Algorithm.MakeDigest(Stream).Equals(Digest);
        }

        /// <summary>
        /// Verify the digest with expected original data buffer.
        /// </summary>
        /// <param name="Digest"></param>
        /// <param name="Buffer"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static bool Verify(this IDsaDigest Digest, ArraySegment<byte> Buffer)
        {
            if (Digest is null)
                throw new ArgumentNullException(nameof(Digest));

            return Digest.Algorithm.MakeDigest(Buffer).Equals(Digest);
        }

        /// <summary>
        /// Verify the digest with expected original data stream.
        /// </summary>
        /// <param name="Digest"></param>
        /// <param name="Stream"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static bool Verify(this DsaDigest Digest, Stream Stream) => Digest.Verify(Stream);

        /// <summary>
        /// Verify the digest with expected original data buffer.
        /// </summary>
        /// <param name="Digest"></param>
        /// <param name="Buffer"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static bool Verify(this DsaDigest Digest, ArraySegment<byte> Buffer) => Digest.Verify(Buffer);

        /// <summary>
        /// Convert to byte array.
        /// </summary>
        /// <param name="Digest"></param>
        /// <returns></returns>
        public static byte[] ToRawBytes(this IDsaRawBytes Digest)
        {
            if (Digest is null)
                return Array.Empty<byte>();

            var Buffer = new byte[Digest.Length];
            Digest.CopyTo(Buffer);
            return Buffer;
        }

        /// <summary>
        /// Convert to byte array.
        /// </summary>
        /// <param name="Digest"></param>
        /// <returns></returns>
        public static byte[] ToRawBytes(this DsaDigest Digest) => Digest.ToRaw().ToRawBytes();

        /// <summary>
        /// Convert to byte array.
        /// </summary>
        /// <param name="Key"></param>
        /// <returns></returns>
        public static byte[] ToRawBytes(this DsaKey Key) => Key.ToRaw().ToRawBytes();

        /// <summary>
        /// Convert to byte array.
        /// </summary>
        /// <param name="Key"></param>
        /// <returns></returns>
        public static byte[] ToRawBytes(this DsaPubKey Key) => Key.ToRaw().ToRawBytes();

        /// <summary>
        /// Convert to byte array.
        /// </summary>
        /// <param name="Sign"></param>
        /// <returns></returns>
        public static byte[] ToRawBytes(this DsaSign Sign) => Sign.ToRaw().ToRawBytes();
    }

}