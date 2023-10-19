using XnetDsa.Impls;

namespace XnetDsa
{
    /// <summary>
    /// Base class of DSA algorithm.
    /// </summary>
    public abstract class DsaAlgorithm
    {
        private static readonly AsyncLocal<DsaAlgorithm> DEFAULT = new();
        private static readonly List<DsaAlgorithm> REGISTRY = new();
        private static readonly DsaAlgorithm[] BUILTINS = new DsaAlgorithm[]
        {
            new SECP256K1(),
        };

        // --
        private static DsaAlgorithm[] REGISTRY_SNAPSHOT = null;

        /// <summary>
        /// Get or set the default DSA algorithm that will be used for library wide.
        /// If set null here, this will restore to be hardcorded default.
        /// </summary>
        public static DsaAlgorithm Default
        {
            get => DEFAULT.Value ?? BUILTINS.FirstOrDefault();
            set => DEFAULT.Value = value;
        }

        /// <summary>
        /// Test whether algorithm exists or not.
        /// </summary>
        /// <param name="Names"></param>
        /// <returns></returns>
        public static bool CanSupport(params string[] Names) => GetAlgorithm(Names) != null;

        /// <summary>
        /// Test whether algorithm exists or not.
        /// </summary>
        /// <param name="Names"></param>
        /// <returns></returns>
        public static bool CanSupport(IEnumerable<string> Names) => GetAlgorithm(Names) != null;

        /// <summary>
        /// Register an algorithm to registry.
        /// </summary>
        /// <param name="Algorithm"></param>
        /// <returns></returns>
        public static bool Register(DsaAlgorithm Algorithm)
        {
            if (Algorithm is null)
                throw new ArgumentNullException(nameof(Algorithm));

            lock (REGISTRY)
            {
                if (CanSupport(Algorithm.Name))
                    return false;

                if (CanSupport(Algorithm.Aliases))
                    return false;

                REGISTRY.Add(Algorithm);
                REGISTRY_SNAPSHOT = null;
            }

            return true;
        }

        /// <summary>
        /// Unregister an algorithm from registry.
        /// </summary>
        /// <param name="Algorithm"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static bool Unregister(DsaAlgorithm Algorithm)
        {
            if (Algorithm is null)
                throw new ArgumentNullException(nameof(Algorithm));

            lock (REGISTRY)
            {
                var Index = REGISTRY.IndexOf(Algorithm);
                if (Index < 0)
                    return false;

                if (REGISTRY_SNAPSHOT != null)
                    REGISTRY_SNAPSHOT[Index] = null;

                REGISTRY.RemoveAt(Index);
            }

            return true;
        }

        /// <summary>
        /// Get an algorithm by its name.
        /// If multiple names are specified,
        /// they are used to select alternatives if missing.
        /// </summary>
        /// <param name="Names"></param>
        /// <returns></returns>
        public static DsaAlgorithm GetAlgorithm(params string[] Names) => GetAlgorithm(Names.AsEnumerable());

        /// <summary>
        /// Get an algorithm by its name.
        /// If multiple names are specified,
        /// they are used to select alternatives if missing.
        /// </summary>
        /// <param name="Names"></param>
        /// <returns></returns>
        public static DsaAlgorithm GetAlgorithm(IEnumerable<string> Names)
        {
            foreach (var Name in Names)
            {
                if (string.IsNullOrWhiteSpace(Name))
                    continue;

                var Selected = GetExactAlgorithm(Name);
                if (Selected is null)
                    continue;

                return Selected;
            }

            return null;
        }

        /// <summary>
        /// Get an exact algorithm bt its name.
        /// </summary>
        /// <param name="Name"></param>
        /// <returns></returns>
        private static DsaAlgorithm GetExactAlgorithm(string Name)
        {
            foreach (var Each in BUILTINS)
            {
                if (Match(Each, Name) == true)
                    return Each;
            }

            foreach (var Each in RegistrySnapshot())
            {
                if (Each is null)
                    continue;

                if (Match(Each, Name) == true)
                    return Each;
            }

            return null;
        }

        /// <summary>
        /// Match algorithm with name.
        /// </summary>
        /// <param name="Algorithm"></param>
        /// <param name="Name"></param>
        /// <returns></returns>
        private static bool Match(DsaAlgorithm Algorithm, string Name)
        {
            if (Algorithm.Name.Equals(Name, StringComparison.OrdinalIgnoreCase))
                return true;

            var Alias = Algorithm.Aliases.FirstOrDefault(
                X => X.Equals(Name, StringComparison.OrdinalIgnoreCase));

            if (string.IsNullOrWhiteSpace(Alias) == false)
                return true;

            return false;
        }

        /// <summary>
        /// Get the registry snapshot.
        /// </summary>
        /// <returns></returns>
        private static DsaAlgorithm[] RegistrySnapshot()
        {
            lock (REGISTRY)
            {
                if (REGISTRY_SNAPSHOT is null)
                    REGISTRY_SNAPSHOT = REGISTRY.ToArray();

                return REGISTRY_SNAPSHOT;
            }
        }

        /// <summary>
        /// Name of algorithm
        /// </summary>
        public abstract string Name { get; }

        /// <summary>
        /// Aliases of algorithm.
        /// </summary>
        public abstract IEnumerable<string> Aliases { get; }

        /// <summary>
        /// Size of <see cref="IDsaKey"/>.
        /// </summary>
        public abstract int SizeOfKey { get; }

