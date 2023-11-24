using Newtonsoft.Json.Linq;

namespace XnetStreams.Internals
{
    /// <summary>
    /// Stream registration.
    /// </summary>
    internal class StreamRegistration : IDisposable, IAsyncDisposable
    {
        private readonly StreamRegistry m_Registry;
        private IDisposable m_Hook;
        private int m_Disposed = 0;

        /// <summary>
        /// Initialize a new <see cref="StreamRegistration"/> instance.
        /// </summary>
        /// <param name="Xnet"></param>
        /// <param name="Stream"></param>
        /// <param name="Id"></param>
        public StreamRegistration(StreamRegistry Registry, Xnet Xnet, StreamContext Context)
        {
            m_Registry = Registry;
            this.Xnet = Xnet;

            Stream = Context.Stream;
            Path = Context.Options.Path;
            Extras = Context.Options.Extras;

            m_Hook = Xnet.Closing.Register(Dispose, false);
        }

        /// <summary>
        /// Connection.
        /// </summary>
        public Xnet Xnet { get; }

        /// <summary>
        /// Path.
        /// </summary>
        public string Path { get; }

        /// <summary>
        /// Extras.
        /// </summary>
        public JObject Extras { get; }

        /// <summary>
        /// Stream.
        /// </summary>
        public Stream Stream { get; }

        /// <summary>
        /// Id.
        /// </summary>
        public Guid Id { get; internal set; }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (Interlocked.CompareExchange(ref m_Disposed, 1, 0) != 0)
                return;

            m_Registry.OnRegistrationDisposing(this);
            if (m_Hook != null)
            {
                m_Hook.Dispose();
                m_Hook = null;
            }

            try { Stream.Dispose(); } catch { }
        }

        /// <inheritdoc/>
        public async ValueTask DisposeAsync()
        {
            if (Interlocked.CompareExchange(ref m_Disposed, 1, 0) != 0)
                return;

            m_Registry.OnRegistrationDisposing(this);
            if (m_Hook != null)
            {
                m_Hook.Dispose();
                m_Hook = null;
            }

            try { await Stream.DisposeAsync(); } catch { }
        }
    }
}
