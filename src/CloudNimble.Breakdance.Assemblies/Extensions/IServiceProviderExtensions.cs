using System;
using System.Collections;
using System.Collections.Generic;

namespace Microsoft.Extensions.DependencyInjection
{

    /// <summary>
    /// 
    /// </summary>
    public static class IServiceProviderExtensions
    {
        /// <summary>
        /// Get all registered <see cref="ServiceDescriptor">ServiceDescriptors</see> for a given container.
        /// </summary>
        /// <param name="provider"></param>
        /// <returns></returns>
        /// <remarks>
        /// Taken from https://stackoverflow.com/a/60529530/403765
        /// </remarks>
        public static Dictionary<Type, ServiceDescriptor> GetAllServiceDescriptors(this IServiceProvider provider)
        {
            if (provider == null)
            {
                throw new ArgumentNullException(nameof(provider));
            }


            if (provider is ServiceProvider serviceProvider)
            {
                var result = new Dictionary<Type, ServiceDescriptor>();

                var engine = serviceProvider.GetFieldValue("_engine");
                var callSiteFactory = engine.GetPropertyValue("CallSiteFactory");
                var descriptorLookup = callSiteFactory.GetFieldValue("_descriptorLookup");
                if (descriptorLookup is IDictionary dictionary)
                {
                    foreach (DictionaryEntry entry in dictionary)
                    {
                        result.Add((Type)entry.Key, (ServiceDescriptor)entry.Value.GetPropertyValue("Last"));
                    }
                }

                return result;
            }

            throw new NotSupportedException($"Type '{provider.GetType()}' is not supported!");
        }
    }

}
