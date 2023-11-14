namespace XnetStreams.Internals.Packets
{
    /// <summary>
    /// Tell request.
    /// </summary>
    internal class PKT_TELL : PKT_BASE
    {
        /// <inheritdoc/>
        public override Task ExecuteAsync(Xnet Connection)
            => StreamExtender.Get(Connection).TellAsync(Connection, this);
    }
}
