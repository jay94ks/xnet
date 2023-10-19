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

            Options.EnableJsonRpc(typeof(Program).Assembly);

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
            Options.EnableJsonRpc(typeof(Program).Assembly);

            var Services = new ServiceCollection();
            using var Provider = Services.BuildServiceProvider();

            await Xnet.Server(Provider, Options, Token);
        }

        /// <summary>
        /// Test packet.
        /// </summary>
        private class PACKET : Xnet.BasicPacket
        {
            public override async Task ExecuteAsync(Xnet Connection)
            {
                Console.WriteLine(Connection.IsServerMode
                    ? "SERVER RECEIVED: PACKET!"
                    : "CLIENT RECEIVED: PACKET!");

                _ = CallTestController(Connection);
                await Connection.EmitAsync(this);
            }

            private static async Task CallTestController(Xnet Connection)
            {
                var Message = await Connection.CallAsync<TestRpcMessage>(
                    "test.hello", new TestRpcMessage { Text = "hi" });

                Console.WriteLine(Message.Text);
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

    public class TestRpcMessage
    {
        /// <summary>
        /// Text.
        /// </summary>
        public string Text { get; set; } = string.Empty;
    }

    [XnetJsonRpcRoute(Name = "test")]
    public class TestController : XnetJsonRpcController
    {
        /// <summary>
        /// Hello.
        /// </summary>
        /// <param name="Message"></param>
        /// <returns></returns>
        [XnetJsonRpcRoute(Name = "hello")]
        public TestRpcMessage Hello([XnetRpcArgs] TestRpcMessage Message)
        {
            return Message;
        }
    }
}