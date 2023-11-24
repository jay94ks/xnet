namespace XnetStreams
{
    /// <summary>
    /// Stream Metadata
    /// </summary>
    public struct StreamMetadata
    {
        /// <summary>
        /// Initialize a new <see cref="StreamMetadata"/> instance.
        /// </summary>
        public StreamMetadata()
        {
        }

        /// <summary>
        /// Indicates whether the path is directory or not.
        /// </summary>
        public bool IsDirectory { get; set; }

        /// <summary>
        /// Creation Time.
        /// </summary>
        public DateTimeOffset CreationTime { get; set; } = DateTimeOffset.UtcNow;

        /// <summary>
        /// Last Access Time.
        /// </summary>
        public DateTimeOffset LastAccessTime { get; set; } = DateTimeOffset.UtcNow;

        /// <summary>
        /// Last Write Time.
        /// </summary>
        public DateTimeOffset LastWriteTime { get; set;} = DateTimeOffset.UtcNow;

        /// <summary>
        /// Size of the target path.
        /// </summary>
        public long TotalSize { get; set; } = 0;
    }
}
