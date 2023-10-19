using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace XnetInternals.Impls
{
    /// <summary>
    /// Rpc Assembly Collection.
    /// </summary>
    internal class RpcAssemblyCollection : HashSet<Assembly>, Xnet.Extender
    {
        /// <summary>
        /// Routes.
        /// </summary>
        public Dictionary<string, RpcRouteInfo> BuiltRoutes { get; } = new();

        // --
        private bool m_Built = false;

        /// <summary>
        /// Build route informations.
        /// </summary>
        /// <returns></returns>
        public void BuildRouteInfo()
        {
            lock(this)
            {
                if (m_Built)
                    return;

                m_Built = true;
            }

            var Types = this.SelectMany(X => X.GetTypes().Where(X => X.IsAbstract == false))
                .Where(X => X.IsAssignableTo(typeof(XnetJsonRpcController)))
                ;

            foreach (var ClassType in Types)
            {
                var Class = ClassType.GetCustomAttribute<XnetJsonRpcRouteAttribute>();
                var Ctor = ClassType.GetConstructors()
                    .OrderByDescending(X => X.GetParameters().Length)
                    .FirstOrDefault();

                if (Ctor is null)
                    continue;

                var Methods = ClassType.GetMethods()
                    .Where(X => X.GetCustomAttribute<XnetJsonRpcRouteAttribute>() != null)
                    ;

                foreach (var Method in Methods)
                {
                    var Attribute = Method.GetCustomAttribute<XnetJsonRpcRouteAttribute>();
                    if (Attribute is null)
                        continue;

                    var ActionName = string.Join('.', 
                        Class.Name ?? string.Empty,
                        Attribute.Name ?? Method.Name);

                    var ParamInfos = Method.GetParameters().Select(MakeParameterResolver);
                    var RouteInfo = new RpcRouteInfo
                    {
                        ControllerType = ClassType,
                        ActionName = ActionName,
                        MethodInfo = Method,
                        ParameterResolvers = ParamInfos.ToArray(),
                        ActionBody = (Xnet, Args) => ExecuteAction(Xnet, Args, ClassType, Ctor, Method),
                        ReturnConverter = MakeReturnConverter(Method)
                    };

                    BuiltRoutes[RouteInfo.ActionName] = RouteInfo;
                }
            }
        }

        /// <summary>
        /// Make a return converter.
        /// </summary>
        /// <param name="MethodInfo"></param>
        /// <returns></returns>
        private static Func<object, Task<JObject>> MakeReturnConverter(MethodInfo MethodInfo)
        {
            if (MethodInfo.ReturnType.IsAssignableTo(typeof(Task<JObject>)))
                return X => X as Task<JObject>;

            if (MethodInfo.ReturnType.IsAssignableTo(typeof(Task)) == false)
                return X => Task.FromResult(X is JObject Json ? Json : JObject.FromObject(X));

            if (MethodInfo.ReturnType == typeof(Task) ||
                MethodInfo.ReturnType.IsConstructedGenericType == false)
                return X => Task.FromResult(null as JObject);

            var ReturnType = MethodInfo.ReturnType.GetGenericArguments().First();
            var Method = _GenericToJson_.MakeGenericMethod(ReturnType);
            return X => Method.Invoke(null, new[] { X }) as Task<JObject>;
        }

        /// <summary>
        /// MethodInfo of <see cref="GenericToJson{T}(Task{T})"/>.
        /// </summary>
        private static MethodInfo _GenericToJson_ = typeof(RpcAssemblyCollection).GetMethod(nameof(GenericToJson));

        /// <summary>
        /// Convert <typeparamref name="T"/> task to JSON object returning task.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="Task"></param>
        /// <returns></returns>
        public static async Task<JObject> GenericToJson<T>(Task<T> Task)
        {
            var Return = await Task.ConfigureAwait(false);
            if (Return is null)
                return null;

            if (Return is JObject Json)
                return Json;

            return JObject.FromObject(Return);
        }

        /// <summary>
        /// Execute an action.
        /// </summary>
        /// <param name="Xnet"></param>
        /// <param name="Args"></param>
        /// <param name="ClassType"></param>
        /// <param name="Ctor"></param>
        /// <param name="Method"></param>
        /// <returns></returns>
        private static object ExecuteAction(Xnet Xnet, object[] Args, Type ClassType, ConstructorInfo Ctor, MethodInfo Method)
        {
            object Controller;

            while (Xnet.Items.TryGetValue(ClassType, out Controller) == false || Controller is null)
            {
                var Params = Ctor
                    .GetParameters()
                    .Select(X => Xnet.Services.GetService(X.ParameterType))
                    .ToArray();

                if (Xnet.Items.TryAdd(ClassType, Controller = Ctor.Invoke(Params)))
                    break;

                DisposeAnyway(Controller);
            }

            return Method.Invoke(Controller, Args);
        }

        /// <summary>
        /// Dispose an object anyway.
        /// </summary>
        /// <param name="Any"></param>
        private static void DisposeAnyway(object Any)
        {
            if (Any is IDisposable Sync)
                Sync.Dispose();

            else if (Any is IAsyncDisposable Async)
            {
                Async
                    .DisposeAsync().ConfigureAwait(false)
                    .GetAwaiter().GetResult();
            }
        }

        /// <summary>
        /// Make the parameter resolver.
        /// </summary>
        /// <param name="Xnet"></param>
        /// <param name="Info"></param>
        /// <returns></returns>
        private static Func<IServiceProvider, JObject, object> MakeParameterResolver(ParameterInfo Info)
        {
            var Attr = Info.GetCustomAttribute<XnetRpcArgsAttribute>();
            if (Attr is null)
            {
                if (Info.ParameterType == typeof(Xnet))
                    return (_1, _2) => Xnet.Current;

                if (Info.ParameterType == typeof(JObject))
                    return (_, Json) => Json;

                return (Services, _) => Services.GetService(Info.ParameterType);
            }

            if (string.IsNullOrWhiteSpace(Attr.Name))
            {
                if (Info.ParameterType == typeof(JObject))
                    return (_, Json) => Json;

                return (_, Json) =>
                {
                    try { return Json.ToObject(Info.ParameterType); }
                    catch
                    {
                    }

                    return null;
                };
            }

            return (_, Json) =>
            {
                var Property = Json.Property(Attr.Name);
                if (Property.Value is null)
                    return null;

                try { return Property.Value.ToObject(Info.ParameterType); }
                catch
                {
                }

                return null;
            };
        }
    }
}
