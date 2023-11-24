using System.Security;

namespace XnetStreams
{
    /// <summary>
    /// Physical disk options.
    /// </summary>
    public class PhysicalDiskOptions
    {
        /// <summary>
        /// Force the open mode.
        /// </summary>
        public FileMode? ForceMode { get; set; } = null;

        /// <summary>
        /// Force the access mode.
        /// </summary>
        public FileAccess? ForceAccess { get; set; } = null;

        /// <summary>
        /// Force the sharing mode.
        /// </summary>
        public FileShare? ForceShare { get; set; } = null;

        /// <summary>
        /// Directory to find the file.
        /// </summary>
        public string Directory { get; set; } = "./";

        /// <summary>
        /// Filter delegate to control accesses.
        /// This has chance to change open-mode, access, sharing...
        /// And this will be invoked after applying `Force` properties of this option instance.
        /// </summary>
        public Func<StreamContext, FileInfo, StreamStatus> Filter { get; set; }

        /// <summary>
        /// Serve physical files for context asynchronously.
        /// </summary>
        /// <param name="PathToMap"></param>
        /// <param name="Options"></param>
        /// <param name="Context"></param>
        /// <param name="Next"></param>
        /// <returns></returns>
        internal static async Task ServeAsync(string PathToMap, PhysicalDiskOptions Options, StreamContext Context, Func<Task> Next)
        {
            if (Context.Stream != null ||
                Context.Path.StartsWith(PathToMap) == false)
            {
                await Next.Invoke();
                return;
            }

            var PathName = PathToMap.Length > 0
                ? Context.Path.Substring(PathToMap.Length)
                : Context.Path;

            PathName = string.Join('/', PathName.Split('/', '\\')
                .Where(X => string.IsNullOrWhiteSpace(X) == false)
                .Where(X => X != "." && X != ".."));

            var FullPath = Path.Combine(Options.Directory, PathName);
            if (await ForceOptionsAsync(Options, Context, Next, FullPath) == false)
                return;

            // --> invoke the filter.
            if (Options.Filter != null)
                InvokeFilter(Options, Context, FullPath);

            // --> finally, open the file stream.
            if (Context.Status == StreamStatus.Ok)
            {
                try
                {
                    var ToEnd = false;
                    if (Context.Mode == FileMode.Append)
                    {
                        ToEnd = true;
                        Context.Mode = FileMode.OpenOrCreate;
                    }

                    if (Context.Request == StreamRequest.Metadata)
                    {
                        var Attrs = File.GetAttributes(FullPath);
                        if (Attrs.HasFlag(FileAttributes.Directory))
                            SetMetadataFromDirectory(Context, FullPath);

                        else
                            SetMetadataFromFile(Context, FullPath);
                    }

                    else if (Context.Request == StreamRequest.Stream)
                    {
                        Context.Stream = new FileStream(FullPath,
                            Context.Mode, Context.Access, Context.Share,
                            16384, true);

                        if (ToEnd)
                        {
                            Context.Stream.Seek(0, SeekOrigin.End);
                        }

                        Context.Status = StreamStatus.Ok;
                    }

                }

                catch (Exception Exception)
                {
                    TranslateStatusFromException(Context, Exception);
                    if (Context.Stream != null)
                    {
                        try { await Context.Stream.DisposeAsync(); }
                        catch
                        {
                        }

                        Context.Stream = null;
                    }
                }
            }
        }

        /// <summary>
        /// Set the metadata from directory info.
        /// </summary>
        /// <param name="Context"></param>
        /// <param name="FullPath"></param>
        private static void SetMetadataFromDirectory(StreamContext Context, string FullPath)
        {
            Context.Status = StreamStatus.PathNotFound;

            var Info = new DirectoryInfo(FullPath);
            if (Info.Exists)
            {
                Context.Metadata = new StreamMetadata()
                {
                    IsDirectory = true,
                    CreationTime = Info.CreationTime.ToUniversalTime(),
                    LastAccessTime = Info.LastAccessTime.ToUniversalTime(),
                    LastWriteTime = Info.LastWriteTime.ToUniversalTime(),
                    TotalSize = 0
                };

                Context.Status = StreamStatus.Ok;
            }
        }

