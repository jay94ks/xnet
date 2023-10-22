using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XnetBuckets
{
    /// <summary>
    /// Bucket manager.
    /// </summary>
    public interface IBucketManager
    {
        /// <summary>
        /// Activate the specified bucket asynchronously.
        /// </summary>
        /// <param name="Bucket"></param>
        /// <returns></returns>
        ValueTask<bool> ActivateAsync(Bucket Bucket);

        /// <summary>
        /// Deactivate the specified bucket asynchronously.
        /// </summary>
        /// <param name="Bucket"></param>
        /// <returns></returns>
        ValueTask<bool> DeactivateAsync(Bucket Bucket);
    }
}