        /// <summary>
        /// Size of <see cref="IDsaPubKey"/>.
        /// </summary>
        public abstract int SizeOfPubKey { get; }

        /// <summary>
        /// Size of <see cref="IDsaDigest"/>.
        /// </summary>
        public abstract int SizeOfDigest { get; }

        /// <summary>
        /// Size of <see cref="IDsaSign"/>.
        /// </summary>
        public abstract int SizeOfSign { get; }

        /// <summary>
        /// Make a DSA digest from stream.
        /// </summary>
        /// <param name="Stream"></param>
        /// <returns></returns>
        public abstract IDsaDigest MakeDigest(Stream Stream);

        /// <summary>
        /// Make a DSA digest from array segment.
        /// </summary>
        /// <param name="Buffer"></param>
        /// <returns></returns>
        public abstract IDsaDigest MakeDigest(ArraySegment<byte> Buffer);

        /// <summary>
        /// Create a new DSA secret key.
        /// </summary>
        /// <returns></returns>
        public abstract IDsaKey NewKey();

        /// <summary>
        /// Validate the DSA secret key.
        /// </summary>
        /// <param name="Key"></param>
        /// <returns></returns>
        public abstract bool Validate(IDsaKey Key);

        /// <summary>
        /// Create a new DSA public key from secret key.
        /// </summary>
        /// <param name="Key"></param>
        /// <returns></returns>
        public abstract IDsaPubKey NewPubKey(IDsaKey Key);

        /// <summary>
        /// Validate the DSA public key.
        /// </summary>
        /// <param name="Key"></param>
        /// <returns></returns>
        public abstract bool ValidatePubKey(IDsaPubKey Key);

        /// <summary>
        /// Sign the input digest using the DSA key.
        /// </summary>
        /// <param name="Key"></param>
        /// <param name="Digest"></param>
        /// <returns></returns>
        public abstract IDsaSign Sign(IDsaKey Key, IDsaDigest Digest);

        /// <summary>
        /// Verify the input signature with digest using DSA public key.
        /// </summary>
        /// <param name="Key"></param>
        /// <param name="Sign"></param>
        /// <param name="Digest"></param>
        /// <returns></returns>
        public abstract bool Verify(IDsaPubKey Key, IDsaSign Sign, IDsaDigest Digest);

        /// <summary>
        /// Encode the <paramref name="RawBytes"/> to <paramref name="Writer"/>.
        /// </summary>
        /// <param name="RawBytes"></param>
        /// <param name="Writer"></param>
        public void Encode(IDsaRawBytes RawBytes, BinaryWriter Writer)
        {
            if (RawBytes is null)
                throw new ArgumentNullException(nameof(RawBytes));

            if (RawBytes.Algorithm != this)
                throw new NotSupportedException($"the object is not supported by this algorithm.");

            Span<byte> Buffer = stackalloc byte[RawBytes.Length];

            // --> copy raw bytes to the buffer.
            RawBytes.CopyTo(Buffer);
            switch(RawBytes)
            {
                case IDsaKey:
                    Writer.Write((byte)0x01);
                    break;

                case IDsaPubKey:
                    Writer.Write((byte)0x02);
                    break;

                case IDsaDigest:
                    Writer.Write((byte)0x03);
                    break;

                case IDsaSign:
                    Writer.Write((byte)0x04);
                    break;

                default:
                    Writer.Write(byte.MinValue);
                    Writer.Write7BitEncodedInt(0);
                    return;
            }

            // --> write raw bytes.
            Writer.Write7BitEncodedInt(Buffer.Length);
            Writer.Write(Buffer);
        }

        /// <summary>
        /// Decode the <see cref="IDsaRawBytes"/> from <paramref name="Reader"/>.
        /// </summary>
        /// <param name="Reader"></param>
        /// <returns></returns>
        public IDsaRawBytes Decode(BinaryReader Reader)
        {
            var Type = Reader.ReadByte();
            var Length = Reader.Read7BitEncodedInt();
            var Data = Length > 0 
                ? Reader.ReadBytes(Length)
                : Array.Empty<byte>();

            IDsaRawBytes Return;
            switch (Type)
            {
                case 0x01: /* Key. */
                    Return = RestoreKey(Data);
                    break;

                case 0x02: /* PubKey. */
                    Return = RestorePubKey(Data);
                    break;

                case 0x03: /* Digest. */
                    Return = RestoreDigest(Data);
                    break;

                case 0x04: /* Sign. */
                    Return = RestoreSign(Data);
                    break;

                default: // --> not supported.
                    return null;
            }

            if (Return is null || Return.Validity == false)
                return null;

            return Return;
        }

        /// <summary>
        /// Restore the DSA secret key from the buffer.
        /// </summary>
        /// <param name="Buffer"></param>
        /// <returns></returns>
        public abstract IDsaKey RestoreKey(Span<byte> Buffer);

        /// <summary>
        /// Restore the DSA public key from the buffer.
        /// </summary>
        /// <param name="Buffer"></param>
        /// <returns></returns>
        public abstract IDsaPubKey RestorePubKey(Span<byte> Buffer);

        /// <summary>
        /// Restore the DSA digest from the buffer.
        /// </summary>
        /// <param name="Buffer"></param>
        /// <returns></returns>
        public abstract IDsaDigest RestoreDigest(Span<byte> Buffer);

        /// <summary>
        /// Restore the DSA signature from the buffer.
        /// </summary>
        /// <param name="Buffer"></param>
        /// <returns></returns>
        public abstract IDsaSign RestoreSign(Span<byte> Buffer);
    }
}