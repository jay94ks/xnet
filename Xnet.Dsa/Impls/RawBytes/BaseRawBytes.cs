namespace XnetDsa.Impls.RawBytes
{
    internal abstract class BaseRawBytes : IDsaRawBytes
    {
        private static readonly Dictionary<int, byte[]> ZEROS = new();
        private static readonly Dictionary<int, string> ZERO_HEXES = new();

        private readonly byte[] m_Data = null;
        private readonly string m_DataHex = null;

        /// <summary>
        /// Get the zero bytes.
        /// </summary>
        /// <param name="Length"></param>
        /// <returns></returns>
        public static byte[] ZeroBytes(int Length)
        {
            lock (ZEROS)
            {
                if (ZEROS.TryGetValue(Length, out var Zeros) == false)
                {
                    ZEROS[Length] = Zeros = new byte[Length];
                    ZERO_HEXES[Length] = Convert.ToHexString(Zeros);
                }

                return Zeros;
            }
        }

        /// <summary>
        /// Get the zero bytes.
        /// </summary>
        /// <param name="Length"></param>
        /// <returns></returns>
        public static string ZeroHex(int Length)
        {
            lock (ZEROS)
            {
                if (ZERO_HEXES.TryGetValue(Length, out var ZeroHex) == false)
                {
                    var ZeroBytes = new byte[Length];
                    ZEROS[Length] = ZeroBytes;
                    ZERO_HEXES[Length] = Convert.ToHexString(ZeroBytes);
                }

                return ZeroHex;
            }
        }

        /// <summary>
        /// Initialize a new <see cref="BaseRawBytes"/> instance.
        /// </summary>
        /// <param name="Algorithm"></param>
        /// <param name="Length"></param>
        public BaseRawBytes(DsaAlgorithm Algorithm, int Length)
        {
            this.Algorithm = Algorithm;
            this.Length = Length;

            m_Data = ZeroBytes(Length);
            m_DataHex = ZeroHex(Length);

            Validity = false;
        }

        /// <summary>
        /// Initialize a new <see cref="BaseRawBytes"/> instance.
        /// </summary>
        /// <param name="Algorithm"></param>
        /// <param name="Length"></param>
        public BaseRawBytes(DsaAlgorithm Algorithm, byte[] Data)
        {
            if (Data is null)
                throw new ArgumentNullException(nameof(Data));

            this.Algorithm = Algorithm;
            Validity = true;
            Length = Data.Length;
            m_Data = Data;
            m_DataHex = Convert.ToHexString(m_Data).ToLower();
        }

        /// <inheritdoc/>
        public DsaAlgorithm Algorithm { get; }

        /// <inheritdoc/>
        public virtual bool Validity { get; }

        /// <inheritdoc/>
        public int Length { get; }

        /// <inheritdoc/>
        public void CopyTo(Span<byte> Buffer) => m_Data.CopyTo(Buffer);

        /// <inheritdoc/>
        public string ToHex() => m_DataHex;
    }
}
