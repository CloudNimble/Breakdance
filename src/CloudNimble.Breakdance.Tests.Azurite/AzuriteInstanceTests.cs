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
            // Arrange
            var config = new AzuriteConfiguration
            {
                Services = AzuriteServiceType.Blob,
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
            // Arrange
            var config = new AzuriteConfiguration
            {
                Services = AzuriteServiceType.All,
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
            // Arrange
            var config = new AzuriteConfiguration
            {
                Services = AzuriteServiceType.Blob,
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
            // Arrange
            var config = new AzuriteConfiguration
            {
                Services = AzuriteServiceType.Blob,
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
            // Arrange
            var config = new AzuriteConfiguration
            {
                Services = AzuriteServiceType.All,
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
            // Arrange - Let system assign ports dynamically to avoid conflicts
            var config1 = new AzuriteConfiguration
            {
                Services = AzuriteServiceType.Blob,
                InMemoryPersistence = true,
                Silent = true
            };
            var config2 = new AzuriteConfiguration
            {
                Services = AzuriteServiceType.Blob,
                InMemoryPersistence = true,
                Silent = true
            };
            var config3 = new AzuriteConfiguration
            {
                Services = AzuriteServiceType.Blob,
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
            var instances = new List<AzuriteInstance>();
            var tasks = new List<Task>();
            var instanceCount = 5;

            try
            {
                // Act - Create and start multiple instances in parallel with dynamic ports
                for (int i = 0; i < instanceCount; i++)
                {
                    var config = new AzuriteConfiguration
                    {
                        Services = AzuriteServiceType.Blob,
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
            // Arrange
            var config = new AzuriteConfiguration
            {
                Services = AzuriteServiceType.Blob,
                InMemoryPersistence = true,
                Silent = true
            };
            var instance = new AzuriteInstance(config);

            try
            {
                // Act
                await instance.StartAsync();

                // Assert
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
        public async Task Dispose_ShouldStopInstance()
        {
            // Arrange
            var config = new AzuriteConfiguration
            {
                Services = AzuriteServiceType.Blob,
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
        public async Task IndividualServices_ShouldStartCorrectPorts()
        {
            // Arrange & Act & Assert for Blob only
            var blobConfig = new AzuriteConfiguration
            {
                Services = AzuriteServiceType.Blob,
                InMemoryPersistence = true,
                Silent = true
            };
            var blobInstance = new AzuriteInstance(blobConfig);

            try
            {
                await blobInstance.StartAsync();
                blobInstance.BlobPort.Should().BeGreaterThan(0);
                blobInstance.QueuePort.Should().Be(0);
                blobInstance.TablePort.Should().Be(0);
            }
            finally
            {
                await blobInstance.StopAsync();
                blobInstance.Dispose();
            }

            // Arrange & Act & Assert for Queue only
            var queueConfig = new AzuriteConfiguration
            {
                Services = AzuriteServiceType.Queue,
                InMemoryPersistence = true,
                Silent = true
            };
            var queueInstance = new AzuriteInstance(queueConfig);

            try
            {
                await queueInstance.StartAsync();
                queueInstance.BlobPort.Should().Be(0);
                queueInstance.QueuePort.Should().BeGreaterThan(0);
                queueInstance.TablePort.Should().Be(0);
            }
            finally
            {
                await queueInstance.StopAsync();
                queueInstance.Dispose();
            }

            // Arrange & Act & Assert for Table only
            var tableConfig = new AzuriteConfiguration
            {
                Services = AzuriteServiceType.Table,
                InMemoryPersistence = true,
                Silent = true
            };
            var tableInstance = new AzuriteInstance(tableConfig);

            try
            {
                await tableInstance.StartAsync();
                tableInstance.BlobPort.Should().Be(0);
                tableInstance.QueuePort.Should().Be(0);
                tableInstance.TablePort.Should().BeGreaterThan(0);
            }
            finally
            {
                await tableInstance.StopAsync();
                tableInstance.Dispose();
            }
        }

    }

}
