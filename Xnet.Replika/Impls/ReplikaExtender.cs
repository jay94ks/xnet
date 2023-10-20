using Microsoft.Extensions.DependencyInjection;
using XnetReplika.Impls.Packets;

namespace XnetReplika.Impls
{
    /// <summary>
    /// Replika extender.
    /// </summary>
    internal class ReplikaExtender : Xnet.BasicPacketProvider<ReplikaExtender>, Xnet.ConnectionExtender
    {
        /// <summary>
        /// Singleton.
        /// </summary>
        public static readonly ReplikaExtender Instance = new ReplikaExtender();
        
        /// <inheritdoc/>
        protected override void MapTypes()
        {
            MapFrom(
                typeof(RPK_RECORD));
        }

        /// <summary>
        /// Resolve the <see cref="ReplikaManager"/> from service provider.
        /// </summary>
        /// <param name="Xnet"></param>
        /// <returns></returns>
        internal static ReplikaManager GetReplikaManager(Xnet Xnet)
        {
            if (Xnet.Items.TryGetValue(Instance, out var Temp))
                return Temp as ReplikaManager;

            Xnet.Items.TryAdd(Instance, Xnet.GetExtender<ReplikaManager>());
            return GetReplikaManager(Xnet);
        }

        /// <inheritdoc/>
        public ValueTask OnConnectedAsync(Xnet Connection)
        {
            var Rep = GetReplikaManager(Connection);
            if (Rep is null)
                return ValueTask.CompletedTask;

            return Rep.OnConnectedAsync(Connection);
        }

        /// <inheritdoc/>
        public ValueTask OnDisconnectedAsync(Xnet Connection)
        {
            var Rep = GetReplikaManager(Connection);
            if (Rep is null)
                return ValueTask.CompletedTask;

            return Rep.OnDisconnectedAsync(Connection);
        }
    }
}
