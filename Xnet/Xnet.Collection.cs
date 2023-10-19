public sealed partial class Xnet
{
    /// <summary>
    /// Collection interface.
    /// </summary>
    public interface Collection : Extender
    {
        /// <summary>
        /// Get a snapshot of connections.
        /// </summary>
        /// <returns></returns>
        Xnet[] Snapshot();

        /// <summary>
        /// Find all connections that the predicate returns true.
        /// </summary>
        /// <param name="Predicate"></param>
        /// <returns></returns>
        public Xnet[] FindAll(Func<Xnet, bool> Predicate);

        /// <summary>
        /// Find a connection that the predicate returns true.
        /// </summary>
        /// <param name="Predicate"></param>
        /// <returns></returns>
        public Xnet Find(Func<Xnet, bool> Predicate);

        /// <summary>
        /// Find a connection from last that the predicate returns true.
        /// </summary>
        /// <param name="Predicate"></param>
        /// <returns></returns>
        public Xnet FindLast(Func<Xnet, bool> Predicate);
    }
}