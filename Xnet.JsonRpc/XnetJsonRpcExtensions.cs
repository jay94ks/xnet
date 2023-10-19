using Newtonsoft.Json.Linq;
using System.Reflection;
using XnetInternals.Impls;
using XnetInternals.Impls.Packets;

/// <summary>
/// Json RPC extensions.
/// </summary>
public static class XnetJsonRpcExtensions
{
    /// <summary>
    /// Enable JsonRpc feature and map all JsonRpc actions in specified assemblies.
    /// </summary>
    /// <typeparam name="TOptions"></typeparam>
    /// <param name="Options"></param>
    /// <param name="Assemblies"></param>
    /// <returns></returns>
    public static TOptions EnableJsonRpc<TOptions>(this TOptions Options, params Assembly[] Assemblies) where TOptions : Xnet.Options
    {
        Options.Extenders.Add(RpcExtender.Instance);
        Options.PacketProviders.Add(RpcPacketProvider.Instance);
        var AssemblyCollection = new RpcAssemblyCollection();

        foreach(var Assembly in Assemblies)
            AssemblyCollection.Add(Assembly);

        Options.Extenders.Add(AssemblyCollection);
        return Options;
    }

    /// <summary>
    /// Call the remote action with argument.
    /// </summary>
    /// <param name="Xnet"></param>
    /// <param name="Action"></param>
    /// <param name="Args"></param>
    /// <param name="Token"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    /// <exception cref="NotSupportedException"></exception>
    /// <exception cref="Exception"></exception>
    public static async Task CallAsync(this Xnet Xnet, string Action, object Args = null, CancellationToken Token = default)
    {
        var Request = new RPC_REQUEST
        {
            Action = Action,
            Arguments = Args != null
                ? (Args is JObject Json ? Json : JObject.FromObject(Args))
                : null
        };

        var Response = await RpcExtender.Instance.CallAsync(Xnet, Request, Token);
        if (Response is null)
            throw new InvalidOperationException("failed to send request.");

        if (Response.Error == null)
            throw new NotSupportedException("no such action exists: " + Action);

        if (Response.Error == true)
            throw new Exception(Response.Reason);
    }

    /// <summary>
    /// Call the remote action with argument.
    /// </summary>
    /// <typeparam name="TReturn"></typeparam>
    /// <param name="Xnet"></param>
    /// <param name="Action"></param>
    /// <param name="Args"></param>
    /// <param name="Token"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    /// <exception cref="NotSupportedException"></exception>
    /// <exception cref="Exception"></exception>
    public static async Task<TReturn> CallAsync<TReturn>(this Xnet Xnet, string Action, object Args = null, CancellationToken Token = default)
    {
        var Request = new RPC_REQUEST
        {
            Action = Action,
            Arguments = Args != null
                ? (Args is JObject Json ? Json : JObject.FromObject(Args))
                : null
        };

        var Response = await RpcExtender.Instance.CallAsync(Xnet, Request, Token);
        if (Response is null)
            throw new InvalidOperationException("failed to send request.");

        if (Response.Error == null)
            throw new NotSupportedException("no such action exists: " + Action);

        if (Response.Error == true)
            throw new Exception(Response.Reason);

        if (typeof(TReturn) == typeof(JObject))
            return (TReturn)((object)Response.Result);

        if (Response.Result is null)
            return default;

        return Response.Result.ToObject<TReturn>();
    }
}