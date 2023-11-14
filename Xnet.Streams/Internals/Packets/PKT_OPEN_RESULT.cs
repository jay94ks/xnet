namespace XnetStreams.Internals.Packets
{
    /// <summary>
    /// Stream Open result.
    /// </summary>
    internal class PKT_OPEN_RESULT : PKT_BASE_RESULT
    {
        /// <summary>
        /// Read timeout, negative if disabled.
        /// </summary>
        public int ReadTimeout { get; set; } = -1;

        /// <summary>
        /// Read timeout, negative if disabled.
        /// </summary>
        public int WriteTimeout { get; set; } = -1;

        /// <summary>
        /// Indicates whether the stream supports `Seek` or not.
        /// </summary>
        public bool CanSeek { get; set; }

        /// <summary>
        /// Indicates whether the stream supports `Write` or not.
        /// </summary>
        public bool CanWrite { get; set; }

        /// <summary>
        /// Indicates whether the stream supports `Read` or not.
        /// </summary>
        public bool CanRead { get; set; }

        /// <summary>
        /// Cursor value.
        /// If seek is false, this will not be valid.
        /// </summary>
        public long Cursor { get; set; } = -1;

        /// <summary>
        /// Length value.
        /// If seek is false, this will not be valid.
        /// </summary>
        public long Length { get; set; } = -1;

        /// <inheritdoc/>
        protected override void Decode(BinaryReader Reader)
        {
            base.Decode(Reader);
            ReadTimeout = Reader.Read7BitEncodedInt();
            WriteTimeout = Reader.Read7BitEncodedInt();
            CanSeek = Reader.ReadByte() != byte.MinValue;
            CanRead = Reader.ReadByte() != byte.MinValue;
            CanWrite = Reader.ReadByte() != byte.MinValue;
            Length = Reader.Read7BitEncodedInt64();
            Cursor = Reader.Read7BitEncodedInt64();
        }

        /// <inheritdoc/>
        protected override void Encode(BinaryWriter Writer)
        {
            base.Encode(Writer);
            Writer.Write7BitEncodedInt(ReadTimeout);
            Writer.Write7BitEncodedInt(WriteTimeout);
            Writer.Write(CanSeek ? byte.MaxValue : byte.MinValue);
            Writer.Write(CanRead ? byte.MaxValue : byte.MinValue);
            Writer.Write(CanWrite ? byte.MaxValue : byte.MinValue);
            Writer.Write7BitEncodedInt64(Length);
            Writer.Write7BitEncodedInt64(Cursor);
        }
    }
}
