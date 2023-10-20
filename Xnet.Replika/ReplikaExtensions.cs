using XnetDsa;
using XnetReplika.Impls;

namespace XnetReplika
{
    /// <summary>
    /// Replika extensions.
    /// </summary>
    public static class ReplikaExtensions
    {
        /// <summary>
        /// Enable the <see cref="IReplikaManager"/> supports and its protocol.
        /// </summary>
        /// <typeparam name="TOptions"></typeparam>
        /// <param name="Options"></param>
        /// <param name="Key"></param>
        /// <param name="PubKey"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public static IReplikaManager EnableReplikaManager<TOptions>(this TOptions Options, DsaKey Key, DsaPubKey PubKey = default) where TOptions : Xnet.Options
        {
            if (Key.Validity == false)
                throw new ArgumentException("the specified key is invalid.");

            if (PubKey.Validity == false)
                PubKey = Key.MakePubKey();

            var Check = DsaDigest.Make(Array.Empty<byte>(), Key.Algorithm);
            if (Key.Sign(Check).Verify(PubKey, Check) == false)
                throw new ArgumentException("the specified public key is invalid.");

            // --> add the DSA extender.
            var Manager = new ReplikaManager(Key, PubKey);
            Options.Extenders.Add(ReplikaExtender.Instance);
            Options.Extenders.Add(Manager);
            return Manager;
        }

        /// <summary>
        /// Get the <see cref="IReplikaManager"/> instance.
        /// </summary>
        /// <param name="Xnet"></param>
        /// <returns></returns>
        public static IReplikaManager GetReplikaManager(this Xnet Xnet) => ReplikaExtender.GetReplikaManager(Xnet);
    }
}
