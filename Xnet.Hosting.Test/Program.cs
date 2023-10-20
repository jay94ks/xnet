using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Text;
using XnetDsa;
using XnetReplika;

namespace XnetTest
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
            var Host = Create().Build();

            Host.UseCors()
                .UseWebSockets();

            Host.MapControllers();
            Host.MapRazorPages();

            var Replika = Host.Services.GetService<IReplikaManager>();
            Replika.Local.Set(Guid.Empty, Guid.Empty, Encoding.UTF8.GetBytes("Hello World"));

            await Host.RunAsync();
        }

        /// <summary>
        /// Create an application builder.
        /// </summary>
        /// <returns></returns>
        private static WebApplicationBuilder Create()
        {
            var Builder = WebApplication.CreateBuilder();
            var ServerKey = DsaKey.Make();
            var ClientKey = DsaKey.Make();

            Builder.Services.AddXnetClient(Options =>
            {
                Options.NetworkId = Guid.Empty;
                Options.Endpoint = new IPEndPoint(IPAddress.Loopback, 7800);
                Options.PacketProviders.Add(new Program());
                Options
                    .EnableDsaSecuredPackets(ClientKey)
                    .EnableJsonRpc(typeof(Program).Assembly);

                Options.EnableReplikaManager(ClientKey);
            });

            Builder.Services.AddXnetServer(Options =>
            {
                Options.NetworkId = Guid.Empty;
                Options.Endpoint = new IPEndPoint(IPAddress.Any, 7800);
                Options.PacketProviders.Add(new Program());
                Options
                    .EnableDsaSecuredPackets(ServerKey)
                    .EnableJsonRpc(typeof(Program).Assembly);

                var Replika = Options.EnableReplikaManager(ServerKey);
                Builder.Services.AddSingleton(Replika);
            });

            Builder.Services.AddCors();
            Builder.Services.AddRazorPages()
                .AddApplicationPart(typeof(Program).Assembly);

            return Builder;
        }

        /// <inheritdoc/>
        protected override void MapTypes()
        {
            MapFrom(typeof(TestPacket));
        }
    }

    /// <summary>
    /// Test packet.
    /// </summary>
    [Xnet.BasicPacket(Name = "test")]
    public class TestPacket : DsaSecuredPacket
    {
        /// <summary>
        /// Message.
        /// </summary>
        public string Message { get; set; }

        /// <inheritdoc/>
        protected override void Decode(BinaryReader Reader, DsaPubKey PubKey)
        {
            Message = Reader.ReadString();
        }

        /// <inheritdoc/>
        protected override void Encode(BinaryWriter Writer, DsaPubKey PubKey)
        {
            Writer.Write(Message ?? "(null)");
        }

        /// <inheritdoc/>
        protected override Task ExecuteAsync(Xnet Connection, bool Validation)
        {
            if (Validation == false)
                return Task.CompletedTask;

            Connection.Services.GetService<ILogger<TestPacket>>()
                ?.LogInformation($"test packet received from: {Connection.RemoteEndpoint}: {Message}.");

            return Task.CompletedTask;
        }
    }

    [Route("api")]
    public class TestController : Controller
    {
        /// <summary>
        /// Set text.
        /// </summary>
        /// <returns></returns>
        [Route("text"), HttpGet]
        public IActionResult SetText(
            [FromServices] IReplikaManager Replika,
            [FromQuery(Name = "text")] string Text = default)
        {
            Text ??= string.Empty;
            Replika.Local.Set(Guid.Empty, Guid.Empty, Encoding.UTF8.GetBytes(Text));
            return StatusCode(200);
        }

        /// <summary>
        /// Emit test packet to all.
        /// </summary>
        /// <param name="ServerManager"></param>
        /// <returns></returns>
        [Route("emit"), HttpGet]
        public async Task<IActionResult> EmitTest(
            [FromServices] IXnetServerManager ServerManager,
            [FromQuery(Name = "msg")] string Message = null)
        {
            var Conns = ServerManager.Snapshot();
            foreach(var Each in Conns)
            {
                await Each.EmitAsync(new TestPacket { Message = Message });
            }

            return StatusCode(200);
        }
    }
}