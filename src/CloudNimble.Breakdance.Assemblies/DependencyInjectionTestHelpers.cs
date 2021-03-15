using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Text;

namespace CloudNimble.Breakdance.Assemblies
{

    /// <summary>
    /// A set of utilities for Breakdance that help you verify Dependency Injection configurations, especially in architrectures sensitive to injection order.
    /// </summary>
    public static class DependencyInjectionTestHelpers
    {
         
        /// <summary>
        /// 
        /// </summary>
        /// <param name="collection"></param>
        public static string GetContainerContentsLog(ServiceCollection collection)
        {
            if (collection == null)
            {
                throw new ArgumentNullException(nameof(collection));
            }

            var sb = new StringBuilder();

            foreach (var descriptor in collection)
            {
                sb.AppendLine($"ServiceType: {descriptor.ServiceType}, ImplementationType: {descriptor.ImplementationType}, Lifetime: {descriptor.Lifetime}");
            }
            return sb.ToString();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="provider"></param>
        public static string GetContainerContentsLog(IServiceProvider provider)
        {
            if (provider == null)
            {
                throw new ArgumentNullException(nameof(provider));
            }

            var dictionary = provider.GetAllServiceDescriptors();

            var sb = new StringBuilder();

            foreach (var descriptor in dictionary)
            {
                sb.AppendLine($"ServiceType: {descriptor.Value.ServiceType}, ImplementationType: {descriptor.Value.ImplementationType}, Lifetime: {descriptor.Value.Lifetime}");
            }
            return sb.ToString();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="hostBuilder"></param>
        public static string GetContainerContentsLog(IHostBuilder hostBuilder)
        {
            if (hostBuilder == null)
            {
                throw new ArgumentNullException(nameof(hostBuilder));
            }

            var dictionary = hostBuilder.GetAllServiceDescriptors();

            var sb = new StringBuilder();

            foreach (var descriptor in dictionary)
            {
                sb.AppendLine($"ServiceType: {descriptor.Value.ServiceType}, ImplementationType: {descriptor.Value.ImplementationType}, Lifetime: {descriptor.Value.Lifetime}");
            }
            return sb.ToString();
        }


    }

}