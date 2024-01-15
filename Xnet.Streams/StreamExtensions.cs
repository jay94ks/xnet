using Newtonsoft.Json.Linq;
using XnetStreams.Internals;
using static Xnet;

namespace XnetStreams
{
    /// <summary>
    /// Stream extensions.
    /// </summary>
    public static class StreamExtensions
    {
        /// <summary>
        /// Enable the remote stream support for <see cref="Xnet"/>.
        /// And if the remote stream support is already enabled,
        /// this just postpend the request delegate to end of stream handler.
        /// </summary>
        /// <typeparam name="TOptions"></typeparam>
        /// <param name="Options"></param>
        /// <returns></returns>
        public static TOptions EnableRemoteStream<TOptions>(this TOptions Options, Action<StreamDelegateBuilder> Builder = null) where TOptions : Xnet.Options
        {
            var Temp = Options.Extenders.FirstOrDefault(X => X is StreamExtender);
            if (Temp is StreamExtender Extender)
            {
                Builder?.Invoke(new StreamDelegateBuilder(Extender));
                return Options;
            }

            Options.Extenders.Add(Extender = new StreamExtender());
            Builder?.Invoke(new StreamDelegateBuilder(Extender));
            return Options;
        }

        /// <summary>
        /// Get the extender instance.
        /// </summary>
        /// <param name="Xnet"></param>
        /// <returns></returns>
        /// <exception cref="NotSupportedException"></exception>
        private static StreamExtender GetExtender(Xnet Xnet)
        {
            var Extender = StreamExtender.Get(Xnet);
            if (Extender is null)
                throw new NotSupportedException("the remote stream feature is not enabled.");

            return Extender;
        }

        /// <summary>
        /// Query the metadata with options asynchronously.
        /// </summary>
        /// <param name="Xnet"></param>
        /// <param name="Options"></param>
        /// <param name="Token"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">the remote connection is not alive.</exception>
        /// <exception cref="InvalidOperationException">the remote connection is closed before completion.</exception>
        /// <exception cref="InvalidDataException">the dispatched result is not correct.</exception>
        /// <exception cref="OperationCanceledException">the token is triggered.</exception>
        /// <exception cref="NotSupportedException">the remote stream feature is not enabled.</exception>
        public static Task<StreamMetadata?> QueryAsync(this Xnet Xnet, StreamOptions Options, CancellationToken Token = default)
        {
            return GetExtender(Xnet).QueryAsync(Xnet, Options, Token);
        }


        /// <summary>
        /// Open the remote stream with options asynchronously.
        /// </summary>
        /// <param name="Xnet"></param>
        /// <param name="Options"></param>
        /// <param name="Token"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">the remote connection is not alive.</exception>
        /// <exception cref="InvalidOperationException">the remote connection is closed before completion.</exception>
        /// <exception cref="InvalidDataException">the dispatched result is not correct.</exception>
        /// <exception cref="OperationCanceledException">the token is triggered.</exception>
        /// <exception cref="NotSupportedException">the remote stream feature is not enabled.</exception>
        public static Task<RemoteStream> OpenAsync(this Xnet Xnet, StreamOptions Options, CancellationToken Token = default)
        {
            return GetExtender(Xnet).OpenAsync(Xnet, Options, Token);
        }

        /// <summary>
        /// Query the metadata with options asynchronously.
        /// </summary>
        /// <param name="Xnet"></param>
        /// <param name="Path"></param>
        /// <param name="Extras"></param>
        /// <param name="Token"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">the remote connection is not alive.</exception>
        /// <exception cref="InvalidOperationException">the remote connection is closed before completion.</exception>
        /// <exception cref="InvalidDataException">the dispatched result is not correct.</exception>
        /// <exception cref="OperationCanceledException">the token is triggered.</exception>
        /// <exception cref="NotSupportedException">the remote stream feature is not enabled.</exception>
        public static Task<StreamMetadata?> QueryAsync(this Xnet Xnet, string Path, JObject Extras, int Timeout, CancellationToken Token = default)
        {
            var Options = new StreamOptions
            {
                Path = Path,
                Timeout = Timeout,
                Mode = FileMode.Open,
                Access = FileAccess.ReadWrite,
                Share = FileShare.ReadWrite | FileShare.Delete,
                Extras = Extras,
            };

            return QueryAsync(Xnet, Options, Token);
        }

