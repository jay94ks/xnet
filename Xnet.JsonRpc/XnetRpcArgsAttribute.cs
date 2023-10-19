
/// <summary>
/// Json RPC controller base.
/// These controllers will be reused for executing RPC requests.
/// </summary>
/// <summary>
/// Marks a parameter that should refer argument to resolve it.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter)]
public sealed class XnetRpcArgsAttribute : Attribute
{
    /// <summary>
    /// Property Name to refer.
    /// if null or whitespace set, refers entire argument.
    /// </summary>
    public string Name { get; set; }
}