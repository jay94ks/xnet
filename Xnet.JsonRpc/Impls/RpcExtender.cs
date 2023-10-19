using System.Collections.Concurrent;
using System.Security.Cryptography;
using XnetInternals.Impls.Packets;

namespace XnetInternals.Impls
{
    /// <summary>
    /// Rpc Extender.
    /// </summary>
    internal class RpcExtender : Xnet.Extender
    {
        /// <summary>
        /// Singleton.
        /// </summary>
        public static readonly RpcExtender Instance = new RpcExtender();

        // --
        private readonly ConcurrentDictionary<Guid, TaskCompletionSource<RPC_RESPONSE>> m_Tasks = new();

        /// <summary>
        /// Make a request id.
        /// </summary>
        /// <returns></returns>
        private static Guid MakeRequestId()
        {
            using var Rng = RandomNumberGenerator.Create();
            Span<byte> Temp = stackalloc byte[16];

            Rng.GetNonZeroBytes(Temp);
            return new Guid(Temp);
        }

        /// <summary>
        /// Send a RPC request and wait its response asynchronously.
        /// </summary>
        /// <param name="Xnet"></param>
        /// <param name="Request"></param>
        /// <param name="Token"></param>
        /// <returns></returns>
        public async Task<RPC_RESPONSE> CallAsync(Xnet Xnet, RPC_REQUEST Request, CancellationToken Token = default)
        {
            var Tcs = new TaskCompletionSource<RPC_RESPONSE>();
            while(true)
            {
                Token.ThrowIfCancellationRequested();
                Request.ReqId = MakeRequestId();

                if (m_Tasks.TryAdd(Request.ReqId, Tcs) == false)
                    continue;

                break;
            }

            var Removed = true;
            try
            {
                if (await Xnet.EmitAsync(Request, Token) == false)
                {
                    Removed = false;
                    return null;
                }

                using (Token.Register(() => Tcs.TrySetResult(null), false))
                {
                    var Response = await Tcs.Task.ConfigureAwait(false);
                    if (Response is null)
                    {
                        Removed = false;
                        Token.ThrowIfCancellationRequested();
                        return null;
                    }

                    return Response;
                }
            }

            finally
            {
                if (Removed == false)
                {
                    m_Tasks.TryRemove(Request.ReqId, out _);
                    Tcs?.TrySetResult(null);
                }
            }
        }

        /// <summary>
        /// Called when response packet received.
        /// </summary>
        /// <param name="Response"></param>
        internal void OnResponse(RPC_RESPONSE Response)
        {
            m_Tasks.TryRemove(Response.ReqId, out var Tcs);
            Tcs?.TrySetResult(Response);
        }
    }
}
