using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Net;
using System.Text;
using XnetDsa;

namespace XnetReplika.Test
{
    internal class Program
    {
        private static readonly DsaKey ClientKey_1 = DsaKey.Make();
        private static readonly DsaKey ClientKey_2 = DsaKey.Make();
        private static readonly DsaKey ClientKey_3 = DsaKey.Make();
        private static readonly DsaKey ClientKey_4 = DsaKey.Make();
        private static readonly DsaKey ServerKey = DsaKey.Make();

        /// <summary>
        /// Entry Point.
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        static async Task Main(string[] args)
        {
            using var Cts = new CancellationTokenSource();
            _ = RunServer(Cts.Token);

            _ = RunClient(4, ClientKey_4, Cts.Token);
            _ = RunClient(3, ClientKey_3, Cts.Token);
            _ = RunClient(2, ClientKey_2, Cts.Token);
            await RunClient(1, ClientKey_1, Cts.Token);
        }

        /// <summary>
        /// Run the client.
        /// </summary>
        /// <param name="Number"></param>
        /// <param name="Token"></param>
        /// <returns></returns>
        private static async Task RunClient(int Number, DsaKey ClientKey, CancellationToken Token)
        {
            var Options = new Xnet.ClientOptions();

            Options.Endpoint = new IPEndPoint(IPAddress.Loopback, 7800);
            Options.NetworkId = Guid.Empty;

            var Replika = Options
                .EnableDsaSecuredPackets(ClientKey)
                .EnableReplikaManager(ClientKey);

            Replika.Overlay[Guid.Empty].Changed += (Dictionary, ItemId) =>
            {
                Dictionary.TryGet(ItemId, out var Data);

                if (Data is null || Data.Length <= 0)
                    return;

                Console.WriteLine($"Client #{Number}: " + Encoding.UTF8.GetString(Data));
            };

            Replika.Local.Set(Guid.Empty, Guid.Empty, Encoding.UTF8.GetBytes($"Client #{Number} Hello World"));

            var Services = new ServiceCollection();
            using var Provider = Services.BuildServiceProvider();
            await Xnet.Client(Provider, Options, Token);
        }

        /// <summary>
        /// Run the server.
        /// </summary>
        /// <param name="Token"></param>
        /// <returns></returns>
        private static async Task RunServer(CancellationToken Token)
        {
            var Options = new Xnet.ServerOptions();
            Options.Endpoint = new IPEndPoint(IPAddress.Any, 7800);
            Options.NetworkId = Guid.Empty;

            var Replika = Options
                .EnableDsaSecuredPackets(ServerKey)
                .EnableReplikaManager(ServerKey);

            Replika.Overlay[Guid.Empty].Changed += OnClientKeyValueChanged;
            Replika.Local.Set(Guid.Empty, Guid.Empty, Encoding.UTF8.GetBytes("Server Hello World"));

            var Services = new ServiceCollection();
            using var Provider = Services.BuildServiceProvider();

            await Xnet.Server(Provider, Options, Token);
        }

        /// <summary>
        /// Called when Client-side replika updated.
        /// </summary>
        /// <param name="Dictionary"></param>
        /// <param name="ItemId"></param>
        private static void OnClientKeyValueChanged(IReplikaDictionary Dictionary, Guid ItemId)
        {
            Dictionary.TryGet(ItemId, out var Data);

            if (Data is null || Data.Length <= 0)
                return;

            Console.WriteLine("Server: " + Encoding.UTF8.GetString(Data));
        }

    }
}
