using System.Text;

namespace XnetDsa.Test
{
    internal class Program
    {
        static void Main(string[] args)
        {
            RunAlgorithmTest();
            Console.WriteLine(" ---- ");

            DsaAlgorithm.Default = DsaAlgorithm.GetAlgorithm("rsa2048");
            RunAlgorithmTest();
            Console.WriteLine(" ---- ");

            DsaAlgorithm.Default = DsaAlgorithm.GetAlgorithm("rsa2048-sha384");
            RunAlgorithmTest();
            Console.WriteLine(" ---- ");

            DsaAlgorithm.Default = DsaAlgorithm.GetAlgorithm("rsa2048-sha512");
            RunAlgorithmTest();

        }

        private static void RunAlgorithmTest()
        {
            Console.WriteLine($"Algorithm: {DsaAlgorithm.Default.Name}.");

            var Text = Encoding.UTF8.GetBytes("Hello World");
            var Digest = DsaDigest.Make(Text);

            Console.WriteLine($"Hello World: {Digest}.");

            var NewKey = DsaKey.Make();
            var PubKey = NewKey.MakePubKey();

            Console.WriteLine($"New Key: {NewKey}");
            Console.WriteLine($"Pub Key: {PubKey}");

            var Sign = NewKey.Sign(Digest);
            Console.WriteLine($"Sign: {Sign}");

            if (Sign.Verify(PubKey, Digest))
                Console.WriteLine("Well done.");

            else
                Console.WriteLine("Failed to verify the signature!");
        }
    }
}