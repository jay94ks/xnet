using XnetStreams.Internals.Packets;

namespace XnetStreams.Internals
{
    /// <summary>
    /// Packet dispatch reservation.
    /// </summary>
    internal class PacketReservation : IDisposable
    {
        private readonly PacketDispatcher m_Dispatcher;
        private readonly TaskCompletionSource<PKT_BASE> m_TaskSource;
        private int m_Disposed = 0;

        /// <summary>
        /// Initialize a new <see cref="PacketReservation"/> instance.
        /// </summary>
        /// <param name="Dispatcher"></param>
        /// <param name="TraceId"></param>
        public PacketReservation(PacketDispatcher Dispatcher)
        {
            m_Dispatcher = Dispatcher;
            m_TaskSource = new TaskCompletionSource<PKT_BASE>();
        }

        /// <summary>
        /// Trace Id.
        /// </summary>
        public Guid TraceId { get; internal set; }

        /// <summary>
        /// Wait for the reservation to be dispatched.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="OperationCanceledException"></exception>
        public async Task<PKT_BASE> WaitAsync(CancellationToken Token = default)
        {
            return await m_TaskSource.Task.WaitAsync(Token).ConfigureAwait(false);
        }

        /// <summary>
        /// Try to peek the result asynchronously.
        /// </summary>
        /// <returns></returns>
        public async ValueTask<PKT_BASE> TryPeekAsync()
        {
            if (m_TaskSource.Task.IsCompletedSuccessfully)
                return await m_TaskSource.Task.ConfigureAwait(false);

            return null;
        }

        /// <summary>
        /// Dispatch a packet to reservation.
        /// </summary>
        /// <param name="Packet"></param>
        /// <returns></returns>
        public bool Dispatch(PKT_BASE Packet)
        {
            return m_TaskSource.TrySetResult(Packet);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (Interlocked.CompareExchange(ref m_Disposed, 1, 0) != 0)
                return;

            Dispatch(null);
            m_Dispatcher.OnReservationDisposing(this);
        }
    }
}
