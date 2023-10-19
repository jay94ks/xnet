using System.Diagnostics.CodeAnalysis;

namespace XnetDsa
{
    /// <summary>
    /// DSA key.
    /// This will use `<see cref="DsaAlgorithm.Default"/>` algorithm if nothing specified.
    /// </summary>
    public readonly struct DsaSign : IEquatable<DsaSign>, IEquatable<IDsaSign>
    {
        private readonly IDsaSign m_Sign;

        /// <summary>
        /// Initialize a new <see cref="DsaSign"/> struct.
        /// </summary>
        public DsaSign() : this(null as IDsaSign) { }

        /// <summary>
        /// Initialize a new <see cref="DsaSign"/> struct.
        /// </summary>
        /// <param name="Digest"></param>
        public DsaSign(IDsaSign Digest) => m_Sign = Digest;

        /// <summary>
        /// Initialize a new <see cref="DsaSign"/> struct.
        /// </summary>
        /// <param name="Digest"></param>
        public DsaSign(byte[] Digest) : this(Digest, DsaAlgorithm.Default) { }

        /// <summary>
        /// Initialize a new <see cref="DsaSign"/> struct.
        /// </summary>
        /// <param name="Digest"></param>
        /// <param name="Algorithm"></param>
        public DsaSign(byte[] Digest, DsaAlgorithm Algorithm) : this((Algorithm ?? DsaAlgorithm.Default).RestoreSign(Digest)) { }

        /// <summary>
        /// Algorithm.
        /// </summary>
        public DsaAlgorithm Algorithm => m_Sign != null ? m_Sign.Algorithm : DsaAlgorithm.Default;

        /// <summary>
        /// Indicates whether this is valid or not.
        /// </summary>
        public bool Validity => ToRaw() != null && ToRaw().Validity;

        /// <summary>
        /// Get <see cref="IDsaSign"/> instance.
        /// </summary>
        /// <returns></returns>
        public IDsaSign ToRaw() => m_Sign;

        /// <inheritdoc/>
        public bool Equals(IDsaSign Other)
        {
            if (ReferenceEquals(m_Sign, Other))
                return true;

            if (ReferenceEquals(m_Sign, null))
                return ReferenceEquals(Other, null);

            if (ReferenceEquals(Other, null))
                return false;

            if (Algorithm != Other.Algorithm)
                return false;

            var Left = m_Sign.ToHex() ?? string.Empty;
            var Right = Other.ToHex() ?? string.Empty;
            return Left.Equals(Right, StringComparison.OrdinalIgnoreCase);
        }

        /// <inheritdoc/>
        public bool Equals(DsaSign Other) => Equals(Other.m_Sign);

        /// <inheritdoc/>
        public override bool Equals([NotNullWhen(true)] object Input)
        {
            switch (Input)
            {
                case IDsaSign Raw: return Equals(Raw);
                case DsaSign Other: return Equals(Other);
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
        public static bool operator ==(DsaSign Left, DsaSign Right) => Left.Equals(Right) == true;

        /// <summary>
        /// `!=` operator.
        /// </summary>
        /// <param name="Left"></param>
        /// <param name="Right"></param>
        /// <returns></returns>
        public static bool operator !=(DsaSign Left, DsaSign Right) => Left.Equals(Right) == false;

        /// <summary>
        /// `==` operator.
        /// </summary>
        /// <param name="Left"></param>
        /// <param name="Right"></param>
        /// <returns></returns>
        public static bool operator ==(DsaSign Left, IDsaSign Right) => Left.Equals(Right) == true;

        /// <summary>
        /// `!=` operator.
        /// </summary>
        /// <param name="Left"></param>
        /// <param name="Right"></param>
        /// <returns></returns>
        public static bool operator !=(DsaSign Left, IDsaSign Right) => Left.Equals(Right) == false;

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            if (m_Sign is null)
                return string.Empty.GetHashCode();

            return (m_Sign.ToHex() ?? string.Empty).GetHashCode();
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            if (m_Sign is null)
                return string.Empty;

            return m_Sign.ToHex() ?? string.Empty;
        }

        /// <summary>
        /// Encode <see cref="DsaSign"/> into <see cref="BinaryWriter"/>.
        /// </summary>
        /// <param name="Writer"></param>
        /// <returns></returns>
        public void Encode(BinaryWriter Writer)
        {
            if (m_Sign is null)
            {
                Writer.Write(string.Empty);
                return;
            }

            Writer.Write(Algorithm.Name);
            Algorithm.Encode(m_Sign, Writer);
        }

        /// <summary>
        /// Decode <see cref="DsaSign"/> from <see cref="BinaryReader"/>.
        /// </summary>
        /// <param name="Reader"></param>
        /// <returns></returns>
        public static DsaSign Decode(BinaryReader Reader)
        {
            var Name = Reader.ReadString();
            if (string.IsNullOrWhiteSpace(Name))
                return default;

            var Algorithm = DsaAlgorithm.GetAlgorithm(Name);
            var RawObject = Algorithm.Decode(Reader);
            if (RawObject is not IDsaSign Digest)
                return default;

            return new DsaSign(Digest);
        }
    }
}