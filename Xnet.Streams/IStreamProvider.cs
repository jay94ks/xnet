namespace XnetStreams
{
    /// <summary>
    /// Stream Provider.
    /// This will be created per connection.
    /// </summary>
    public interface IStreamProvider
    {
        /// <summary>
        /// Get the stream asynchronously.
        /// </summary>
        /// <param name="Xnet"></param>
        /// <param name="Options"></param>
        /// <param name="Token"></param>
        /// <returns></returns>
        ValueTask<Stream> GetStreamAsync(Xnet Xnet, StreamOptions Options, CancellationToken Token = default);
    }
}
