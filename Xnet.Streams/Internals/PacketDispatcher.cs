using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using XnetStreams.Internals.Packets;

namespace XnetStreams.Internals
{
    /// <summary>
    /// Packet dispatcher.
    /// </summary>
    internal class PacketDispatcher
    {
        private readonly ConcurrentDictionary<Guid, PacketReservation> m_Reservations;

        /// <summary>
        /// Initialize a new <see cref="PacketDispatcher"/> instance.
        /// </summary>
        public PacketDispatcher()
        {
            m_Reservations = new ConcurrentDictionary<Guid, PacketReservation>();
        }

        /// <summary>
        /// Make an identification using random number generator.
        /// </summary>
        /// <returns></returns>
        internal static Guid MakeId()
        {
            using var Rng = RandomNumberGenerator.Create();
            Span<byte> Temp = stackalloc byte[16];

            Rng.GetNonZeroBytes(Temp);
            return new Guid(Temp);
        }

        /// <summary>
        /// Reserve to dispatch result packet from remote.
        /// </summary>
        /// <param name="TraceId"></param>
        public PacketReservation Reserve()
        {
            var Retval = new PacketReservation(this);

            Retval.TraceId = MakeId();
            while (m_Reservations.TryAdd(Retval.TraceId, Retval) == false)
            {
                Retval.TraceId = MakeId();
            }

            return Retval;
        }

        /// <summary>
        /// Dispatch a packet to reservation.
        /// </summary>
        /// <param name="Packet"></param>
        /// <returns></returns>
        public bool Dispatch(PKT_BASE Packet)
        {
            if (m_Reservations.TryGetValue(Packet.TraceId, out var Reservation) == false)
                return false;

            return Reservation.Dispatch(Packet);
        }

        /// <summary>
        /// Called when the <see cref="PacketReservation"/> is disposing.
        /// </summary>
        /// <param name="Reservation"></param>
        internal bool OnReservationDisposing(PacketReservation Reservation)
        {
            if (m_Reservations.TryUpdate(Reservation.TraceId, null, Reservation) == false)
                return false;

            return m_Reservations.Remove(Reservation.TraceId, out _);
        }
    }
}
