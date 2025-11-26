using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using CloudNimble.Breakdance.Azurite;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading.Tasks;

namespace CloudNimble.Breakdance.Tests.Azurite
{

    /// <summary>
    /// Tests for Azure Storage API operations against Azurite.
    /// Uses a single Azurite instance with all services enabled.
    /// </summary>
    [TestClass]
    public class AzureStorageApiTests : AzuriteBreakdanceTestBase
    {

        private static AzuriteInstance _azurite;

        protected override AzuriteInstance Azurite => _azurite;

        [ClassInitialize]
        public static async Task ClassInit(TestContext ctx)
        {
            _azurite = await CreateAndStartInstanceAsync(new AzuriteConfiguration
            {
                Services = AzuriteServiceType.All,
                InMemoryPersistence = true,
                Silent = true
            });
        }

        [ClassCleanup]
        public static async Task ClassCleanup()
        {
            await StopAndDisposeAsync(_azurite);
            _azurite = null;
        }

        #region Blob Storage Tests

        [TestMethod]
        public async Task Blob_CanCreateContainer()
        {
            // Arrange
            var client = new BlobServiceClient(ConnectionString);
            var containerName = $"test-{Guid.NewGuid():N}";

            // Act
            var container = client.GetBlobContainerClient(containerName);
            await container.CreateAsync();

            // Assert
            var exists = await container.ExistsAsync();
            exists.Value.Should().BeTrue();
        }

        [TestMethod]
        public async Task Blob_CanUploadAndDownloadBlob()
        {
            // Arrange
            var client = new BlobServiceClient(ConnectionString);
            var containerName = $"test-{Guid.NewGuid():N}";
            var container = client.GetBlobContainerClient(containerName);
            await container.CreateAsync();

            var blobName = "test-blob.txt";
            var content = "Hello from Azurite!";
            var blob = container.GetBlobClient(blobName);

            // Act
            await blob.UploadAsync(BinaryData.FromString(content));
            var downloaded = await blob.DownloadContentAsync();

            // Assert
            downloaded.Value.Content.ToString().Should().Be(content);
        }

        [TestMethod]
        public async Task Blob_CanDeleteBlob()
        {
            // Arrange
            var client = new BlobServiceClient(ConnectionString);
            var containerName = $"test-{Guid.NewGuid():N}";
            var container = client.GetBlobContainerClient(containerName);
            await container.CreateAsync();

            var blob = container.GetBlobClient("to-delete.txt");
            await blob.UploadAsync(BinaryData.FromString("Delete me"));

            // Act
            await blob.DeleteAsync();

            // Assert
            var exists = await blob.ExistsAsync();
            exists.Value.Should().BeFalse();
        }

        [TestMethod]
        public async Task Blob_CanListBlobs()
        {
            // Arrange
            var client = new BlobServiceClient(ConnectionString);
            var containerName = $"test-{Guid.NewGuid():N}";
            var container = client.GetBlobContainerClient(containerName);
            await container.CreateAsync();

            await container.GetBlobClient("blob1.txt").UploadAsync(BinaryData.FromString("1"));
            await container.GetBlobClient("blob2.txt").UploadAsync(BinaryData.FromString("2"));
            await container.GetBlobClient("blob3.txt").UploadAsync(BinaryData.FromString("3"));

            // Act
            var blobs = container.GetBlobsAsync();
            var count = 0;
            await foreach (var blob in blobs)
            {
                count++;
            }

            // Assert
            count.Should().Be(3);
        }

        #endregion

        #region Queue Storage Tests

        [TestMethod]
        public async Task Queue_CanCreateQueue()
        {
            // Arrange
            var client = new QueueServiceClient(ConnectionString);
            var queueName = $"test-{Guid.NewGuid():N}";

            // Act
            var queue = client.GetQueueClient(queueName);
            await queue.CreateAsync();

            // Assert
            var exists = await queue.ExistsAsync();
            exists.Value.Should().BeTrue();
        }

        [TestMethod]
        public async Task Queue_CanSendAndReceiveMessage()
        {
            // Arrange
            var client = new QueueServiceClient(ConnectionString);
            var queueName = $"test-{Guid.NewGuid():N}";
            var queue = client.GetQueueClient(queueName);
            await queue.CreateAsync();

            var messageContent = "Hello from Azurite Queue!";

            // Act
            await queue.SendMessageAsync(messageContent);
            var messages = await queue.ReceiveMessagesAsync(maxMessages: 1);

            // Assert
            messages.Value.Should().HaveCount(1);
            messages.Value[0].MessageText.Should().Be(messageContent);
        }

        [TestMethod]
        public async Task Queue_CanPeekMessages()
        {
            // Arrange
            var client = new QueueServiceClient(ConnectionString);
            var queueName = $"test-{Guid.NewGuid():N}";
            var queue = client.GetQueueClient(queueName);
            await queue.CreateAsync();

            await queue.SendMessageAsync("Message 1");
            await queue.SendMessageAsync("Message 2");

            // Act
            var peeked = await queue.PeekMessagesAsync(maxMessages: 2);

            // Assert
            peeked.Value.Should().HaveCount(2);
        }

