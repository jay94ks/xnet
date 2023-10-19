using Secp256k1Net;

namespace XnetDsa.Impls.Shared
{
    /// <summary>
    /// Shared SECP256K1.
    /// </summary>
    internal class SharedSecp256k1 : SharedUsing<Secp256k1>
    {
        /// <summary>
        /// Initialize the static SECP256K1 constants.
        /// </summary>
        static SharedSecp256k1()
        {
            // --> set the constructor for shared using.
            SetCtor(() => new());
        }
    }
}