        /// <summary>
        /// Open the remote stream with options asynchronously.
        /// </summary>
        /// <param name="Xnet"></param>
        /// <param name="Path"></param>
        /// <param name="Mode"></param>
        /// <param name="Access"></param>
        /// <param name="Share"></param>
        /// <param name="Token"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">the remote connection is not alive.</exception>
        /// <exception cref="InvalidOperationException">the remote connection is closed before completion.</exception>
        /// <exception cref="InvalidDataException">the dispatched result is not correct.</exception>
        /// <exception cref="OperationCanceledException">the token is triggered.</exception>
        /// <exception cref="NotSupportedException">the remote stream feature is not enabled.</exception>
        public static Task<RemoteStream> OpenAsync(this Xnet Xnet, string Path, FileMode Mode, FileAccess Access, FileShare Share, CancellationToken Token = default)
        {
            var Options = new StreamOptions
            {
                Path = Path,
                Mode = Mode,
                Access = Access,
                Share = Share,
            };

            return OpenAsync(Xnet, Options, Token);
        }

        /// <summary>
        /// Open the remote stream with options asynchronously.
        /// This opens the path without access sharing.
        /// </summary>
        /// <param name="Xnet"></param>
        /// <param name="Path"></param>
        /// <param name="Mode"></param>
        /// <param name="Access"></param>
        /// <param name="Token"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">the remote connection is not alive.</exception>
        /// <exception cref="InvalidOperationException">the remote connection is closed before completion.</exception>
        /// <exception cref="InvalidDataException">the dispatched result is not correct.</exception>
        /// <exception cref="OperationCanceledException">the token is triggered.</exception>
        /// <exception cref="NotSupportedException">the remote stream feature is not enabled.</exception>
        public static Task<RemoteStream> OpenAsync(this Xnet Xnet, string Path, FileMode Mode, FileAccess Access, CancellationToken Token = default)
        {
            return OpenAsync(Xnet, Path, Mode, Access, FileShare.None, Token);
        }

        /// <summary>
        /// Open the remote stream with options asynchronously.
        /// This opens the existing path with `Read-only` access and `Read-share`.
        /// </summary>
        /// <param name="Xnet"></param>
        /// <param name="Path"></param>
        /// <param name="Token"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">the remote connection is not alive.</exception>
        /// <exception cref="InvalidOperationException">the remote connection is closed before completion.</exception>
        /// <exception cref="InvalidDataException">the dispatched result is not correct.</exception>
        /// <exception cref="OperationCanceledException">the token is triggered.</exception>
        /// <exception cref="NotSupportedException">the remote stream feature is not enabled.</exception>
        public static Task<RemoteStream> OpenAsync(this Xnet Xnet, string Path, CancellationToken Token = default)
        {
            return OpenAsync(Xnet, Path, FileMode.Open, FileAccess.Read, FileShare.Read, Token);
        }


        /// <summary>
        /// Query the metadata with options asynchronously.
        /// </summary>
        /// <param name="Xnet"></param>
        /// <param name="Path"></param>
        /// <param name="Token"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">the remote connection is not alive.</exception>
        /// <exception cref="InvalidOperationException">the remote connection is closed before completion.</exception>
        /// <exception cref="InvalidDataException">the dispatched result is not correct.</exception>
        /// <exception cref="OperationCanceledException">the token is triggered.</exception>
        /// <exception cref="NotSupportedException">the remote stream feature is not enabled.</exception>
        public static Task<StreamMetadata?> QueryAsync(this Xnet Xnet, string Path, CancellationToken Token = default) => QueryAsync(Xnet, Path, -1, Token);

        /// <summary>
        /// Query the metadata with options asynchronously.
        /// </summary>
        /// <param name="Xnet"></param>
        /// <param name="Path"></param>
        /// <param name="Timeout"></param>
        /// <param name="Token"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">the remote connection is not alive.</exception>
        /// <exception cref="InvalidOperationException">the remote connection is closed before completion.</exception>
        /// <exception cref="InvalidDataException">the dispatched result is not correct.</exception>
        /// <exception cref="OperationCanceledException">the token is triggered.</exception>
        /// <exception cref="NotSupportedException">the remote stream feature is not enabled.</exception>
        public static Task<StreamMetadata?> QueryAsync(this Xnet Xnet, string Path, int Timeout, CancellationToken Token = default) => QueryAsync(Xnet, Path, null, Timeout, Token);
    }
}
