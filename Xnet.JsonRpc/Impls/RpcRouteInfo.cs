using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using XnetInternals.Impls.Packets;

namespace XnetInternals.Impls
{
    internal class RpcRouteInfo
    {
        /// <summary>
        /// Controller Type.
        /// </summary>
        public Type ControllerType { get; set; }

        /// <summary>
        /// Constructor.
        /// </summary>
        public Func<Xnet, XnetJsonRpcController> Constructor { get; set; }

        /// <summary>
        /// Action Name.
        /// </summary>
        public string ActionName { get; set; }

        /// <summary>
        /// Action Method Info.
        /// </summary>
        public MethodInfo MethodInfo { get; set; }

        /// <summary>
        /// Action Parameter Resolvers.
        /// </summary>
        public Func<IServiceProvider, JObject, object>[] ParameterResolvers { get; set; }

        /// <summary>
        /// Action Body.
        /// </summary>
        public Func<Xnet, object[], object> ActionBody { get; set; }

        /// <summary>
        /// Converts returned object to JSON object.
        /// </summary>
        public Func<object, Task<JObject>> ReturnConverter { get; set; }

        /// <summary>
        /// Invoke the route asynchronously.
        /// </summary>
        /// <param name="Xnet"></param>
        /// <returns></returns>
        public async Task<JObject> InvokeAsync(Xnet Xnet, RPC_REQUEST Request)
        {
            var Args = Request.Arguments != null ? Request.Arguments : new JObject();
            var Params = ParameterResolvers.Select(Resolver => Resolver.Invoke(Xnet.Services, Args));
            var Return = ActionBody.Invoke(Xnet, Params.ToArray());
            return await ReturnConverter.Invoke(Return);
        }
    }
}
