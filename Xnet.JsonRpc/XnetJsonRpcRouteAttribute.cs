
/// <summary>
/// Json RPC controller base.
/// These controllers will be reused for executing RPC requests.
/// </summary>
/// <summary>
/// Json Rpc Route attribute.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class XnetJsonRpcRouteAttribute : Attribute
{
    /// <summary>
    /// Name of route.
    /// These will be combined to single action name.
    /// e.g. test.rpc_handler1 (class route: test, method route: rpc_handler1)
    /// </summary>
    public string Name { get; set; }
}