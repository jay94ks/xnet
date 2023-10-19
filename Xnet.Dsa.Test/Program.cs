using System.Text;

namespace XnetDsa.Test
{
    internal class Program
    {
        static void Main(string[] args)
        {
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