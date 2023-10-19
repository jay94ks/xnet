using System.Diagnostics.CodeAnalysis;

namespace XnetDsa
{
    /// <summary>
    /// DSA key.
    /// This will use `<see cref="DsaAlgorithm.Default"/>` algorithm if nothing specified.
    /// </summary>
    public readonly struct DsaPubKey : IEquatable<DsaPubKey>, IEquatable<IDsaPubKey>
    {
        private readonly IDsaPubKey m_Key;

        /// <summary>
        /// Initialize a new <see cref="DsaPubKey"/> struct.
        /// </summary>
        public DsaPubKey() : this(null as IDsaPubKey) { }

        /// <summary>
        /// Initialize a new <see cref="DsaPubKey"/> struct.
        /// </summary>
        /// <param name="Digest"></param>
        public DsaPubKey(IDsaPubKey Digest) => m_Key = Digest;

        /// <summary>
        /// Initialize a new <see cref="DsaPubKey"/> struct.
        /// </summary>
        /// <param name="Digest"></param>
        public DsaPubKey(byte[] Digest) : this(Digest, DsaAlgorithm.Default) { }

        /// <summary>
        /// Initialize a new <see cref="DsaPubKey"/> struct.
        /// </summary>
        /// <param name="Digest"></param>
        /// <param name="Algorithm"></param>
        public DsaPubKey(byte[] Digest, DsaAlgorithm Algorithm) : this((Algorithm ?? DsaAlgorithm.Default).RestorePubKey(Digest)) { }

        /// <summary>
        /// Algorithm.
        /// </summary>
        public DsaAlgorithm Algorithm => m_Key != null ? m_Key.Algorithm : DsaAlgorithm.Default;

        /// <summary>
        /// Indicates whether this is valid or not.
        /// </summary>
        public bool Validity => ToRaw() != null && ToRaw().Validity;

        /// <summary>
        /// Get <see cref="IDsaPubKey"/> instance.
        /// </summary>
        /// <returns></returns>
        public IDsaPubKey ToRaw() => m_Key;

        /// <inheritdoc/>
        public bool Equals(IDsaPubKey Other)
        {
            if (ReferenceEquals(m_Key, Other))
                return true;

            if (ReferenceEquals(m_Key, null))
                return ReferenceEquals(Other, null);

            if (ReferenceEquals(Other, null))
                return false;

            if (Algorithm != Other.Algorithm)
                return false;

            var Left = m_Key.ToHex() ?? string.Empty;
            var Right = Other.ToHex() ?? string.Empty;
            return Left.Equals(Right, StringComparison.OrdinalIgnoreCase);
        }

        /// <inheritdoc/>
        public bool Equals(DsaPubKey Other) => Equals(Other.m_Key);

        /// <inheritdoc/>
        public override bool Equals([NotNullWhen(true)] object Input)
        {
            switch (Input)
            {
                case IDsaPubKey Raw: return Equals(Raw);
                case DsaPubKey Other: return Equals(Other);
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
        public static bool operator ==(DsaPubKey Left, DsaPubKey Right) => Left.Equals(Right) == true;

        /// <summary>
        /// `!=` operator.
        /// </summary>
        /// <param name="Left"></param>
        /// <param name="Right"></param>
        /// <returns></returns>
        public static bool operator !=(DsaPubKey Left, DsaPubKey Right) => Left.Equals(Right) == false;

        /// <summary>
        /// `==` operator.
        /// </summary>
        /// <param name="Left"></param>
        /// <param name="Right"></param>
        /// <returns></returns>
        public static bool operator ==(DsaPubKey Left, IDsaPubKey Right) => Left.Equals(Right) == true;

        /// <summary>
        /// `!=` operator.
        /// </summary>
        /// <param name="Left"></param>
        /// <param name="Right"></param>
        /// <returns></returns>
        public static bool operator !=(DsaPubKey Left, IDsaPubKey Right) => Left.Equals(Right) == false;

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            if (m_Key is null)
                return string.Empty.GetHashCode();

            return (m_Key.ToHex() ?? string.Empty).GetHashCode();
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            if (m_Key is null)
                return string.Empty;

            return m_Key.ToHex() ?? string.Empty;
        }

        /// <summary>
        /// Encode <see cref="DsaPubKey"/> into <see cref="BinaryWriter"/>.
        /// </summary>
        /// <param name="Writer"></param>
        /// <returns></returns>
        public void Encode(BinaryWriter Writer)
        {
            if (m_Key is null)
            {
                Writer.Write(string.Empty);
                return;
            }

            Writer.Write(Algorithm.Name);
            Algorithm.Encode(m_Key, Writer);
        }

        /// <summary>
        /// Decode <see cref="DsaPubKey"/> from <see cref="BinaryReader"/>.
        /// </summary>
        /// <param name="Reader"></param>
        /// <returns></returns>
        public static DsaPubKey Decode(BinaryReader Reader)
        {
            var Name = Reader.ReadString();
            if (string.IsNullOrWhiteSpace(Name))
                return default;

            var Algorithm = DsaAlgorithm.GetAlgorithm(Name);
            var RawObject = Algorithm.Decode(Reader);
            if (RawObject is not IDsaPubKey Digest)
                return default;

            return new DsaPubKey(Digest);
        }
    }
}