using Newtonsoft.Json.Linq;

namespace XnetStreams
{
    /// <summary>
    /// A delegate for opening the stream by its context.
    /// </summary>
    /// <param name="Context"></param>
    /// <param name="Next"></param>
    /// <returns></returns>
    public delegate Task StreamDelegate(StreamContext Context, Func<Task> Next);

    /// <summary>
    /// Stream context.
    /// </summary>
    public class StreamContext
    {
        /// <summary>
        /// Connection.
        /// </summary>
        public Xnet Connection { get; set; }

        /// <summary>
        /// Open Options.
        /// </summary>
        public StreamOptions Options { get; set; }

        /// <summary>
        /// Stream status.
        /// </summary>
        public StreamStatus Status { get; set; } = StreamStatus.PathNotFound;

        /// <summary>
        /// Stream to provide.
        /// </summary>
        public Stream Stream { get; set; }

        /// <summary>
        /// Get a value and if <see cref="Options"/> is null,
        /// returns default instead of calling getter.
        /// </summary>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="Getter"></param>
        /// <returns></returns>
        private TValue Get<TValue>(Func<TValue> Getter, TValue Default)
        {
            if (Options is null)
                return Default;

            return Getter.Invoke();
        }

        /// <summary>
        /// Set the value and if <see cref="Options"/> is null, create new.
        /// </summary>
        /// <param name="Setter"></param>
        private void Set(Action Setter)
        {
            if (Options is null)
                Options = new StreamOptions();

            Setter.Invoke();
        }

        /// <summary>
        /// Timeout.
        /// </summary>
        public int Timeout
        {
            get => Get(() => Options.Timeout, -1);
            set => Set(() => Options.Timeout = value);
        }

        /// <summary>
        /// Read Timeout.
        /// </summary>
        public int ReadTimeout
        {
            get => Get(() => Options.ReadTimeout, -1);
            set => Set(() => Options.ReadTimeout = value);
        }

        /// <summary>
        /// Write Timeout.
        /// </summary>
        public int WriteTimeout
        {
            get => Get(() => Options.WriteTimeout, -1);
            set => Set(() => Options.WriteTimeout = value);
        }

        /// <summary>
        /// Path to open.
        /// </summary>
        public string Path
        {
            get => Get(() => Options.Path, string.Empty);
            set => Set(() => Options.Path = value);
        }

        /// <summary>
        /// Open Mode.
        /// </summary>
        public FileMode Mode
        {
            get => Get(() => Options.Mode, FileMode.Open);
            set => Set(() => Options.Mode = value);
        }

        /// <summary>
        /// Access Mode.
        /// </summary>
        public FileAccess Access
        {
            get => Get(() => Options.Access, FileAccess.ReadWrite);
            set => Set(() => Options.Access = value);
        }

        /// <summary>
        /// Sharing Mode.
        /// </summary>
        public FileShare Share
        {
            get => Get(() => Options.Share, FileShare.None);
            set => Set(() => Options.Share = value);
        }

        /// <summary>
        /// Extras to pass if required.
        /// </summary>
        public JObject Extras
        {
            get => Get(() => Options.Extras, null);
            set => Set(() => Options.Extras = value);
        }
    }
}
