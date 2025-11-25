using CloudNimble.Breakdance.Azurite;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading.Tasks;

namespace CloudNimble.Breakdance.Tests.Azurite
{

    [TestClass]
    public class DiagnosticTest
    {

        [TestMethod]
        public async Task DiagnoseStartupFailure()
        {
            // Arrange - Use a random high port to avoid conflicts with parallel tests
            var random = new Random();
            var blobPort = random.Next(45000, 50000);

            var config = new AzuriteConfiguration
            {
                Services = AzuriteServiceType.Blob,
                BlobPort = blobPort,
                InMemoryPersistence = true,
                Silent = true
            };
            var instance = new AzuriteInstance(config);

            try
            {
                // Act
                Console.WriteLine("Attempting to start Azurite...");
                await instance.StartAsync();

                Console.WriteLine($"Azurite started successfully!");
                Console.WriteLine($"BlobPort: {instance.BlobPort}");
                Console.WriteLine($"IsRunning: {instance.IsRunning}");
                Console.WriteLine($"StandardOutput: {instance.StandardOutput}");
                Console.WriteLine($"StandardError: {instance.StandardError}");

            }
            catch (Exception ex)
            {
                // This will show us the actual error
                Console.WriteLine($"EXCEPTION: {ex.GetType().Name}");
                Console.WriteLine($"MESSAGE: {ex.Message}");
                Console.WriteLine($"STACK TRACE: {ex.StackTrace}");
                Console.WriteLine($"StandardOutput: {instance.StandardOutput}");
                Console.WriteLine($"StandardError: {instance.StandardError}");
                throw;
            }
            finally
            {
                await instance.StopAsync();
                instance.Dispose();
            }

        }

    }

}
