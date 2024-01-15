using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Text;
using XnetStreams;

namespace XnetStreamTest
{
    internal class Program
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
            Options.EnableRemoteStream();

            Options.BeforeConnectionLoop = OnBeforeClientLoop;

            var Services = new ServiceCollection();
            using var Provider = Services.BuildServiceProvider();
            await Xnet.Client(Provider, Options, Cts.Token);
        }

        /// <summary>
        /// Called before the client loop is running.
        /// </summary>
        /// <param name="xnet"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        private static Task OnBeforeClientLoop(Xnet xnet)
        {
            _ = HelloAsync(xnet);
            return Task.CompletedTask;
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
            Options
                .EnableRemoteStream(Builder =>
                {
                    Builder.UsePhysicalDisk("test", new PhysicalDiskOptions
                    {
                        Directory = "./test",
                        ForceShare = FileShare.Read,
                    });
                });

            var Services = new ServiceCollection();
            using var Provider = Services.BuildServiceProvider();
            await Xnet.Server(Provider, Options, Token);
        }


        /// <summary>
        /// Append text messages on `test/hello.txt` file.
        /// </summary>
        /// <returns></returns>
        private static async Task HelloAsync(Xnet Xnet)
        {
            var Name = $"test/hello.txt";
            try
            {
                var Meta = await Xnet.QueryAsync(Name);

                Console.WriteLine($"Is Directory: {Meta.Value.IsDirectory}.");
                Console.WriteLine($"Creation Time: {Meta.Value.CreationTime}.");
                Console.WriteLine($"Last Access Time: {Meta.Value.LastAccessTime}.");
                Console.WriteLine($"Last Write Time: {Meta.Value.LastWriteTime}.");
                Console.WriteLine($"Total Size: {Meta.Value.TotalSize}.");

                await using var Stream = await Xnet.OpenAsync(Name, FileMode.OpenOrCreate, FileAccess.ReadWrite);

                using (var Reader = new StreamReader(Stream, Encoding.UTF8, true, 2048, true))
                {
                    var Data = await Reader.ReadToEndAsync();
                    Console.WriteLine(Data);
                }

                await Stream.WriteAsync(Encoding.UTF8.GetBytes("Hello World.\r\n"));
                await Stream.WriteAsync(Encoding.UTF8.GetBytes("This works very well.\r\n\r\n"));
                await Stream.FlushAsync();
            }

            catch(Exception Error)
            {
                Console.WriteLine(Error.Message);
            }
        }

    }
}