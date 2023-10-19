using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XnetInternals.Impls.Packets;

namespace XnetInternals.Impls
{
    /// <summary>
    /// Rpc Packet Provider.
    /// </summary>
    internal class RpcPacketProvider : Xnet.BasicPacketProvider<RpcPacketProvider>
    {
        /// <summary>
        /// Singleton.
        /// </summary>
        public static readonly RpcPacketProvider Instance = new RpcPacketProvider();

        /// <inheritdoc/>
        protected override void MapTypes()
        {
            MapFrom(typeof(RPC_REQUEST), typeof(RPC_RESPONSE));
        }
    }
}
