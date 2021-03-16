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

        [TestMethod]
        public void DependencyInjection_ServiceCollection_WritesCorrectly()
        {
            var collection = GetServiceCollection();
            var result = DependencyInjectionTestHelpers.GetContainerContentsLog(collection);
            result.Should().NotBeNullOrWhiteSpace();

            var baseline = File.ReadAllText("..//..//..//Baselines/ServiceCollection.txt");
            result.Should().Be(baseline);
        }

        [TestMethod]
        public void DependencyInjection_HostBuilder_WritesCorrectly()
        {
            var host = GetSimpleMessageBusHost();
            var result = DependencyInjectionTestHelpers.GetContainerContentsLog(host);
            result.Should().NotBeNullOrWhiteSpace();

            var baseline = File.ReadAllText("..//..//..//Baselines/HostBuilder.txt");
            result.Should().Be(baseline);
        }


        [BreakdanceManifestGenerator]
        public async Task WriteServiceCollectionOutputLog_Async(string projectPath)
        {
            var collection = GetServiceCollection();
            var result = DependencyInjectionTestHelpers.GetContainerContentsLog(collection);
            var fullPath = Path.Combine(projectPath, "Baselines//ServiceCollection.txt");

            if (!Directory.Exists(Path.GetDirectoryName(fullPath)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
            }
            File.WriteAllText(fullPath, result);
            await Task.FromResult(0);
        }

        [BreakdanceManifestGenerator]
        public void WriteHostBuilderOutputLog(string projectPath)
        {
            var host = GetSimpleMessageBusHost();
            var result = DependencyInjectionTestHelpers.GetContainerContentsLog(host);
            var fullPath = Path.Combine(projectPath, "Baselines//HostBuilder.txt");
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
