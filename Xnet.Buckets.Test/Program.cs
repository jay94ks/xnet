using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Xml.Linq;
using XnetDsa;

namespace XnetBuckets.Test
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

            var BucketManager = Options
                .EnableDsaSecuredPackets(DsaKey.Make())
                .EnableBucketManager();

            var Services = new ServiceCollection();
            using var Provider = Services.BuildServiceProvider();

            _ = ClientBucket(BucketManager);
            await Xnet.Client(Provider, Options, Cts.Token);
        }

        /// <summary>
        /// Client bucket.
        /// </summary>
        /// <param name="Manager"></param>
        /// <returns></returns>
        private static async Task ClientBucket(IBucketManager Manager)
        {
            var Bucket = new Bucket<MyBucketItem>(Manager, "my-bucket-item");
            Bucket.OnNetworkPush += (Bucket, Item) =>
            {
                Console.WriteLine("Bucket Item Pushed: #" + Item.Number);
                if (Item.Number > 110)
                    return;

                Bucket.Push(new MyBucketItem
                {
                    Number = Item.Number + 1
                });
            };

            await Bucket.ActivateAsync();
            for (int i = 0; i < 10; ++i)
            {
                await Bucket.PushAsync(new MyBucketItem()
                {
                    Number = (i + 1) * 10
                });
            }
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

            var BucketManager = Options
                .EnableDsaSecuredPackets(DsaKey.Make())
                .EnableBucketManager();

            var Services = new ServiceCollection();
            var Bucket = new Bucket<MyBucketItem>(BucketManager, "my-bucket-item");
            Bucket.OnNetworkPush += (Bucket, Item) =>
            {
                Console.WriteLine("Bucket Item Pushed: #" + Item.Number);
                if (Item.Number > 110)
                    return;

                Bucket.Push(new MyBucketItem
                {
                    Number = Item.Number + 1
                });
            };


            Services.AddSingleton(Bucket);

            using var Provider = Services.BuildServiceProvider();
            await Bucket.ActivateAsync();
            await Xnet.Server(Provider, Options, Token);
        }

        /// <inheritdoc/>
        protected override void MapTypes()
        {
        }
    }

    public class MyBucketItem : IBucketItem
    {
        public int Number = 0;

        public void Serialize(BinaryWriter Writer)
        {
            Writer.Write7BitEncodedInt(Number);
        }

        public void Deserialize(BinaryReader Reader)
        {
            Number = Reader.Read7BitEncodedInt();
        }

    }

}