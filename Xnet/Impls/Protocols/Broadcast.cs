using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XnetInternals.Impls.Protocols
{
    /// <summary>
    /// Xnet Collection.
    /// </summary>
    internal class Collection : Xnet.ConnectionExtender, Xnet.Collection
    {
        private readonly HashSet<Xnet> m_Connections = new();

        /// <inheritdoc/>
        public ValueTask OnConnectedAsync(Xnet Connection)
        {
            lock (m_Connections)
                m_Connections.Add(Connection);

            return ValueTask.CompletedTask;
        }

        /// <inheritdoc/>
        public ValueTask OnDisconnectedAsync(Xnet Connection)
        {
            lock (m_Connections)
                m_Connections.Remove(Connection);

            return ValueTask.CompletedTask;
        }

        /// <summary>
        /// Get a snapshot of connections.
        /// </summary>
        /// <returns></returns>
        public Xnet[] Snapshot() { lock (m_Connections) return m_Connections.ToArray(); }

        /// <summary>
        /// Find all connections that the predicate returns true.
        /// </summary>
        /// <param name="Predicate"></param>
        /// <returns></returns>
        public Xnet[] FindAll(Func<Xnet, bool> Predicate)
        {
            lock (m_Connections)
            {
                return m_Connections
                    .Where(Predicate)
                    .ToArray();
            }
        }

        /// <summary>
        /// Find a connection that the predicate returns true.
        /// </summary>
        /// <param name="Predicate"></param>
        /// <returns></returns>
        public Xnet Find(Func<Xnet, bool> Predicate)
        {
            lock (m_Connections)
            {
                return m_Connections
                    .Where(Predicate)
                    .FirstOrDefault();
            }
        }

        /// <summary>
        /// Find a connection from last that the predicate returns true.
        /// </summary>
        /// <param name="Predicate"></param>
        /// <returns></returns>
        public Xnet FindLast(Func<Xnet, bool> Predicate)
        {
            lock (m_Connections)
            {
                return m_Connections
                    .Where(Predicate)
                    .LastOrDefault();
            }
        }
    }
}
