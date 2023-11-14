using System.Collections.Concurrent;
using XnetStreams.Internals.Packets;

namespace XnetStreams.Internals
{
    /// <summary>
    /// Stream registry.
    /// This manages local streams that opened by remote host.
    /// </summary>
    internal class StreamRegistry
    {
        private readonly ConcurrentDictionary<Guid, StreamRegistration> m_Streams = new();

        /// <summary>
        /// Register the stream for providing <see cref="PKT_OPEN_RESULT"/>.
        /// </summary>
        /// <param name="Stream"></param>
        /// <param name="Result"></param>
        public StreamRegistration Register(Xnet Xnet, Stream Stream)
        {
            var Retval = new StreamRegistration(this, Xnet, Stream);

            Retval.Id = PacketDispatcher.MakeId();
            while (m_Streams.TryAdd(Retval.Id, Retval) == false)
            {
                Retval.Id = PacketDispatcher.MakeId();
            }

            return Retval;
        }

        /// <summary>
        /// Get the registration.
        /// </summary>
        /// <param name="Guid"></param>
        /// <returns></returns>
        public StreamRegistration Get(Guid Guid)
        {
            if (Guid == Guid.Empty)
                return null;

            m_Streams.TryGetValue(Guid, out var Registration);
            return Registration;
        }

        /// <summary>
        /// Called when the registration object is disposing.
        /// </summary>
        /// <param name="Registration"></param>
        /// <returns></returns>
        internal bool OnRegistrationDisposing(StreamRegistration Registration)
        {
            if (m_Streams.TryUpdate(Registration.Id, null, Registration) == false)
                return false;

            return m_Streams.Remove(Registration.Id, out _);
        }
    }
}
