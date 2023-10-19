using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

/// <summary>
/// Helper methods for <see cref="Xnet"/> instances.
/// </summary>
public static class XnetHelpers
{
    /// <summary>
    /// Add packet providers that scanned from the specified assembly.
    /// </summary>
    /// <param name="Set"></param>
    /// <param name="Assembly"></param>
    /// <returns></returns>
    public static HashSet<Xnet.PacketProvider> AddAssembly(this HashSet<Xnet.PacketProvider> Set, Assembly Assembly, params string[] Kinds)
    {
        Set.Add(new Xnet.BasicPacketProviderImpl(Assembly, Kinds));
        return Set;
    }
}