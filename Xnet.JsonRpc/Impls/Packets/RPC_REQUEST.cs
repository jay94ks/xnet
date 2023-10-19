using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XnetInternals.Impls.Packets
{
    [Xnet.BasicPacket(Name = "xnet.rpc.request", Kind = "xnet.rpc")]
    internal class RPC_REQUEST : Xnet.BasicPacket
    {
        /// <summary>
        /// Request Id.
        /// </summary>
        public Guid ReqId { get; set; }

        /// <summary>
        /// Action Name.
        /// </summary>
        public string Action { get; set; }

        /// <summary>
        /// Arguments.
        /// </summary>
        public JObject Arguments { get; set; }

        /// <inheritdoc/>
        protected override void Encode(BinaryWriter Writer)
        {
            Writer.Write(ReqId.ToByteArray());
            Writer.Write(Action ?? string.Empty);

            var Bson = Arguments.ToBson();
            Writer.Write7BitEncodedInt(Bson.Length);
            Writer.Write(Bson);
        }

        /// <inheritdoc/>
        protected override void Decode(BinaryReader Reader)
        {
            ReqId =  new Guid(Reader.ReadBytes(16));
            Action = Reader.ReadString();

            var Bson = Reader.Read7BitEncodedInt();
            Arguments = Reader.ReadBytes(Bson).ToJson();
        }

        /// <inheritdoc/>
        public override async Task ExecuteAsync(Xnet Connection)
        {
            if (Connection.Items.TryGetValue(typeof(RpcAssemblyCollection), out var Collection) == false)
            {
                Collection = Connection.GetExtender<RpcAssemblyCollection>();
                Connection.Items.TryAdd(typeof(RpcAssemblyCollection), Collection);
            }

            if (Collection is not RpcAssemblyCollection Rpc)
            {
                await EmitNoSuchRoute(Connection);
                return;
            }

            Rpc.BuildRouteInfo();
            if (Rpc.BuiltRoutes.TryGetValue(Action, out var RouteInfo) == false)
            {
                await EmitNoSuchRoute(Connection);
                return;
            }

            await ExecuteRequestAsync(Connection, RouteInfo);
        }

        /// <summary>
        /// Execute the request asynchronously.
        /// </summary>
        /// <param name="Connection"></param>
        /// <param name="RouteInfo"></param>
        /// <returns></returns>
        private async Task ExecuteRequestAsync(Xnet Connection, RpcRouteInfo RouteInfo)
        {
            var Response = new RPC_RESPONSE()
            {
                ReqId = ReqId,
                Error = false
            };

            try { Response.Result = await RouteInfo.InvokeAsync(Connection, this); }
            catch (Exception Error)
            {
                Response.Error = true;
                Response.Reason = Error.Message;
            }

            await Connection.EmitAsync(Response);
        }

        /// <summary>
        /// Emit no such route response.
        /// </summary>
        /// <param name="Connection"></param>
        /// <returns></returns>
        private async Task EmitNoSuchRoute(Xnet Connection)
        {
            await Connection.EmitAsync(new RPC_RESPONSE
            {
                ReqId = ReqId,
                Error = null,
                Reason = null,
                Result = null
            });
        }
    }
}
