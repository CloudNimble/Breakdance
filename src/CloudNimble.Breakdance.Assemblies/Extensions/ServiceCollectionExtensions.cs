using CloudNimble.Breakdance.Assemblies;
using Microsoft.Extensions.Hosting;
using System.Linq;

namespace Microsoft.Extensions.DependencyInjection
{

    /// <summary>
    /// A set of enhancements for an <see cref="IHostBuilder"/> instance.
    /// </summary>
    public static class Breakdance_Assemblies_ServiceCollectionExtensions
    {

        /// <summary>
        /// 
        /// </summary>
        /// <param name="collection"></param>
        public static string GetContainerContentsLog(this ServiceCollection collection)
        {
            Ensure.ArgumentNotNull(collection, nameof(collection));

            return collection.ToList().ToDetailedString();
        }

    }

}
