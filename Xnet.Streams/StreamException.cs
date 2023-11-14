namespace XnetStreams
{
    /// <summary>
    /// Stream exception.
    /// </summary>
    public class StreamException : Exception
    {
        /// <summary>
        /// Initialize a new <see cref="StreamException"/> instance.
        /// </summary>
        public StreamException(StreamStatus Status) : base(Status.ToString())
        {
            this.Status = Status;
        }

        /// <summary>
        /// Status that returned from the remote host.
        /// </summary>
        public StreamStatus Status { get; }
    }
}
