using System.Security.Cryptography;

namespace XnetDsa.Impls.Shared
{
    internal class SharedSha256 : SharedUsing<SHA256>
    {
        /// <summary>
        /// Initialize the static SHA256 constants.
        /// </summary>
        static SharedSha256()
        {
            // --> set the constructor for shared using.
            SetCtor(() => SHA256.Create());
        }
    }
}
