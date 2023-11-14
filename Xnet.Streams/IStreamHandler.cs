namespace XnetStreams
{
    /// <summary>
    /// Stream Handler.
    /// This will be created per request.
    /// </summary>
    public interface IStreamHandler
    {
        /// <summary>
        /// Handle the <see cref="StreamContext"/> asynchronously.
        /// </summary>
        /// <param name="Context"></param>
        /// <param name="Next"></param>
        /// <returns></returns>
        Task HandleAsync(StreamContext Context, Func<Task> Next);
    }
}
