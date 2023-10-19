using System.Diagnostics.CodeAnalysis;

namespace XnetDsa
{
    /// <summary>
    /// DSA digest.
    /// This will use `<see cref="DsaAlgorithm.Default"/>` algorithm if nothing specified.
    /// </summary>
    public readonly struct DsaDigest : IEquatable<DsaDigest>, IEquatable<IDsaDigest>
    {
        private readonly IDsaDigest m_Digest;

        /// <summary>
        /// Initialize a new <see cref="DsaDigest"/> struct.
        /// </summary>
        public DsaDigest() : this(null as IDsaDigest) { }

        /// <summary>
        /// Initialize a new <see cref="DsaDigest"/> struct.
        /// </summary>
        /// <param name="Digest"></param>
        public DsaDigest(IDsaDigest Digest) => m_Digest = Digest;

        /// <summary>
        /// Initialize a new <see cref="DsaDigest"/> struct.
        /// </summary>
        /// <param name="Digest"></param>
        public DsaDigest(byte[] Digest) : this(Digest, DsaAlgorithm.Default) { }

        /// <summary>
        /// Initialize a new <see cref="DsaDigest"/> struct.
        /// </summary>
        /// <param name="Digest"></param>
        /// <param name="Algorithm"></param>
        public DsaDigest(byte[] Digest, DsaAlgorithm Algorithm) : this((Algorithm ?? DsaAlgorithm.Default).RestoreDigest(Digest)) { }

        /// <summary>
        /// Make a digest using default algorithm.
        /// </summary>
        /// <param name="Stream"></param>
        /// <returns></returns>
        public static DsaDigest Make(Stream Stream) => Make(Stream, DsaAlgorithm.Default);

        /// <summary>
        /// Make a digest using default algorithm.
        /// </summary>
        /// <param name="Buffer"></param>
        /// <returns></returns>
        public static DsaDigest Make(ArraySegment<byte> Buffer) => Make(Buffer, DsaAlgorithm.Default);

        /// <summary>
        /// Make a digest using specified algorithm.
        /// </summary>
        /// <param name="Stream"></param>
        /// <param name="Algorithm"></param>
        /// <returns></returns>
        public static DsaDigest Make(Stream Stream, DsaAlgorithm Algorithm) => new DsaDigest((Algorithm ?? DsaAlgorithm.Default).MakeDigest(Stream));

        /// <summary>
        /// Make a digest using specified algorithm.
        /// </summary>
        /// <param name="Buffer"></param>
        /// <param name="Algorithm"></param>
        /// <returns></returns>
        public static DsaDigest Make(ArraySegment<byte> Buffer, DsaAlgorithm Algorithm) => new DsaDigest((Algorithm ?? DsaAlgorithm.Default).MakeDigest(Buffer));

        /// <summary>
        /// Algorithm.
        /// </summary>
        public DsaAlgorithm Algorithm => m_Digest != null ? m_Digest.Algorithm : DsaAlgorithm.Default;

        /// <summary>
        /// Indicates whether this is valid or not.
        /// </summary>
        public bool Validity => ToRaw() != null && ToRaw().Validity;

        /// <summary>
        /// Get <see cref="IDsaDigest"/> instance.
        /// </summary>
        /// <returns></returns>
        public IDsaDigest ToRaw() => m_Digest;

        /// <inheritdoc/>
        public bool Equals(IDsaDigest Other)
        {
            if (ReferenceEquals(m_Digest, Other))
                return true;

            if (ReferenceEquals(m_Digest, null))
                return ReferenceEquals(Other, null);

            if (ReferenceEquals(Other, null))
                return false;

            if (Algorithm != Other.Algorithm) 
                return false;

            var Left = m_Digest.ToHex() ?? string.Empty;
            var Right = Other.ToHex() ?? string.Empty;
            return Left.Equals(Right, StringComparison.OrdinalIgnoreCase);
        }

        /// <inheritdoc/>
        public bool Equals(DsaDigest Other) => Equals(Other.m_Digest);

        /// <inheritdoc/>
        public override bool Equals([NotNullWhen(true)] object Input)
        {
            switch (Input)
            {
                case IDsaDigest Raw: return Equals(Raw);
                case DsaDigest Other: return Equals(Other);
                default:
                    break;
            }

            return base.Equals(Input);
        }

        /// <summary>
        /// `==` operator.
        /// </summary>
        /// <param name="Left"></param>
        /// <param name="Right"></param>
        /// <returns></returns>
        public static bool operator ==(DsaDigest Left, DsaDigest Right) => Left.Equals(Right) == true;

        /// <summary>
        /// `!=` operator.
        /// </summary>
        /// <param name="Left"></param>
        /// <param name="Right"></param>
        /// <returns></returns>
        public static bool operator !=(DsaDigest Left, DsaDigest Right) => Left.Equals(Right) == false;

        /// <summary>
        /// `==` operator.
        /// </summary>
        /// <param name="Left"></param>
        /// <param name="Right"></param>
        /// <returns></returns>
        public static bool operator ==(DsaDigest Left, IDsaDigest Right) => Left.Equals(Right) == true;

        /// <summary>
        /// `!=` operator.
        /// </summary>
        /// <param name="Left"></param>
        /// <param name="Right"></param>
        /// <returns></returns>
        public static bool operator !=(DsaDigest Left, IDsaDigest Right) => Left.Equals(Right) == false;

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            if (m_Digest is null)
                return string.Empty.GetHashCode();

            return (m_Digest.ToHex() ?? string.Empty).GetHashCode();
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            if (m_Digest is null)
                return string.Empty;

            return m_Digest.ToHex() ?? string.Empty;
        }

        /// <summary>
        /// Encode <see cref="DsaDigest"/> into <see cref="BinaryWriter"/>.
        /// </summary>
        /// <param name="Writer"></param>
        /// <returns></returns>
        public void Encode(BinaryWriter Writer)
        {
            if (m_Digest is null)
            {
                Writer.Write(string.Empty);
                return;
            }

            Writer.Write(Algorithm.Name);
            Algorithm.Encode(m_Digest, Writer);
        }

        /// <summary>
        /// Decode <see cref="DsaDigest"/> from <see cref="BinaryReader"/>.
        /// </summary>
        /// <param name="Reader"></param>
        /// <returns></returns>
        public static DsaDigest Decode(BinaryReader Reader)
        {
            var Name = Reader.ReadString();
            if (string.IsNullOrWhiteSpace(Name))
                return default;

            var Algorithm = DsaAlgorithm.GetAlgorithm(Name);
            var RawObject = Algorithm.Decode(Reader);
            if (RawObject is not IDsaDigest Digest)
                return default;

            return new DsaDigest(Digest);
        }
    }
}