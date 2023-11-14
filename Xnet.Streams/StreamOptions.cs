using Newtonsoft.Json.Linq;

namespace XnetStreams
{
    /// <summary>
    /// Stream Open options.
    /// </summary>
    public class StreamOptions
    {
        /// <summary>
        /// Timeout.
        /// </summary>
        public int Timeout { get; set; } = -1;

        /// <summary>
        /// Read timeout if required.
        /// </summary>
        public int ReadTimeout { get; set; } = -1;

        /// <summary>
        /// Read timeout if required.
        /// </summary>
        public int WriteTimeout { get; set; } = -1;

        /// <summary>
        /// Path to open.
        /// </summary>
        public string Path { get; set; } = string.Empty;

        /// <summary>
        /// Open Mode.
        /// </summary>
        public FileMode Mode { get; set; } = FileMode.Open;

        /// <summary>
        /// Access Mode.
        /// </summary>
        public FileAccess Access { get; set; } = FileAccess.ReadWrite;

        /// <summary>
        /// Sharing Mode.
        /// </summary>
        public FileShare Share { get; set; } = FileShare.None;

        /// <summary>
        /// Extras to pass if required.
        /// </summary>
        public JObject Extras { get; set; }
    }
}
