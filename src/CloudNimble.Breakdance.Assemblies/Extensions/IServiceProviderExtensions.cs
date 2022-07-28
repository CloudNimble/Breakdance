﻿using CloudNimble.EasyAF.Core;
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
            Ensure.ArgumentNotNull(provider, nameof(provider));

            if (provider is ServiceProvider serviceProvider)
            {
#if NET6_0_OR_GREATER
                return GetServicesFromServiceProviderEngine(serviceProvider);
#endif
#if NET5_0 || NETCOREAPP3_1 || NETSTANDARD2_0_OR_GREATER || NET472_OR_GREATER
                var engine = serviceProvider.GetFieldValue("_engine");
                return GetServicesFromServiceProviderEngine(engine);
#endif
            }

            else if (provider.GetType().Name == "ServiceProviderEngineScope")
            {
                var engine = provider.GetPropertyValue("Engine");
                return GetServicesFromServiceProviderEngine(engine);
            }

            throw new NotSupportedException($"Type '{provider.GetType()}' is not supported!");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="engine"></param>
        /// <returns></returns>
        private static Dictionary<Type, ServiceDescriptor> GetServicesFromServiceProviderEngine(object engine)
        {
            var result = new Dictionary<Type, ServiceDescriptor>();
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

    }

}
