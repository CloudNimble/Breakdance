using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;

namespace Microsoft.Extensions.DependencyInjection
{

    /// <summary>
    /// A set of enhancements for an <see cref="IHostBuilder"/> instance.
    /// </summary>
    public static class IHostBuilderExtensions
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
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (builder is HostBuilder hostBuilder)
            {
                var appServices = hostBuilder.GetFieldValue("_appServices") as ServiceProvider;
                return appServices.GetAllServiceDescriptors();
            }

            throw new NotSupportedException($"Type '{builder.GetType()}' is not supported!");
        }

    }

}
