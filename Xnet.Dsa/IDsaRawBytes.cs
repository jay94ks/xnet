namespace XnetDsa
{
    /// <summary>
    /// DSA raw bytes container interface.
    /// </summary>
    public interface IDsaRawBytes
    {
        /// <summary>
        /// Algorithm interface.
        /// </summary>
        DsaAlgorithm Algorithm { get; }

        /// <summary>
        /// Indicates whether this object is valid or not.
        /// </summary>
        bool Validity { get; }

        /// <summary>
        /// Length in bytes.
        /// </summary>
        int Length { get; }

        /// <summary>
        /// Copy raw bytes to the buffer.
        /// </summary>
        /// <param name="Buffer"></param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        void CopyTo(Span<byte> Buffer);

        /// <summary>
        /// Get the hex string of data.
        /// </summary>
        /// <returns></returns>
        string ToHex();
    }
}