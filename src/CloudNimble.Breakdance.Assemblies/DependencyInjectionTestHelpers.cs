using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace CloudNimble.Breakdance.Assemblies
{

    /// <summary>
    /// A set of utilities for Breakdance that help you verify Dependency Injection configurations, especially in architectures sensitive to injection order.
    /// </summary>   
    public static class DependencyInjectionTestHelpers
    {

        /// <summary>
        /// 
        /// </summary>
        /// <param name="collection"></param>
        [Obsolete("Use the ServiceCollection.GetContainerContentsLog() extension method instead.", false)]
        public static string GetContainerContentsLog(ServiceCollection collection)
        {
            return collection.GetContainerContentsLog();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="provider"></param>
        [Obsolete("Use the IServiceProvider.GetContainerContentsLog() extension method instead.", false)]
        public static string GetContainerContentsLog(IServiceProvider provider)
        {
            return provider.GetContainerContentsLog();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="hostBuilder"></param>
        [Obsolete("Use the IHostBuilder.GetContainerContentsLog() extension method instead.", false)]
        public static string GetContainerContentsLog(IHostBuilder hostBuilder)
        {
            return hostBuilder.GetContainerContentsLog();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="func"></param>
        /// <returns></returns>
        private static string FuncToString(Func<IServiceProvider, object> func)
        {
            if (func == null)
            {
                return "None";
            }
            Expression<Func<IServiceProvider, object>> expression = (x) => func;
            return expression.Body.ToString();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="list"></param>
        /// <returns></returns>
        internal static string ToDetailedString(this List<ServiceDescriptor> list)
        {
            if (list == null || list.Count == 0)
            {
                return string.Empty;
            }

            var serviceTypeLength = list.Select(c => c.ServiceType.ToString().Length).OrderByDescending(c => c).First();
            var implementationTypeLength = list.Select(c => c.ImplementationType?.ToString()?.Length ?? 0).OrderByDescending(c => c).First();

            var sb = new StringBuilder();

            list.ForEach(c =>
            {
                sb.Append($"Lifetime: {c.Lifetime,-9}  |  ServiceType: ");
                sb.AppendFormat(GetFormatString(0, -serviceTypeLength), c.ServiceType.ToString());
                sb.Append("  |  ImplementationType: ");
                sb.AppendFormat(GetFormatString(0, -implementationTypeLength), c.ImplementationType?.ToString() ?? "None");
                sb.Append($"  |  ImplementationFactory: {FuncToString(c.ImplementationFactory)}\n");
            });
            return sb.ToString();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="index"></param>
        /// <param name="spacing"></param>
        /// <returns></returns>
        private static string GetFormatString(int index, int spacing)
        {
            var test = $"{{{index}, {spacing}}}";
            return test;
        }

    }

}