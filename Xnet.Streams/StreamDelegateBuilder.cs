using System.Security;
using XnetStreams.Internals;

namespace XnetStreams
{
    /// <summary>
    /// Stream request delegate builder.
    /// </summary>
    public sealed class StreamDelegateBuilder
    {
        private readonly StreamExtender m_Extender;

        /// <summary>
        /// Initialize a new <see cref="StreamDelegateBuilder"/> instance.
        /// </summary>
        /// <param name="Extender"></param>
        internal StreamDelegateBuilder(StreamExtender Extender)
        {
            m_Extender = Extender;
        }

        /// <summary>
        /// Add a delegate to the builder.
        /// </summary>
        /// <param name="Delegate"></param>
        /// <returns></returns>
        public StreamDelegateBuilder Use(StreamDelegate Delegate)
        {
            m_Extender.Use(Delegate);
            return this;
        }

        /// <summary>
        /// Item Key generator.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        private class ItemKeyFor<T>
        {
            /// <summary>
            /// Key.
            /// </summary>
            public static object KEY = new();
        }

        /// <summary>
        /// Add a delegate that uses <see cref="IStreamProvider"/>.
        /// </summary>
        /// <typeparam name="TProvider"></typeparam>
        /// <returns></returns>
        public StreamDelegateBuilder UseProvider<TProvider>() where TProvider : IStreamProvider, new() => Use(FromProvider<TProvider>);

        /// <summary>
        /// Add a delegate that uses <see cref="IStreamHandler"/>.
        /// </summary>
        /// <typeparam name="THandler"></typeparam>
        /// <returns></returns>
        public StreamDelegateBuilder UseHandler<THandler>() where THandler : IStreamHandler, new() => Use(FromHandler<THandler>);

        /// <summary>
        /// Add a delegate that wraps the stream instance.
        /// This will not change status, this just alter the stream only.
        /// And if the previous delegate doesn't set anything, this will be skipped.
        /// </summary>
        /// <param name="Wrapper"></param>
        /// <returns></returns>
        public StreamDelegateBuilder UseWrapper(Func<Stream, Stream> Wrapper)
        {
            return Use((Context, Next) =>
            {
                if (Context.Stream != null)
                    Context.Stream = Wrapper.Invoke(Context.Stream);

                return Next.Invoke();
            });
        }

        /// <summary>
        /// Add a delegate that serve files in physical disk.
        /// </summary>
        /// <param name="PathToMap">path to map.</param>
        /// <param name="Options"></param>
        /// <returns></returns>
        public StreamDelegateBuilder UsePhysicalDisk(string PathToMap, PhysicalDiskOptions Options)
        {
            if (string.IsNullOrWhiteSpace(PathToMap))
                PathToMap = string.Empty;

            else if (PathToMap.EndsWith("/") == false)
                PathToMap = PathToMap + "/";

            return Use((Context, Next) => PhysicalDiskOptions.ServeAsync(PathToMap, Options, Context, Next));
        }

        /// <summary>
        /// Execute <typeparamref name="THandler"/> like middleware of ASP.NET core.
        /// </summary>
        /// <typeparam name="THandler"></typeparam>
        /// <param name="Context"></param>
        /// <param name="Next"></param>
        /// <returns></returns>
        private static async Task FromHandler<THandler>(StreamContext Context, Func<Task> Next) where THandler : IStreamHandler, new()
        {
            var New = new THandler();

            try { await New.HandleAsync(Context, Next); }
            finally
            {
                if (New is IAsyncDisposable Async)
                    await Async.DisposeAsync().ConfigureAwait(false);

                else if (New is IDisposable Sync)
                    Sync.Dispose();
            }
        }

        /// <summary>
        /// Provide <see cref="Stream"/> instances from <see cref="IStreamProvider"/> instance.
        /// </summary>
        /// <typeparam name="TProvider"></typeparam>
        /// <param name="Context"></param>
        /// <param name="Next"></param>
        /// <returns></returns>
        private static async Task FromProvider<TProvider>(StreamContext Context, Func<Task> Next) where TProvider : IStreamProvider, new()
        {
            var Conn = Context.Connection;
            if (Conn is null)
            {
                await Next.Invoke();
                return;
            }

            var Item = Conn.Items.GetValueOrDefault(ItemKeyFor<TProvider>.KEY);
            if (Item is not TProvider Provider)
            {
                Conn.Items[ItemKeyFor<TProvider>.KEY] = Provider = new TProvider();
            }

            try
            {
                if (Context.Request == StreamRequest.Metadata)
                {
                    var Metadata = await Provider.GetMetadataAsync(Conn, Context.Options, Context.RequestTimeout);
                    if (Metadata.HasValue)
                    {
                        Context.Metadata = Metadata;
                        Context.Status = StreamStatus.Ok;
                        return;
                    }
                }

                else if (Context.Request == StreamRequest.Stream)
                {
                    var Stream = await Provider.GetStreamAsync(Conn, Context.Options, Context.RequestTimeout);
                    if (Stream != null)
                    {
                        Context.Stream = Stream;
                        Context.Status = StreamStatus.Ok;
                        return;
                    }
                }
            }

            finally
            {
                // --> register the hook to dispose the provider if it implements disposable interface.
                if (Provider is IDisposable Sync)
                    Conn.Closing.Register(Sync.Dispose, false);

                else if (Provider is IAsyncDisposable Async)
                {
                    Conn.Closing.Register(() =>
                    {
                        Async
                            .DisposeAsync()
                            .ConfigureAwait(false)
                            .GetAwaiter().GetResult();
                    }, false);
                }
            }

            await Next.Invoke();
        }
    }
}
