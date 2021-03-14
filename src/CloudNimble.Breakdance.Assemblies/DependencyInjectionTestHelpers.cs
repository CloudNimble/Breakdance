using Microsoft.Extensions.DependencyInjection;
using System;
using System.Text;

namespace CloudNimble.Breakdance.Assemblies
{

    /// <summary>
    /// 
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

    }

}