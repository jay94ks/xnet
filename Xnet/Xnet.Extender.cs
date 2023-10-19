public sealed partial class Xnet
{
    /// <summary>
    /// Extension interface.
    /// </summary>
    public interface Extender
    {

    }

    /// <summary>
    /// Connection extender interface.
    /// </summary>
    public interface ConnectionExtender : Extender
    {
        /// <summary>
        /// Called when a new connection arrived.
        /// </summary>
        /// <param name="Connection"></param>
        /// <returns></returns>
        ValueTask OnConnectedAsync(Xnet Connection);

        /// <summary>
        /// Called when the connection is disonnected.
        /// </summary>
        /// <param name="Connection"></param>
        /// <returns></returns>
        ValueTask OnDisconnectedAsync(Xnet Connection);
    }

    /// <summary>
    /// Packet extender interface.
    /// </summary>
    public interface PacketExtender : Extender
    {
        /// <summary>
        /// Execute the packet asynchronously.
        /// </summary>
        /// <param name="Connection"></param>
        /// <param name="Packet"></param>
        /// <param name="Next"></param>
        /// <returns></returns>
        Task ExecuteAsync(Xnet Connection, Packet Packet, Func<Task> Next);
    }

    /// <summary>
    /// Get all extenders.
    /// </summary>
    /// <returns></returns>
    public IEnumerable<Extender> GetExtenders() => Impls.Extenders;

    /// <summary>
    /// Get all extenders by their types.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public IEnumerable<T> GetExtenders<T>() where T : Extender
    {
        return Impls.Extenders.Where(X => X is T).Select(X => (T)X);
    }

    /// <summary>
    /// Get an extender by its type.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public T GetExtender<T>() where T : Extender => GetExtenders<T>().FirstOrDefault();
}