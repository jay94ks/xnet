namespace XnetStreams.Internals.Packets
{
    /// <summary>
    /// Query result.
    /// </summary>
    internal class PKT_QUERY_RESULT : PKT_BASE_RESULT
    {
        /// <summary>
        /// Metadata.
        /// </summary>
        public StreamMetadata? Metadata { get; set; }

        /// <inheritdoc/>
        protected override void Encode(BinaryWriter Writer)
        {
            base.Encode(Writer);
            if (Metadata.HasValue == false)
            {
                Writer.Write(byte.MinValue);
                return;
            }

            var Ctime = Metadata.Value.CreationTime - DateTimeOffset.UnixEpoch;
            var Atime = Metadata.Value.LastAccessTime - DateTimeOffset.UnixEpoch;
            var Wtime = Metadata.Value.LastWriteTime - DateTimeOffset.UnixEpoch;

            Writer.Write(byte.MaxValue);
            Writer.Write(Metadata.Value.IsDirectory ? byte.MaxValue : byte.MinValue);

            Writer.Write7BitEncodedInt64(Metadata.Value.TotalSize);
            Writer.Write7BitEncodedInt64((long)Ctime.TotalSeconds);
            Writer.Write7BitEncodedInt64((long)Atime.TotalSeconds);
            Writer.Write7BitEncodedInt64((long)Wtime.TotalSeconds);
        }

        /// <inheritdoc/>
        protected override void Decode(BinaryReader Reader)
        {
            base.Decode(Reader);

            if (Reader.ReadByte() == byte.MinValue)
            {
                this.Metadata = null;
                return;
            }

            var Metadata = new StreamMetadata();
            Metadata.IsDirectory = Reader.ReadByte() != byte.MinValue;
            Metadata.TotalSize = Reader.Read7BitEncodedInt64();

            var Ctime = Reader.Read7BitEncodedInt64();
            var Atime = Reader.Read7BitEncodedInt64();
            var Wtime = Reader.Read7BitEncodedInt64();

            Metadata.CreationTime = DateTimeOffset.UnixEpoch.AddSeconds(Ctime);
            Metadata.LastAccessTime = DateTimeOffset.UnixEpoch.AddSeconds(Atime);
            Metadata.LastWriteTime = DateTimeOffset.UnixEpoch.AddSeconds(Wtime);

            this.Metadata = Metadata;
        }
    }
}