        /// <summary>
        /// Set the metadata from file info.
        /// </summary>
        /// <param name="Context"></param>
        /// <param name="FullPath"></param>
        private static void SetMetadataFromFile(StreamContext Context, string FullPath)
        {
            Context.Status = StreamStatus.PathNotFound;

            var Info = new FileInfo(FullPath);
            if (Info.Exists)
            {
                Context.Metadata = new StreamMetadata()
                {
                    IsDirectory = false,
                    CreationTime = Info.CreationTime.ToUniversalTime(),
                    LastAccessTime = Info.LastAccessTime.ToUniversalTime(),
                    LastWriteTime = Info.LastWriteTime.ToUniversalTime(),
                    TotalSize = Info.Length
                };

                Context.Status = StreamStatus.Ok;
            }
        }

        /// <summary>
        /// Apply `Force` options and invoke next delegate if file not found.
        /// This returns false if the next delegate set the stream.
        /// </summary>
        /// <param name="Options"></param>
        /// <param name="Context"></param>
        /// <param name="Next"></param>
        /// <param name="FullPath"></param>
        /// <returns></returns>
        private static async Task<bool> ForceOptionsAsync(PhysicalDiskOptions Options, StreamContext Context, Func<Task> Next, string FullPath)
        {
            var Mode = Options.ForceMode.HasValue
                ? Options.ForceMode.Value
                : Context.Mode;

            var Access = Options.ForceAccess.HasValue
                ? Options.ForceAccess.Value
                : Context.Access;

            var Share = Options.ForceShare.HasValue
                ? Options.ForceShare.Value
                : Context.Share;

            if (File.Exists(FullPath) == false)
            {
                await Next.Invoke();

                if (Context.Stream != null)
                    return false;
            }

            // --> copy `forced` modes.
            Context.Mode = Mode;
            Context.Access = Access;
            Context.Share = Share;
            Context.Status = StreamStatus.Ok;
            return true;
        }

        /// <summary>
        /// Invoke the filter.
        /// </summary>
        /// <param name="Options"></param>
        /// <param name="Context"></param>
        /// <param name="FullPath"></param>
        private static void InvokeFilter(PhysicalDiskOptions Options, StreamContext Context, string FullPath)
        {
            FileInfo Info;

            try
            {
                Info = new FileInfo(FullPath);
                if (Info.Exists == false)
                {
                    Context.Status = StreamStatus.PathNotFound;
                    return;
                }
            }
            catch (Exception Exception)
            {
                TranslateStatusFromException(Context, Exception);
                return;
            }

            Context.Status = Options.Filter(Context, Info);
        }

        /// <summary>
        /// Translate the status from the exception.
        /// </summary>
        /// <param name="Context"></param>
        /// <param name="Exception"></param>
        internal static void TranslateStatusFromException(StreamContext Context, Exception Exception)
        {
            Context.Status = StreamStatus.UnhandledException;

            if (Exception is FileNotFoundException || Exception is DirectoryNotFoundException)
                Context.Status = StreamStatus.PathNotFound;

            else if (Exception is NotSupportedException)
                Context.Status = StreamStatus.NotSupported;

            else if (Exception is UnauthorizedAccessException)
                Context.Status = StreamStatus.Unauthorized;

            else if (Exception is SecurityException)
                Context.Status = StreamStatus.Forbidden;

            else if (Exception is PathTooLongException)
                Context.Status = StreamStatus.PathTooLong;

            else if (Exception is ArgumentOutOfRangeException || Exception is ArgumentException)
                Context.Status = StreamStatus.InvalidParameters;

            else if (Exception is IOException IO)
            {
                if (string.IsNullOrWhiteSpace(IO.Message) ||
                    IO.Message.Contains("sharing", StringComparison.OrdinalIgnoreCase) == false)
                {
                    Context.Status = StreamStatus.UnhandledException;
                }

                else
                {
                    Context.Status = StreamStatus.Exclusive;
                }
            }

            else if (Exception is InvalidOperationException)
                Context.Status = StreamStatus.InvalidOperation;
        }

    }
}
