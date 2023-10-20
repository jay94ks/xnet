using Microsoft.Extensions.DependencyInjection;
using XnetInternals.Internals;

/// <summary>
/// Xnet Hosting Extensions.
/// </summary>
public static class XnetHostingExtensions
{
    /// <summary>
    /// Get the <see cref="Xnet.ServerOptions"/> from <see cref="IServiceCollection"/> instance.
    /// </summary>
    /// <param name="Services"></param>
    /// <returns></returns>
    private static Xnet.ServerOptions GetServerOptions(this IServiceCollection Services)
    {
        var Sd = Services.FirstOrDefault(X => X.ServiceType == typeof(Xnet.ServerOptions));
        if (Sd is null || Sd.ImplementationInstance is not Xnet.ServerOptions ServerOptions)
        {
            var ServerManager = new XnetServerManager();
            Services
                .AddSingleton(ServerOptions = new Xnet.ServerOptions())
                .AddSingleton<IXnetServerManager>(ServerManager)
                .AddSingleton<IXnetConnectionManager, XnetConnectionManager>();

            Services.AddHostedService<XnetServerService>();
            ServerOptions.Extenders.Add(ServerManager);
        }

        return ServerOptions;
    }

    /// <summary>
    /// Get the <see cref="Xnet.ClientOptions"/> from <see cref="IServiceCollection"/> instance.
    /// </summary>
    /// <param name="Services"></param>
    /// <returns></returns>
    private static XnetClientManager GetClientManager(this IServiceCollection Services)
    {
        var Sd = Services.FirstOrDefault(X => X.ServiceType == typeof(XnetClientManager));
        if (Sd is null || Sd.ImplementationInstance is not XnetClientManager ClientManager)
        {
            Services
                .AddSingleton(ClientManager = new XnetClientManager())
                .AddSingleton<IXnetClientManager>(ClientManager)
                .AddSingleton<IXnetConnectionManager, XnetConnectionManager>();

            Services.AddHostedService<XnetClientService>();
        }

        return ClientManager;
    }

    /// <summary>
    /// Add <see cref="Xnet.Server(IServiceProvider, Xnet.ServerOptions, CancellationToken)"/> service to the service provider.
    /// </summary>
    /// <param name="Services"></param>
    /// <param name="ServerOptions"></param>
    /// <returns></returns>
    public static IServiceCollection AddXnetServer(this IServiceCollection Services, Action<Xnet.ServerOptions> ServerOptions)
    {
        var Options = Services.GetServerOptions();

        ServerOptions?.Invoke(Options);

        return Services;
    }

    /// <summary>
    /// Add <see cref="Xnet.Client(IServiceProvider, Xnet.ClientOptions, CancellationToken)"/> service to the service provider.
    /// </summary>
    /// <param name="Services"></param>
    /// <param name="ClientOptions"></param>
    /// <returns></returns>
    public static IServiceCollection AddXnetClient(this IServiceCollection Services, Action<Xnet.ClientOptions> ClientOptions = null)
    {
        var ClientManager = Services.GetClientManager();
        if (ClientOptions != null)
        {
            var Options = new Xnet.ClientOptions()
            {
                AllowRetry = true,
                AllowRecovery = true,
            };

            ClientOptions?.Invoke(Options);
            ClientManager.Push(Options);
        }

        return Services;
    }
}