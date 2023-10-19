using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using System.Net;

namespace XnetTests.Test
{
    internal class Program : Xnet.BasicPacketProvider<Program>
    {
        /// <summary>
        /// Entry Point.
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        static async Task Main(string[] args)
        {
            using var Cts = new CancellationTokenSource();
            _ = RunServer(Cts.Token);

            var Options = new Xnet.ClientOptions();

            Options.Endpoint = new IPEndPoint(IPAddress.Loopback, 7800);
            Options.NetworkId = Guid.Empty;
            Options.PacketProviders.Add(new Program());
            Options.BeforeConnectionLoop = Conn =>
            {
                return Conn.EmitAsync(new PACKET());
            };

            var Services = new ServiceCollection();
            using var Provider = Services.BuildServiceProvider();
            await Xnet.Client(Provider, Options, Cts.Token);
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
            Options.PacketProviders.Add(new Program());

            var Services = new ServiceCollection();
            using var Provider = Services.BuildServiceProvider();

            await Xnet.Server(Provider, Options, Token);
        }

        /// <summary>
        /// Test packet.
        /// </summary>
        private class PACKET : Xnet.BasicPacket
        {
            public override Task ExecuteAsync(Xnet Connection)
            {
                Console.WriteLine(Connection.IsServerMode
                    ? "SERVER RECEIVED: PACKET!" 
                    : "CLIENT RECEIVED: PACKET!");

                return Connection.EmitAsync(this);
            }

            protected override void Decode(BinaryReader Reader)
            {
            }

            protected override void Encode(BinaryWriter Writer)
            {
            }
        }

        /// <inheritdoc/>
        protected override void MapTypes()
        {
            Map<PACKET>("test");
        }
    }
}