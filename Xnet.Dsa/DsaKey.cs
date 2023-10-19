using System.Diagnostics.CodeAnalysis;

namespace XnetDsa
{
    /// <summary>
    /// DSA key.
    /// This will use `<see cref="DsaAlgorithm.Default"/>` algorithm if nothing specified.
    /// </summary>
    public readonly struct DsaKey : IEquatable<DsaKey>, IEquatable<IDsaKey>
    {
        private readonly IDsaKey m_Key;

        /// <summary>
        /// Initialize a new <see cref="DsaKey"/> struct.
        /// </summary>
        public DsaKey() : this(null as IDsaKey) { }

        /// <summary>
        /// Initialize a new <see cref="DsaKey"/> struct.
        /// </summary>
        /// <param name="Digest"></param>
        public DsaKey(IDsaKey Digest) => m_Key = Digest;

        /// <summary>
        /// Initialize a new <see cref="DsaKey"/> struct.
        /// </summary>
        /// <param name="Digest"></param>
        public DsaKey(byte[] Digest) : this(Digest, DsaAlgorithm.Default) { }

        /// <summary>
        /// Initialize a new <see cref="DsaKey"/> struct.
        /// </summary>
        /// <param name="Digest"></param>
        /// <param name="Algorithm"></param>
        public DsaKey(byte[] Digest, DsaAlgorithm Algorithm) : this((Algorithm ?? DsaAlgorithm.Default).RestoreKey(Digest)) { }

        /// <summary>
        /// Make a key using default algorithm.
        /// </summary>
        /// <returns></returns>
        public static DsaKey Make() => Make(DsaAlgorithm.Default);

        /// <summary>
        /// Make a key using specified algorithm.
        /// </summary>
        /// <param name="Algorithm"></param>
        /// <returns></returns>
        public static DsaKey Make(DsaAlgorithm Algorithm) => new DsaKey((Algorithm ?? DsaAlgorithm.Default).NewKey());

        /// <summary>
        /// Algorithm.
        /// </summary>
        public DsaAlgorithm Algorithm => m_Key != null ? m_Key.Algorithm : DsaAlgorithm.Default;

        /// <summary>
        /// Indicates whether this is valid or not.
        /// </summary>
        public bool Validity => ToRaw() != null && ToRaw().Validity;

        /// <summary>
        /// Get <see cref="IDsaKey"/> instance.
        /// </summary>
        /// <returns></returns>
        public IDsaKey ToRaw() => m_Key;

        /// <inheritdoc/>
        public bool Equals(IDsaKey Other)
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
        public bool Equals(DsaKey Other) => Equals(Other.m_Key);

        /// <inheritdoc/>
        public override bool Equals([NotNullWhen(true)] object Input)
        {
            switch (Input)
            {
                case IDsaKey Raw: return Equals(Raw);
                case DsaKey Other: return Equals(Other);
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
        public static bool operator ==(DsaKey Left, DsaKey Right) => Left.Equals(Right) == true;

        /// <summary>
        /// `!=` operator.
        /// </summary>
        /// <param name="Left"></param>
        /// <param name="Right"></param>
        /// <returns></returns>
        public static bool operator !=(DsaKey Left, DsaKey Right) => Left.Equals(Right) == false;

        /// <summary>
        /// `==` operator.
        /// </summary>
        /// <param name="Left"></param>
        /// <param name="Right"></param>
        /// <returns></returns>
        public static bool operator ==(DsaKey Left, IDsaKey Right) => Left.Equals(Right) == true;

        /// <summary>
        /// `!=` operator.
        /// </summary>
        /// <param name="Left"></param>
        /// <param name="Right"></param>
        /// <returns></returns>
        public static bool operator !=(DsaKey Left, IDsaKey Right) => Left.Equals(Right) == false;

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
        /// Encode <see cref="DsaKey"/> into <see cref="BinaryWriter"/>.
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
        /// Decode <see cref="DsaKey"/> from <see cref="BinaryReader"/>.
        /// </summary>
        /// <param name="Reader"></param>
        /// <returns></returns>
        public static DsaKey Decode(BinaryReader Reader)
        {
            var Name = Reader.ReadString();
            if (string.IsNullOrWhiteSpace(Name))
                return default;

            var Algorithm = DsaAlgorithm.GetAlgorithm(Name);
            var RawObject = Algorithm.Decode(Reader);
            if (RawObject is not IDsaKey Digest)
                return default;

            return new DsaKey(Digest);
        }
    }
}