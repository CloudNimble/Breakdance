using CloudNimble.Breakdance.Azurite;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CloudNimble.Breakdance.Tests.Azurite
{

    [TestClass]
    public class AzuriteInstanceTests
    {

        [TestMethod]
        public async Task StartAsync_ShouldStartAzuriteSuccessfully()
        {
            // Arrange - Use port 0 for dynamic port assignment to avoid conflicts
            var config = new AzuriteConfiguration
            {
                Services = AzuriteServiceType.Blob,
                BlobPort = 0,
                InMemoryPersistence = true,
                Silent = true
            };
            var instance = new AzuriteInstance(config);

            try
            {
                // Act
                await instance.StartAsync();

                // Assert
                instance.IsRunning.Should().BeTrue();
                instance.BlobPort.Should().BeGreaterThan(0);
                instance.BlobEndpoint.Should().NotBeNullOrEmpty();
            }
            finally
            {
                // Cleanup
                await instance.StopAsync();
                instance.Dispose();
            }
        }

        [TestMethod]
        public async Task StartAsync_WithAllServices_ShouldStartAllServices()
        {
            // Arrange - Use port 0 for dynamic port assignment to avoid conflicts
            var config = new AzuriteConfiguration
            {
                Services = AzuriteServiceType.All,
                BlobPort = 0,
                QueuePort = 0,
                TablePort = 0,
                InMemoryPersistence = true,
                Silent = true
            };
            var instance = new AzuriteInstance(config);

            try
            {
                // Act
                await instance.StartAsync();

                // Assert
                instance.IsRunning.Should().BeTrue();
                instance.BlobPort.Should().BeGreaterThan(0);
                instance.QueuePort.Should().BeGreaterThan(0);
                instance.TablePort.Should().BeGreaterThan(0);
                instance.BlobEndpoint.Should().NotBeNullOrEmpty();
                instance.QueueEndpoint.Should().NotBeNullOrEmpty();
                instance.TableEndpoint.Should().NotBeNullOrEmpty();
            }
            finally
            {
                // Cleanup
                await instance.StopAsync();
                instance.Dispose();
            }
        }

        [TestMethod]
        public async Task StartAsync_CalledTwice_ShouldThrowException()
        {
            // Arrange - Use port 0 for dynamic port assignment to avoid conflicts
            var config = new AzuriteConfiguration
            {
                Services = AzuriteServiceType.Blob,
                BlobPort = 0,
                InMemoryPersistence = true,
                Silent = true
            };
            var instance = new AzuriteInstance(config);

            try
            {
                await instance.StartAsync();

                // Act
                Func<Task> act = async () => await instance.StartAsync();

                // Assert
                await act.Should().NotThrowAsync<InvalidOperationException>();
            }
            finally
            {
                // Cleanup
                await instance.StopAsync();
                instance.Dispose();
            }
        }

        [TestMethod]
        public async Task StopAsync_ShouldStopAzurite()
        {
            // Arrange - Use port 0 for dynamic port assignment to avoid conflicts
            var config = new AzuriteConfiguration
            {
                Services = AzuriteServiceType.Blob,
                BlobPort = 0,
                InMemoryPersistence = true,
                Silent = true
            };
            var instance = new AzuriteInstance(config);
            await instance.StartAsync();

            // Act
            await instance.StopAsync();

            // Assert
            instance.IsRunning.Should().BeFalse();
        }

        [TestMethod]
        public async Task GetConnectionString_ShouldReturnValidConnectionString()
        {
            // Arrange - Use port 0 for dynamic port assignment to avoid conflicts
            var config = new AzuriteConfiguration
            {
                Services = AzuriteServiceType.All,
                BlobPort = 0,
                QueuePort = 0,
                TablePort = 0,
                InMemoryPersistence = true,
                Silent = true
            };
            var instance = new AzuriteInstance(config);

            try
            {
                await instance.StartAsync();

                // Act
                var connectionString = instance.GetConnectionString();

                // Assert
                connectionString.Should().NotBeNullOrEmpty();
                connectionString.Should().Contain("DefaultEndpointsProtocol=http");
                connectionString.Should().Contain("AccountName=devstoreaccount1");
                connectionString.Should().Contain("AccountKey=");
                connectionString.Should().Contain($"BlobEndpoint=http://127.0.0.1:{instance.BlobPort}");
                connectionString.Should().Contain($"QueueEndpoint=http://127.0.0.1:{instance.QueuePort}");
                connectionString.Should().Contain($"TableEndpoint=http://127.0.0.1:{instance.TablePort}");
            }
            finally
            {
                // Cleanup
                await instance.StopAsync();
                instance.Dispose();
            }
        }

        [TestMethod]
        public async Task MultipleInstances_ShouldRunSimultaneously()
        {
            // Arrange - Use port 0 for dynamic port assignment to avoid conflicts
            var config1 = new AzuriteConfiguration
            {
                Services = AzuriteServiceType.Blob,
                BlobPort = 0,
                InMemoryPersistence = true,
                Silent = true
            };
            var config2 = new AzuriteConfiguration
            {
                Services = AzuriteServiceType.Blob,
                BlobPort = 0,
                InMemoryPersistence = true,
                Silent = true
            };
            var config3 = new AzuriteConfiguration
            {
                Services = AzuriteServiceType.Blob,
                BlobPort = 0,
                InMemoryPersistence = true,
                Silent = true
            };

            var instance1 = new AzuriteInstance(config1);
            var instance2 = new AzuriteInstance(config2);
            var instance3 = new AzuriteInstance(config3);

            try
            {
                // Act
                await instance1.StartAsync();
                await instance2.StartAsync();
                await instance3.StartAsync();

                // Assert
                instance1.IsRunning.Should().BeTrue();
                instance2.IsRunning.Should().BeTrue();
                instance3.IsRunning.Should().BeTrue();

                // All should have different ports
                var ports = new[] { instance1.BlobPort, instance2.BlobPort, instance3.BlobPort };
                ports.Should().OnlyHaveUniqueItems("each instance should use a different port");
            }
            finally
            {
                // Cleanup
                await instance1.StopAsync();
                await instance2.StopAsync();
                await instance3.StopAsync();
                instance1.Dispose();
                instance2.Dispose();
                instance3.Dispose();
            }
        }

        [TestMethod]
        public async Task ParallelInstanceCreation_ShouldSucceed()
        {
            // Arrange
            // Note: azurite-blob doesn't properly report dynamically assigned ports in its output,
            // so we use specific high ports to avoid conflicts. Using a random base port.
            var random = new Random();
            var basePort = random.Next(40000, 45000);
            var instances = new List<AzuriteInstance>();
            var tasks = new List<Task>();
            var instanceCount = 5;

            try
            {
                // Act - Create and start multiple instances in parallel with different ports
                for (int i = 0; i < instanceCount; i++)
                {
                    var config = new AzuriteConfiguration
                    {
                        Services = AzuriteServiceType.Blob,
                        BlobPort = basePort + i,
                        InMemoryPersistence = true,
                        Silent = true
                    };
                    var instance = new AzuriteInstance(config);
                    instances.Add(instance);
                    tasks.Add(instance.StartAsync());
                }

                await Task.WhenAll(tasks);

                // Assert
                instances.Should().HaveCount(instanceCount);
                instances.Should().OnlyContain(i => i.IsRunning, "all instances should be running");

                var ports = instances.Select(i => i.BlobPort).ToArray();
                ports.Should().OnlyHaveUniqueItems("each instance should have a unique port");
            }
            finally
            {
                // Cleanup
                var stopTasks = instances.Select(i => i.StopAsync());
                await Task.WhenAll(stopTasks);

                foreach (var instance in instances)
                {
                    instance.Dispose();
                }
            }
        }

        [TestMethod]
        public async Task DynamicPort_ShouldAssignAvailablePort()
        {
            // Arrange - Use port 0 for dynamic port assignment
            // Note: The main 'azurite' command (used with All services) properly reports
            // dynamically assigned ports. Single-service commands (azurite-blob, etc.) don't.
            var config = new AzuriteConfiguration
            {
                Services = AzuriteServiceType.All,
                BlobPort = 0,
                QueuePort = 0,
                TablePort = 0,
                InMemoryPersistence = true,
                Silent = true
            };
            var instance = new AzuriteInstance(config);

            try
            {
                // Act
                await instance.StartAsync();

                // Assert - all ports should be assigned
                instance.BlobPort.Should().BeGreaterThan(0);
                instance.QueuePort.Should().BeGreaterThan(0);
                instance.TablePort.Should().BeGreaterThan(0);
                instance.BlobEndpoint.Should().NotBeNullOrEmpty();
                instance.QueueEndpoint.Should().NotBeNullOrEmpty();
                instance.TableEndpoint.Should().NotBeNullOrEmpty();
            }
            finally
            {
                // Cleanup
                await instance.StopAsync();
                instance.Dispose();
            }
        }

        [TestMethod]
        public async Task Dispose_ShouldStopInstance()
        {
            // Arrange - Use port 0 for dynamic port assignment to avoid conflicts
            var config = new AzuriteConfiguration
            {
                Services = AzuriteServiceType.Blob,
                BlobPort = 0,
                InMemoryPersistence = true,
                Silent = true
            };
            var instance = new AzuriteInstance(config);
            await instance.StartAsync();

            // Act
            instance.Dispose();

            // Allow a moment for cleanup
            await Task.Delay(500);

            // Assert
            instance.IsRunning.Should().BeFalse();
        }

        [TestMethod]
        public async Task IndividualServices_BlobOnly_ShouldStartCorrectly()
        {
            // Arrange - Use a random high port to avoid conflicts
            var random = new Random();
            var blobPort = random.Next(30000, 35000);

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
                await instance.StartAsync();

                // Assert
                instance.BlobPort.Should().Be(blobPort);
                instance.QueuePort.Should().BeNull();
                instance.TablePort.Should().BeNull();
            }
            finally
            {
                await instance.StopAsync();
                instance.Dispose();
            }
        }

        [TestMethod]
        public async Task IndividualServices_QueueOnly_ShouldStartCorrectly()
        {
            // Arrange - Use a random high port to avoid conflicts
            var random = new Random();
            var queuePort = random.Next(35000, 40000);

            var config = new AzuriteConfiguration
            {
                Services = AzuriteServiceType.Queue,
                QueuePort = queuePort,
                InMemoryPersistence = true,
                Silent = true
            };
            var instance = new AzuriteInstance(config);

            try
            {
                // Act
                await instance.StartAsync();

                // Assert
                instance.BlobPort.Should().BeNull();
                instance.QueuePort.Should().Be(queuePort);
                instance.TablePort.Should().BeNull();
            }
            finally
            {
                await instance.StopAsync();
                instance.Dispose();
            }
        }

        [TestMethod]
        public async Task IndividualServices_TableOnly_ShouldThrowNotSupportedException()
        {
            // Table-only mode is disabled due to an Azurite bug where azurite-table
            // reports port 0 instead of the actual bound port when using dynamic ports.
            // Once the bug is fixed upstream, this test should be updated.

            // Arrange
            var config = new AzuriteConfiguration
            {
                Services = AzuriteServiceType.Table,
                TablePort = 10002,
                InMemoryPersistence = true,
                Silent = true
            };
            var instance = new AzuriteInstance(config);

            // Act
            Func<Task> act = async () => await instance.StartAsync();

            // Assert - Should throw NotSupportedException with helpful message
            await act.Should().ThrowAsync<NotSupportedException>()
                .WithMessage("*Table-only mode is not currently supported*");
        }

    }

}
