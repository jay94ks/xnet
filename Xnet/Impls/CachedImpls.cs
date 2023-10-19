using XnetInternals.Impls.Protocols;

namespace XnetInternals.Impls
{
    /// <summary>
    /// Cached implementations.
    /// </summary>
    internal class CachedImpls
    {
        private static readonly Func<Xnet.Extender>[] DEFAULT_EXTENDER_FACTORIES
            = new Func<Xnet.Extender>[]
            {
                () => PingPong.Instance,
                () => new Collection(),
            };

        /// <summary>
        /// Initialize a new <see cref="CachedImpls"/> instance.
        /// </summary>
        /// <param name="Options"></param>
        public CachedImpls(Xnet.Options Options)
        {
            
            Extenders = Options.Extenders
                .Concat(DEFAULT_EXTENDER_FACTORIES.Select(X => X.Invoke()))
                .ToArray();

            ConnectionExtenders = Extenders
                .Where(X => X is Xnet.ConnectionExtender)
                .Select(X => X as Xnet.ConnectionExtender)
                .ToArray();

            var ExtendedPackets = Extenders
                .Where(X => X is Xnet.PacketProvider)
                .Select(X => X as Xnet.PacketProvider)
                ;

            PacketProviders = Options.PacketProviders
                .Concat(ExtendedPackets)
                .ToArray();

            PacketExtenders = Extenders
                .Where(X => X is Xnet.PacketExtender)
                .Select(X => X as Xnet.PacketExtender)
                .ToArray();

            BeforeConnectionLoop = Options.BeforeConnectionLoop;
            AfterConnectionLoop = Options.AfterConnectionLoop;
        }

        /// <summary>
        /// Extenders.
        /// </summary>
        public readonly Xnet.Extender[] Extenders;

        /// <summary>
        /// Connection extenders.
        /// </summary>
        public readonly Xnet.ConnectionExtender[] ConnectionExtenders;

        /// <summary>
        /// Packet Providers.
        /// </summary>
        public readonly Xnet.PacketProvider[] PacketProviders;

        /// <summary>
        /// Packet Extenders.
        /// </summary>
        public readonly Xnet.PacketExtender[] PacketExtenders;

        /// <summary>
        /// Called before the connection loop.
        /// </summary>
        public readonly Func<Xnet, Task> BeforeConnectionLoop;

        /// <summary>
        /// Called after the connection loop.
        /// </summary>
        public readonly Func<Xnet, Task> AfterConnectionLoop;

        /// <summary>
        /// Execute the packet asynchronously.
        /// </summary>
        /// <param name="Xnet"></param>
        /// <param name="Packet"></param>
        /// <returns></returns>
        public Task ExecuteWithPacketExtenders(Xnet Xnet, Xnet.Packet Packet)
        {
            var Queue = new Queue<Xnet.PacketExtender>(PacketExtenders);
            return ExecuteQueuedPacketExtenders(Queue, Xnet, Packet);
        }

        /// <summary>
        /// Execute the packet asynchronously.
        /// </summary>
        /// <param name="Xnet"></param>
        /// <param name="Packet"></param>
        /// <returns></returns>
        private static async Task ExecuteQueuedPacketExtenders(Queue<Xnet.PacketExtender> Queue, Xnet Xnet, Xnet.Packet Packet)
        {
            if (Queue.TryDequeue(out var Extender))
            {
                await Extender.ExecuteAsync(Xnet, Packet,
                    () => ExecuteQueuedPacketExtenders(Queue, Xnet, Packet));

                return;
            }

            await Packet.ExecuteAsync(Xnet);
        }
    }
}
