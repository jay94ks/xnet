namespace XnetStreams.Internals.Packets
{
    /// <summary>
    /// Flush request.
    /// </summary>
    internal class PKT_FLUSH : PKT_BASE
    {
        /// <inheritdoc/>
        public override Task ExecuteAsync(Xnet Connection)
            => StreamExtender.Get(Connection).FlushAsync(Connection, this);

    }
}
