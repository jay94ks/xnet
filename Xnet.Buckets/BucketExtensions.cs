using XnetBuckets.Impls;

namespace XnetBuckets
{
    /// <summary>
    /// Bucket extensions.
    /// </summary>
    public static class BucketExtensions
    {
        /// <summary>
        /// Enable the bucket manager for <see cref="Xnet"/>.
        /// </summary>
        /// <param name="Options"></param>
        /// <returns></returns>
        public static IBucketManager EnableBucketManager(this Xnet.Options Options)
        {
            var Temp = Options.Extenders.FirstOrDefault(X => X is IBucketManager);
            if (Temp is IBucketManager Manager)
                return Manager;

            Options.Extenders.Add(Manager = new BucketManager());
            return Manager;
        }

    }
}
