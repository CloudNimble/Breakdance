using CloudNimble.Breakdance.Assemblies;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Extensions.DependencyInjection
{

    /// <summary>
    /// A set of enhancements for an <see cref="IHostBuilder"/> instance.
    /// </summary>
    public static class Breakdance_Assemblies_IHostBuilderExtensions
    {

        /// <summary>
        /// Get all registered <see cref="ServiceDescriptor">ServiceDescriptors</see> for a given container.
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        /// <remarks>
        /// Taken from https://stackoverflow.com/a/60529530/403765
        /// </remarks>
        public static Dictionary<Type, ServiceDescriptor> GetAllServiceDescriptors(this IHostBuilder builder)
        {
            Ensure.ArgumentNotNull(builder, nameof(builder));

            if (builder is HostBuilder hostBuilder)
            {
                var appServices = hostBuilder.GetFieldValue("_appServices") as ServiceProvider;
                return appServices.GetAllServiceDescriptors();
            }

            throw new NotSupportedException($"Type '{builder.GetType()}' is not supported!");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="hostBuilder"></param>
        /// <returns></returns>
        public static string GetContainerContentsLog(this IHostBuilder hostBuilder)
        {
            Ensure.ArgumentNotNull(hostBuilder, nameof(hostBuilder));

            var dictionary = hostBuilder.GetAllServiceDescriptors();
            return dictionary.Select(c => c.Value).ToList().ToDetailedString();
        }

    }

}