        [TestMethod]
        public async Task Queue_CanDeleteMessage()
        {
            // Arrange
            var client = new QueueServiceClient(ConnectionString);
            var queueName = $"test-{Guid.NewGuid():N}";
            var queue = client.GetQueueClient(queueName);
            await queue.CreateAsync();

            await queue.SendMessageAsync("To be deleted");
            var messages = await queue.ReceiveMessagesAsync(maxMessages: 1);
            var message = messages.Value[0];

            // Act
            await queue.DeleteMessageAsync(message.MessageId, message.PopReceipt);

            // Assert
            var remaining = await queue.PeekMessagesAsync();
            remaining.Value.Should().BeEmpty();
        }

        [TestMethod]
        public async Task Queue_CanClearQueue()
        {
            // Arrange
            var client = new QueueServiceClient(ConnectionString);
            var queueName = $"test-{Guid.NewGuid():N}";
            var queue = client.GetQueueClient(queueName);
            await queue.CreateAsync();

            await queue.SendMessageAsync("Message 1");
            await queue.SendMessageAsync("Message 2");
            await queue.SendMessageAsync("Message 3");

            // Act
            await queue.ClearMessagesAsync();

            // Assert
            var remaining = await queue.PeekMessagesAsync();
            remaining.Value.Should().BeEmpty();
        }

        #endregion

        #region Table Storage Tests

        [TestMethod]
        public async Task Table_CanCreateTable()
        {
            // Arrange
            var client = new TableServiceClient(ConnectionString);
            var tableName = $"test{Guid.NewGuid():N}".Substring(0, 20);

            // Act
            var table = client.GetTableClient(tableName);
            await table.CreateAsync();

            // Assert
            var tables = client.QueryAsync(filter: $"TableName eq '{tableName}'");
            var count = 0;
            await foreach (var t in tables)
            {
                count++;
            }
            count.Should().Be(1);
        }

        [TestMethod]
        public async Task Table_CanAddAndGetEntity()
        {
            // Arrange
            var client = new TableServiceClient(ConnectionString);
            var tableName = $"test{Guid.NewGuid():N}".Substring(0, 20);
            var table = client.GetTableClient(tableName);
            await table.CreateAsync();

            var entity = new TableEntity("partition1", "row1")
            {
                { "Name", "Test Entity" },
                { "Value", 42 }
            };

            // Act
            await table.AddEntityAsync(entity);
            var result = await table.GetEntityAsync<TableEntity>("partition1", "row1");

            // Assert
            result.Value["Name"].Should().Be("Test Entity");
            result.Value["Value"].Should().Be(42);
        }

        [TestMethod]
        public async Task Table_CanUpdateEntity()
        {
            // Arrange
            var client = new TableServiceClient(ConnectionString);
            var tableName = $"test{Guid.NewGuid():N}".Substring(0, 20);
            var table = client.GetTableClient(tableName);
            await table.CreateAsync();

            var entity = new TableEntity("partition1", "row1")
            {
                { "Name", "Original" }
            };
            await table.AddEntityAsync(entity);

            // Act
            entity["Name"] = "Updated";
            await table.UpdateEntityAsync(entity, Azure.ETag.All, TableUpdateMode.Replace);
            var result = await table.GetEntityAsync<TableEntity>("partition1", "row1");

            // Assert
            result.Value["Name"].Should().Be("Updated");
        }

        [TestMethod]
        public async Task Table_CanDeleteEntity()
        {
            // Arrange
            var client = new TableServiceClient(ConnectionString);
            var tableName = $"test{Guid.NewGuid():N}".Substring(0, 20);
            var table = client.GetTableClient(tableName);
            await table.CreateAsync();

            var entity = new TableEntity("partition1", "row1")
            {
                { "Name", "To Delete" }
            };
            await table.AddEntityAsync(entity);

            // Act
            await table.DeleteEntityAsync("partition1", "row1");

            // Assert
            Func<Task> act = async () => await table.GetEntityAsync<TableEntity>("partition1", "row1");
            await act.Should().ThrowAsync<Azure.RequestFailedException>();
        }

        [TestMethod]
        public async Task Table_CanQueryEntities()
        {
            // Arrange
            var client = new TableServiceClient(ConnectionString);
            var tableName = $"test{Guid.NewGuid():N}".Substring(0, 20);
            var table = client.GetTableClient(tableName);
            await table.CreateAsync();

            await table.AddEntityAsync(new TableEntity("partition1", "row1") { { "Value", 10 } });
            await table.AddEntityAsync(new TableEntity("partition1", "row2") { { "Value", 20 } });
            await table.AddEntityAsync(new TableEntity("partition1", "row3") { { "Value", 30 } });

            // Act
            var entities = table.QueryAsync<TableEntity>(filter: "PartitionKey eq 'partition1'");
            var count = 0;
            await foreach (var e in entities)
            {
                count++;
            }

            // Assert
            count.Should().Be(3);
        }

        [TestMethod]
        public async Task Table_CanQueryWithFilter()
        {
            // Arrange
            var client = new TableServiceClient(ConnectionString);
            var tableName = $"test{Guid.NewGuid():N}".Substring(0, 20);
            var table = client.GetTableClient(tableName);
            await table.CreateAsync();

            await table.AddEntityAsync(new TableEntity("partition1", "row1") { { "Value", 10 } });
            await table.AddEntityAsync(new TableEntity("partition1", "row2") { { "Value", 20 } });
            await table.AddEntityAsync(new TableEntity("partition1", "row3") { { "Value", 30 } });

            // Act
            var entities = table.QueryAsync<TableEntity>(filter: "Value ge 20");
            var count = 0;
            await foreach (var e in entities)
            {
                count++;
            }

            // Assert
            count.Should().Be(2);
        }

        #endregion

    }

}
