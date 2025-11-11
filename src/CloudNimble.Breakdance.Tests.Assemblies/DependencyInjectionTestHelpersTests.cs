using CloudNimble.Breakdance.Assemblies;
using CloudNimble.Breakdance.Tests.Assemblies.SampleApis;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Threading.Tasks;

namespace CloudNimble.Breakdance.Tests.Assemblies
{

    [TestClass]
    public class DependencyInjectionTestHelpersTests
    {

        private const string projectPath = "..//..//..//";

        [TestMethod]
        public void DependencyInjection_ServiceCollection_WritesCorrectly()
        {
            var collection = GetServiceCollection();
            var result = collection.GetContainerContentsLog();
            result.Should().NotBeNullOrWhiteSpace();

            var baseline = File.ReadAllText(Path.Combine(projectPath, "Baselines/ServiceCollection.txt"));
            result.Should().Be(baseline);
        }

        [TestMethod]
        public void DependencyInjection_HostBuilder_WritesCorrectly()
        {
            var host = GetSimpleMessageBusHost();
            var result = host.GetContainerContentsLog();
            result.Should().NotBeNullOrWhiteSpace();

            //RWM: If we're in a .NET Core test, remove the Core crap.
            //result = result.Replace("Core", "");
#if NET10_0_OR_GREATER
            var baseline = File.ReadAllText(Path.Combine(projectPath, "Baselines/HostBuilder_NET10.txt"));
#elif NET8_0_OR_GREATER
            var baseline = File.ReadAllText(Path.Combine(projectPath, "Baselines/HostBuilder_NET8.txt"));
#elif NET6_0_OR_GREATER
            var baseline = File.ReadAllText(Path.Combine(projectPath, "Baselines/HostBuilder_NET6.txt"));
#endif
            result.Should().Be(baseline);
        }

        //[DataRow(projectPath)]
        //[TestMethod]
        [BreakdanceManifestGenerator]
        public async Task WriteServiceCollectionOutputLog_Async(string projectPath)
        {
            var collection = GetServiceCollection();
            var result = collection.GetContainerContentsLog();
            var fullPath = Path.Combine(projectPath, "Baselines//ServiceCollection.txt");

            if (!Directory.Exists(Path.GetDirectoryName(fullPath)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
            }
            File.WriteAllText(fullPath, result);
            await Task.FromResult(0);
        }

        //[DataRow(projectPath)]
        //[TestMethod]
        [BreakdanceManifestGenerator]
        public void WriteHostBuilderOutputLog(string projectPath)
        {
            var host = GetSimpleMessageBusHost();
            var result = host.GetContainerContentsLog();
#if NET10_0_OR_GREATER
            var fullPath = Path.Combine(projectPath, "Baselines//HostBuilder_NET10.txt");
#elif NET8_0_OR_GREATER
            var fullPath = Path.Combine(projectPath, "Baselines//HostBuilder_NET8.txt");
#elif NET6_0_OR_GREATER
            var fullPath = Path.Combine(projectPath, "Baselines//HostBuilder_NET6.txt");
#endif
            if (!Directory.Exists(Path.GetDirectoryName(fullPath)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
            }
            File.WriteAllText(fullPath, result);
        }

        private ServiceCollection GetServiceCollection()
        {
            var collection = new ServiceCollection();
            collection.AddSingleton<SomeEventArgs>();
            collection.AddScoped<SomeStringList>();
            return collection;
        }

        private IHostBuilder GetSimpleMessageBusHost()
        {
            var builder = Host.CreateDefaultBuilder();
            builder
                // RWM: Configure the services before you call Use____QueueProcessor so that the assembly is loaded into memory before the Reflection happens.
                .ConfigureServices((hostContext, services) =>
                {
                    Console.WriteLine($"SimpleMessageBus starting in the {hostContext.HostingEnvironment.EnvironmentName} Environment");
                    //RWM: There could be scope issues here. Need to discuss further.
                })
                .UseAzureStorageQueueMessagePublisher()
                .UseAzureStorageQueueProcessor()
                .UseOrderedMessageDispatcher()
                .ConfigureLogging((context, b) =>
                {
                    b.SetMinimumLevel(LogLevel.Debug);
                    b.AddConsole();

                    Console.WriteLine($"Queue ConnectionString:  {context.Configuration["AzureStorageQueueOptions:StorageConnectionString"]}");
                    Console.WriteLine($"WebJob ConnectionString: {context.Configuration["ConnectionStrings:AzureWebJobsStorage"]}");
                });

            builder.Build();
            return builder;
        }


    }
}
