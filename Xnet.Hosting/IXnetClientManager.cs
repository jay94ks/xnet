
/// <summary>
/// Client Manager.
/// </summary>
public interface IXnetClientManager : Xnet.Collection
{
    /// <summary>
    /// Add a new option.
    /// </summary>
    /// <param name="Options"></param>
    bool Add(Xnet.ClientOptions Options);
}
