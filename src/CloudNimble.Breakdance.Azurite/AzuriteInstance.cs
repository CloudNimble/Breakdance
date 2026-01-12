using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace CloudNimble.Breakdance.Azurite
{

    /// <summary>
    /// Represents a running Azurite instance with process lifecycle management.
    /// </summary>
    public class AzuriteInstance : IDisposable, IAsyncDisposable
    {

        #region Private Members

        private Process _process;
        private readonly AzuriteConfiguration _config;
        private bool _isRunning;
        private bool _disposed;
        private StringBuilder _outputBuffer = new();
        private StringBuilder _errorBuffer = new();
        private TaskCompletionSource<bool> _portsDetected = new();
        private int _successfulServicesCount = 0;
        private readonly Random _random = new();
        private int? _attemptingBlobPort;
        private int? _attemptingQueuePort;
        private int? _attemptingTablePort;

        // HTTP client for storage REST API calls
        private HttpClient _httpClient;
        private const string DefaultAccountName = "devstoreaccount1";
        private const string DefaultAccountKey = "Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==";
        private const string StorageApiVersion = "2020-10-02";

        #endregion

        #region Properties

        /// <summary>
        /// Gets the port number for the Blob service, or null if not started.
        /// </summary>
        public int? BlobPort { get; private set; }

        /// <summary>
        /// Gets the port number for the Queue service, or null if not started.
        /// </summary>
        public int? QueuePort { get; private set; }

        /// <summary>
        /// Gets the port number for the Table service, or null if not started.
        /// </summary>
        public int? TablePort { get; private set; }

        /// <summary>
        /// Gets the HTTP endpoint URL for the Blob service.
        /// Returns null if Blob service was not requested or not started.
        /// </summary>
        public string BlobEndpoint => _config.Services.HasFlag(AzuriteServiceType.Blob) && BlobPort.HasValue
            ? $"http://127.0.0.1:{BlobPort}"
            : null;

        /// <summary>
        /// Gets the HTTP endpoint URL for the Queue service.
        /// Returns null if Queue service was not requested or not started.
        /// </summary>
        public string QueueEndpoint => _config.Services.HasFlag(AzuriteServiceType.Queue) && QueuePort.HasValue
            ? $"http://127.0.0.1:{QueuePort}"
            : null;

        /// <summary>
        /// Gets the HTTP endpoint URL for the Table service.
        /// Returns null if Table service was not requested or not started.
        /// </summary>
        public string TableEndpoint => _config.Services.HasFlag(AzuriteServiceType.Table) && TablePort.HasValue
            ? $"http://127.0.0.1:{TablePort}"
            : null;

        /// <summary>
        /// Gets whether the Azurite instance is currently running.
        /// </summary>
        public bool IsRunning => _isRunning && _process != null && !_process.HasExited;

        /// <summary>
        /// Gets the standard output captured from the Azurite process.
        /// </summary>
        public string StandardOutput => _outputBuffer.ToString();

        /// <summary>
        /// Gets the standard error captured from the Azurite process.
        /// </summary>
        public string StandardError => _errorBuffer.ToString();

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new <see cref="AzuriteInstance"/> with the specified configuration.
        /// </summary>
        /// <param name="configuration">The configuration options.</param>
        public AzuriteInstance(AzuriteConfiguration configuration = null)
        {
            _config = configuration ?? new AzuriteConfiguration();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Starts the Azurite instance asynchronously.
        /// Includes automatic retry logic for port conflicts when <see cref="AzuriteConfiguration.AutoAssignPorts"/> is true.
        /// </summary>
        /// <returns>A task that completes when Azurite is ready to accept connections.</returns>
        public async Task StartAsync()
        {
            // If already running, no need to start
            if (_isRunning) return;

            var maxRetries = _config.AutoAssignPorts ? _config.MaxRetries : 1;

            for (int attempt = 0; attempt < maxRetries; attempt++)
            {
                try
                {
                    // Assign random ports if needed
                    AssignPortsIfNeeded();

                    // Start the process and wait for ready
                    await StartProcessAsync();
                    await WaitForReadyAsync();

                    // Success!
                    _isRunning = true;

                    System.Diagnostics.Debug.WriteLine($"[AzuriteInstance] Successfully started on ports " +
                        $"Blob={BlobPort}, Queue={QueuePort}, Table={TablePort}");

                    return;
                }
                catch (InvalidOperationException ex) when (_config.AutoAssignPorts && IsPortConflict(ex))
                {
                    System.Diagnostics.Debug.WriteLine($"[AzuriteInstance] Port conflict on attempt {attempt + 1}: {ex.Message}");

                    // Clean up failed process and reset state for retry
                    await CleanupFailedProcessAsync();

                    if (attempt == maxRetries - 1)
                    {
                        throw new InvalidOperationException(
                            $"Failed to start Azurite after {maxRetries} attempts. All port ranges tested were in use.\n" +
                            $"Last error: {ex.Message}", ex);
                    }

                    // Brief delay before retry
                    await Task.Delay(50);
                }
            }
        }

        /// <summary>
        /// Starts the Azurite process (internal helper for retry logic).
        /// </summary>
        private async Task StartProcessAsync()
        {
            // Build command arguments
            var args = BuildArguments();

            // Determine which Azurite command to use based on services requested
            var command = GetAzuriteCommand();

            // Get working directory
            var workingDirectory = GetAzuriteDirectory();

            // Calculate appropriate Node.js heap size:
            // - If ExtentMemoryLimitMB is set, add 128MB overhead for Azurite's internal structures
            // - Otherwise, use 256MB default (sufficient for basic in-memory operations)
            var heapSizeMB = _config.ExtentMemoryLimitMB.HasValue
                ? _config.ExtentMemoryLimitMB.Value + 128
                : 256;

            // Set NODE_OPTIONS to limit Node.js heap size to prevent OOM errors
            // when multiple Azurite instances run in parallel (e.g., parallel tests)
            var nodeOptions = $"--max-old-space-size={heapSizeMB}";
            var fullCommand = $"set NODE_OPTIONS={nodeOptions} && npx {command} {args}";

            // Build the window title for process identification
            var instanceName = string.IsNullOrWhiteSpace(_config.InstanceName) ? "Unknown" : _config.InstanceName;
            var windowTitle = $"Breakdance.Azurite - {instanceName}";

            // Use cmd.exe /k (instead of /c) to keep cmd.exe alive.
            // This maintains the process tree so Kill(entireProcessTree: true) works correctly.
            // With /c, cmd.exe exits immediately after spawning npx, orphaning the Node processes.
            _process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/k title {windowTitle} && {fullCommand}",
                    WorkingDirectory = workingDirectory,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    RedirectStandardInput = true,
                    StandardOutputEncoding = System.Text.Encoding.UTF8,
                    StandardErrorEncoding = System.Text.Encoding.UTF8
                }
            };

            // Log the command for debugging
            _outputBuffer.AppendLine($"[DEBUG] Starting process: cmd.exe /k title {windowTitle} && {fullCommand}");
            _outputBuffer.AppendLine($"[DEBUG] Working directory: {workingDirectory}");
            _outputBuffer.AppendLine($"[DEBUG] Instance name: {instanceName}");

            // Capture output and parse ports
            _process.OutputDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    _outputBuffer.AppendLine(e.Data);
                    ParsePortFromOutput(e.Data);
                }
            };

            _process.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                    _errorBuffer.AppendLine(e.Data);
            };

            _process.Start();

            // Assign the process to a Windows Job Object so it's automatically killed
            // when the parent .NET process exits (for any reason: crash, Ctrl+C, debugger detach, etc.)
            WindowsJobObject.AssignProcess(_process);

            _process.BeginOutputReadLine();
            _process.BeginErrorReadLine();
        }

        /// <summary>
        /// Stops the Azurite instance asynchronously.
        /// </summary>
        /// <returns>A task that completes when Azurite has stopped.</returns>
        public async Task StopAsync()
        {
            if (!_isRunning || _process == null)
                return;

            try
            {
                if (!_process.HasExited)
                {
                    // Try graceful shutdown first
                    try
                    {
                        // Send Ctrl+C to the process to initiate graceful shutdown
                        // For cmd.exe /k, we need to send an exit command through stdin
                        try
                        {
                            await _process.StandardInput.WriteLineAsync("exit");
                            await _process.StandardInput.FlushAsync();
                        }
                        catch
                        {
                            // Ignore errors writing to stdin
                        }

                        _process.StandardInput.Close();

                        // Wait up to 3 seconds for graceful exit
                        var gracefulCts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
                        await _process.WaitForExitAsync(gracefulCts.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        // Graceful shutdown timed out, will force kill below
                    }
                    catch
                    {
                        // Ignore errors during graceful shutdown, will force kill below
                    }

                    // Force kill entire process tree if still running
                    if (!_process.HasExited)
                    {
                        try
                        {
                            _process.Kill(entireProcessTree: true);

                            // Wait up to 5 seconds for force kill to complete
                            var forceCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                            await _process.WaitForExitAsync(forceCts.Token);
                        }
                        catch (OperationCanceledException)
                        {
                            // Process still running after force kill - log but continue
                        }
                        catch
                        {
                            // Ignore kill errors
                        }
                    }
                }
            }
            finally
            {
                _isRunning = false;
                BlobPort = null;
                QueuePort = null;
                TablePort = null;
                _attemptingBlobPort = null;
                _attemptingQueuePort = null;
                _attemptingTablePort = null;
            }
        }

        /// <summary>
        /// Gets a connection string for the Development Storage account.
        /// Only includes endpoints for services that were requested.
        /// </summary>
        /// <param name="accountName">The account name. Defaults to "devstoreaccount1".</param>
        /// <param name="accountKey">The account key. Defaults to the well-known development key.</param>
        /// <returns>A connection string that can be used with Azure Storage SDKs.</returns>
        public string GetConnectionString(
            string accountName = "devstoreaccount1",
            string accountKey = "Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==")
        {
            var parts = new List<string>
            {
                "DefaultEndpointsProtocol=http",
                $"AccountName={accountName}",
                $"AccountKey={accountKey}"
            };

            // Only include endpoints for requested services
            if (BlobEndpoint != null)
                parts.Add($"BlobEndpoint={BlobEndpoint}/{accountName}");
            if (QueueEndpoint != null)
                parts.Add($"QueueEndpoint={QueueEndpoint}/{accountName}");
            if (TableEndpoint != null)
                parts.Add($"TableEndpoint={TableEndpoint}/{accountName}");

            return string.Join(";", parts) + ";";
        }

        /// <summary>
        /// Disposes the Azurite instance and releases all resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Asynchronously disposes the Azurite instance and releases all resources.
        /// </summary>
        public async ValueTask DisposeAsync()
        {
            await DisposeAsyncCore().ConfigureAwait(false);
            Dispose(disposing: false);
            GC.SuppressFinalize(this);
        }

        #endregion

        #region Storage Cleanup Methods

        /// <summary>
        /// Clears all messages from a queue. Does nothing if the queue doesn't exist.
        /// </summary>
        /// <param name="queueName">The name of the queue to clear.</param>
        /// <returns>A task that completes when the operation finishes.</returns>
        public async Task ClearQueueMessagesAsync(string queueName)
        {
            if (string.IsNullOrWhiteSpace(queueName))
                throw new ArgumentException("Queue name cannot be null or empty.", nameof(queueName));

            if (QueueEndpoint == null)
                throw new InvalidOperationException("Queue service is not running.");

            using var response = await SendStorageRequestAsync(
                HttpMethod.Delete,
                QueueEndpoint,
                $"/{queueName}/messages").ConfigureAwait(false);

            // Accept success or 404 (queue doesn't exist) - idempotent operation
            if (!response.IsSuccessStatusCode && response.StatusCode != System.Net.HttpStatusCode.NotFound)
            {
                throw new InvalidOperationException($"Failed to clear queue '{queueName}': {response.StatusCode}");
            }
        }

        /// <summary>
        /// Deletes a queue. Does nothing if the queue doesn't exist.
        /// </summary>
        /// <param name="queueName">The name of the queue to delete.</param>
        /// <returns>A task that completes when the operation finishes.</returns>
        public async Task DeleteQueueAsync(string queueName)
        {
            if (string.IsNullOrWhiteSpace(queueName))
                throw new ArgumentException("Queue name cannot be null or empty.", nameof(queueName));

            if (QueueEndpoint == null)
                throw new InvalidOperationException("Queue service is not running.");

            using var response = await SendStorageRequestAsync(
                HttpMethod.Delete,
                QueueEndpoint,
                $"/{queueName}").ConfigureAwait(false);

            // Accept success or 404 (queue doesn't exist) - idempotent operation
            if (!response.IsSuccessStatusCode && response.StatusCode != System.Net.HttpStatusCode.NotFound)
            {
                throw new InvalidOperationException($"Failed to delete queue '{queueName}': {response.StatusCode}");
            }
        }

        /// <summary>
        /// Lists all queues in the storage account.
        /// </summary>
        /// <returns>A list of queue names.</returns>
        public async Task<List<string>> ListQueuesAsync()
        {
            if (QueueEndpoint == null)
                throw new InvalidOperationException("Queue service is not running.");

            using var response = await SendStorageRequestAsync(
                HttpMethod.Get,
                QueueEndpoint,
                "?comp=list").ConfigureAwait(false);

            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var doc = XDocument.Parse(content);
            var queues = new List<string>();

            foreach (var queue in doc.Descendants("Queue"))
            {
                var name = queue.Element("Name")?.Value;
                if (!string.IsNullOrEmpty(name))
                    queues.Add(name);
            }

            return queues;
        }

        /// <summary>
        /// Deletes all queues in the storage account.
        /// </summary>
        /// <returns>A task that completes when all queues are deleted.</returns>
        public async Task ClearAllQueuesAsync()
        {
            var queues = await ListQueuesAsync().ConfigureAwait(false);
            foreach (var queue in queues)
            {
                await DeleteQueueAsync(queue).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Deletes a blob container. Does nothing if the container doesn't exist.
        /// </summary>
        /// <param name="containerName">The name of the container to delete.</param>
        /// <returns>A task that completes when the operation finishes.</returns>
        public async Task DeleteBlobContainerAsync(string containerName)
        {
            if (string.IsNullOrWhiteSpace(containerName))
                throw new ArgumentException("Container name cannot be null or empty.", nameof(containerName));

            if (BlobEndpoint == null)
                throw new InvalidOperationException("Blob service is not running.");

            using var response = await SendStorageRequestAsync(
                HttpMethod.Delete,
                BlobEndpoint,
                $"/{containerName}?restype=container").ConfigureAwait(false);

            // Accept success or 404 (container doesn't exist) - idempotent operation
            if (!response.IsSuccessStatusCode && response.StatusCode != System.Net.HttpStatusCode.NotFound)
            {
                throw new InvalidOperationException($"Failed to delete container '{containerName}': {response.StatusCode}");
            }
        }

        /// <summary>
        /// Lists all blob containers in the storage account.
        /// </summary>
        /// <returns>A list of container names.</returns>
        public async Task<List<string>> ListBlobContainersAsync()
        {
            if (BlobEndpoint == null)
                throw new InvalidOperationException("Blob service is not running.");

            using var response = await SendStorageRequestAsync(
                HttpMethod.Get,
                BlobEndpoint,
                "/?comp=list").ConfigureAwait(false);

            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var doc = XDocument.Parse(content);
            var containers = new List<string>();

            foreach (var container in doc.Descendants("Container"))
            {
                var name = container.Element("Name")?.Value;
                if (!string.IsNullOrEmpty(name))
                    containers.Add(name);
            }

            return containers;
        }

        /// <summary>
        /// Deletes all blob containers in the storage account.
        /// </summary>
        /// <returns>A task that completes when all containers are deleted.</returns>
        public async Task ClearAllBlobContainersAsync()
        {
            var containers = await ListBlobContainersAsync().ConfigureAwait(false);
            foreach (var container in containers)
            {
                await DeleteBlobContainerAsync(container).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Deletes a table. Does nothing if the table doesn't exist.
        /// </summary>
        /// <param name="tableName">The name of the table to delete.</param>
        /// <returns>A task that completes when the operation finishes.</returns>
        public async Task DeleteTableAsync(string tableName)
        {
            if (string.IsNullOrWhiteSpace(tableName))
                throw new ArgumentException("Table name cannot be null or empty.", nameof(tableName));

            if (TableEndpoint == null)
                throw new InvalidOperationException("Table service is not running.");

            // Table service requires OData headers
            var tableHeaders = new Dictionary<string, string>
            {
                ["Accept"] = "application/json;odata=minimalmetadata",
                ["DataServiceVersion"] = "3.0"
            };

            using var response = await SendStorageRequestAsync(
                HttpMethod.Delete,
                TableEndpoint,
                $"/Tables('{tableName}')",
                tableHeaders,
                useTableAuth: true).ConfigureAwait(false);

            // Accept success or 404 (table doesn't exist) - idempotent operation
            if (!response.IsSuccessStatusCode && response.StatusCode != System.Net.HttpStatusCode.NotFound)
            {
                throw new InvalidOperationException($"Failed to delete table '{tableName}': {response.StatusCode}");
            }
        }

        /// <summary>
        /// Lists all tables in the storage account.
        /// </summary>
        /// <returns>A list of table names.</returns>
        public async Task<List<string>> ListTablesAsync()
        {
            if (TableEndpoint == null)
                throw new InvalidOperationException("Table service is not running.");

            // Table service requires OData headers
            var tableHeaders = new Dictionary<string, string>
            {
                ["Accept"] = "application/json;odata=minimalmetadata",
                ["DataServiceVersion"] = "3.0"
            };

            using var response = await SendStorageRequestAsync(
                HttpMethod.Get,
                TableEndpoint,
                "/Tables",
                tableHeaders,
                useTableAuth: true).ConfigureAwait(false);

            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            // Table service returns JSON, not XML
            var tables = new List<string>();

            // Simple JSON parsing for table names - format: {"value":[{"TableName":"name1"},{"TableName":"name2"}]}
            // Using basic string parsing to avoid adding System.Text.Json dependency concerns
            var startIndex = 0;
            while ((startIndex = content.IndexOf("\"TableName\":", startIndex, StringComparison.Ordinal)) != -1)
            {
                startIndex += 12; // Skip past "TableName":
                var valueStart = content.IndexOf('"', startIndex);
                if (valueStart == -1) break;
                var valueEnd = content.IndexOf('"', valueStart + 1);
                if (valueEnd == -1) break;
                tables.Add(content.Substring(valueStart + 1, valueEnd - valueStart - 1));
                startIndex = valueEnd + 1;
            }

            return tables;
        }

        /// <summary>
        /// Deletes all tables in the storage account.
        /// </summary>
        /// <returns>A task that completes when all tables are deleted.</returns>
        public async Task ClearAllTablesAsync()
        {
            var tables = await ListTablesAsync().ConfigureAwait(false);
            foreach (var table in tables)
            {
                await DeleteTableAsync(table).ConfigureAwait(false);
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Gets the appropriate Azurite command based on which services are requested.
        /// </summary>
        /// <returns>The Azurite command to execute (azurite, azurite-blob, azurite-queue, or azurite-table).</returns>
        private string GetAzuriteCommand()
        {
            // Use service-specific commands when only one service is requested
            return _config.Services switch
            {
                AzuriteServiceType.Blob => "azurite-blob",
                AzuriteServiceType.Queue => "azurite-queue",
                // NOTE: Table-only mode is disabled due to a bug in Azurite where azurite-table
                // reports port 0 in its output instead of the actual bound port when using dynamic ports.
                // See: https://github.com/Azure/Azurite - table/main.ts uses config.port instead of server.getHttpServerAddress()
                // Once this is fixed upstream, uncomment the line below:
                // AzuriteServiceType.Table => "azurite-table",
                AzuriteServiceType.Table => throw new NotSupportedException(
                    "Table-only mode is not currently supported due to an Azurite bug. " +
                    "Please use AzuriteServiceType.All instead, which starts all services including Table. " +
                    "See: https://github.com/Azure/Azurite/issues for the upstream bug."),
                _ => "azurite" // Default to all services
            };
        }

        /// <summary>
        /// Parses port information from Azurite's standard output.
        /// Azurite outputs two types of messages:
        /// 1. "Azurite Blob service is starting on 127.0.0.1:PORT" - BEFORE port is bound (uses config port, may be 0)
        /// 2. "Azurite Blob service successfully listens on http://127.0.0.1:PORT" - AFTER port is bound (uses actual port)
        ///
        /// We track "attempting" ports from the "starting" messages for retry logic,
        /// and set actual ports from the "successfully" messages.
        /// </summary>
        /// <param name="output">A line of output from Azurite.</param>
        private void ParsePortFromOutput(string output)
        {
            try
            {
                // Determine which service this message is about
                string serviceType = null;
                if (output.Contains("Blob", StringComparison.OrdinalIgnoreCase))
                    serviceType = "Blob";
                else if (output.Contains("Queue", StringComparison.OrdinalIgnoreCase))
                    serviceType = "Queue";
                else if (output.Contains("Table", StringComparison.OrdinalIgnoreCase))
                    serviceType = "Table";

                if (serviceType == null)
                    return;

                // Parse the port from this line - support multiple URL formats
                int? parsedPort = ParsePortFromLine(output);

                // Check for "starting on" messages - these set the "attempting" port for retry logic
                // The port here may be 0 if using dynamic port assignment
                if (output.Contains("is starting on", StringComparison.OrdinalIgnoreCase))
                {
                    if (parsedPort.HasValue)
                    {
                        switch (serviceType)
                        {
                            case "Blob": _attemptingBlobPort = parsedPort; break;
                            case "Queue": _attemptingQueuePort = parsedPort; break;
                            case "Table": _attemptingTablePort = parsedPort; break;
                        }
                    }
                }
                // Check for "success" messages - these set the actual port (service is now listening)
                else if (output.Contains("success", StringComparison.OrdinalIgnoreCase))
                {
                    _successfulServicesCount++;

                    // Only set port if it's a valid port (not 0)
                    // Port 0 means the OS assigned a port, but the message still shows 0
                    // This happens with azurite-table due to an upstream bug
                    if (parsedPort.HasValue && parsedPort.Value > 0)
                    {
                        switch (serviceType)
                        {
                            case "Blob": BlobPort = parsedPort; break;
                            case "Queue": QueuePort = parsedPort; break;
                            case "Table": TablePort = parsedPort; break;
                        }
                    }

                    // Check if all expected services are successfully listening
                    int expectedServices = 0;
                    if (_config.Services.HasFlag(AzuriteServiceType.Blob)) expectedServices++;
                    if (_config.Services.HasFlag(AzuriteServiceType.Queue)) expectedServices++;
                    if (_config.Services.HasFlag(AzuriteServiceType.Table)) expectedServices++;

                    if (_successfulServicesCount >= expectedServices)
                    {
                        _portsDetected.TrySetResult(true);
                    }
                }
            }
            catch (Exception ex)
            {
                // Log parsing errors but don't throw - we're in an event handler
                _errorBuffer.AppendLine($"Exception parsing Azurite output: {ex.Message}");
                _errorBuffer.AppendLine($"Output line: {output}");
            }
        }

        /// <summary>
        /// Parses a port number from a line of Azurite output.
        /// </summary>
        /// <param name="output">The output line to parse.</param>
        /// <returns>The parsed port number, or null if not found.</returns>
        private int? ParsePortFromLine(string output)
        {
            // Format 1: "http://127.0.0.1:10000" or "https://..."
            var match = System.Text.RegularExpressions.Regex.Match(output, @"https?://[^:]+:(\d+)");

            if (!match.Success)
            {
                // Format 2: "127.0.0.1:10000" (without http prefix)
                match = System.Text.RegularExpressions.Regex.Match(output, @"\d+\.\d+\.\d+\.\d+:(\d+)");
            }

            if (!match.Success)
            {
                // Format 3: "port 10000" or "port: 10000"
                match = System.Text.RegularExpressions.Regex.Match(output, @"port\s*:?\s*(\d+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            }

            if (match.Success && int.TryParse(match.Groups[1].Value, out var port))
            {
                return port;
            }

            return null;
        }

        /// <summary>
        /// Builds the command-line arguments for Azurite.
        /// </summary>
        /// <returns>The arguments string.</returns>
        private string BuildArguments()
        {
            var args = new List<string>();

            if (_config.InMemoryPersistence)
            {
                args.Add("--inMemoryPersistence");

                if (_config.ExtentMemoryLimitMB.HasValue)
                    args.Add($"--extentMemoryLimit {_config.ExtentMemoryLimitMB.Value}");
            }
            else if (!string.IsNullOrWhiteSpace(_config.Location))
            {
                args.Add($"--location \"{_config.Location}\"");
            }

            if (_config.Silent)
                args.Add("--silent");

            if (_config.LooseMode)
                args.Add("--loose");

            if (_config.SkipApiVersionCheck)
                args.Add("--skipApiVersionCheck");

            if (_config.DisableTelemetry)
                args.Add("--disableTelemetry");

            if (!string.IsNullOrWhiteSpace(_config.DebugLogPath))
                args.Add($"--debug \"{_config.DebugLogPath}\"");

            // Only specify ports if explicitly configured
            if (_config.BlobPort.HasValue)
                args.Add($"--blobPort {_config.BlobPort.Value}");
            if (_config.QueuePort.HasValue)
                args.Add($"--queuePort {_config.QueuePort.Value}");
            if (_config.TablePort.HasValue)
                args.Add($"--tablePort {_config.TablePort.Value}");

            return string.Join(" ", args);
        }

        /// <summary>
        /// Gets the directory containing the Azurite node_modules, or null if using global install.
        /// </summary>
        /// <returns>The directory path where node_modules exists, or null if Azurite is globally installed.</returns>
        private string GetAzuriteDirectory()
        {
            try
            {
                // Check if Azurite is globally installed
                using (var globalCheck = Process.Start(new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = "/c npm list -g azurite --depth=0",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }))
                {
                    globalCheck?.WaitForExit();

                    if (globalCheck?.ExitCode == 0)
                    {
                        // Azurite is globally installed, npx will find it automatically
                        // Return current directory as working directory
                        return Environment.CurrentDirectory;
                    }
                }

                // Fall back to checking for local node_modules
                var assemblyLocation = typeof(AzuriteInstance).Assembly.Location;
                var assemblyDir = Path.GetDirectoryName(assemblyLocation);
                var nodeModulesPath = Path.Combine(assemblyDir, "node_modules");

                // Verify node_modules exists
                if (!Directory.Exists(nodeModulesPath))
                {
                    throw new DirectoryNotFoundException(
                        $"Azurite not found. Neither global install nor local node_modules found.\n" +
                        $"Please install Azurite globally: npm install -g azurite\n" +
                        $"Or ensure the Breakdance.Azurite NuGet package build target ran successfully.");
                }

                // Verify azurite package exists within node_modules
                var azuritePackagePath = Path.Combine(nodeModulesPath, "azurite");
                if (!Directory.Exists(azuritePackagePath))
                {
                    throw new DirectoryNotFoundException(
                        $"Azurite package not found at '{azuritePackagePath}'.\n" +
                        $"Please install Azurite globally: npm install -g azurite");
                }

                // Return the assembly directory since npx will look for azurite in node_modules
                return assemblyDir;
            }
            catch (Exception ex) when (!(ex is DirectoryNotFoundException || ex is InvalidOperationException))
            {
                throw new InvalidOperationException(
                    $"Failed to determine Azurite directory: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Waits for Azurite services to be ready to accept connections.
        /// </summary>
        /// <returns>A task that completes when services are ready.</returns>
        private async Task WaitForReadyAsync()
        {
            var timeout = TimeSpan.FromSeconds(_config.StartupTimeoutSeconds);
            var stopwatch = Stopwatch.StartNew();

            // First wait for ports to be detected from output, but poll for errors
            var portsDetectionTask = _portsDetected.Task;
            var timeoutTask = Task.Delay(timeout);

            while (!portsDetectionTask.IsCompleted && stopwatch.Elapsed < timeout)
            {
                // Check for fatal errors (fail fast)
                CheckForFatalErrors();

                // Check if process has exited
                if (_process.HasExited)
                {
                    throw new InvalidOperationException(
                        $"Azurite process exited unexpectedly with code {_process.ExitCode}.\n" +
                        $"Output: {StandardOutput}\n" +
                        $"Error: {StandardError}");
                }

                // Wait a bit before checking again
                await Task.WhenAny(portsDetectionTask, Task.Delay(100));
            }

            if (!portsDetectionTask.IsCompleted)
            {
                throw new TimeoutException(
                    $"Azurite failed to start within {timeout.TotalSeconds} seconds (ports not detected).\n" +
                    $"Output: {StandardOutput}\n" +
                    $"Error: {StandardError}");
            }

            // Ports detected from STDOUT - Azurite is ready!
            // The "service is starting at" messages from Azurite indicate the services are listening and ready.
            // No need for additional HTTP polling which adds unnecessary delay.
        }

        /// <summary>
        /// Assigns random ports to the configuration if AutoAssignPorts is enabled and ports aren't set.
        /// </summary>
        private void AssignPortsIfNeeded()
        {
            if (!_config.AutoAssignPorts) return;

            var basePort = _random.Next(20000, 30000);

            if (_config.Services.HasFlag(AzuriteServiceType.Blob) && !_config.BlobPort.HasValue)
                _config.BlobPort = basePort;
            if (_config.Services.HasFlag(AzuriteServiceType.Queue) && !_config.QueuePort.HasValue)
                _config.QueuePort = basePort + 1;
            if (_config.Services.HasFlag(AzuriteServiceType.Table) && !_config.TablePort.HasValue)
                _config.TablePort = basePort + 2;

            System.Diagnostics.Debug.WriteLine($"[AzuriteInstance] Assigned ports: Blob={_config.BlobPort}, Queue={_config.QueuePort}, Table={_config.TablePort}");
        }

        /// <summary>
        /// Cleans up a failed process and resets state for retry.
        /// </summary>
        private async Task CleanupFailedProcessAsync()
        {
            // Kill the failed process
            if (_process != null)
            {
                try
                {
                    if (!_process.HasExited)
                    {
                        _process.Kill(entireProcessTree: true);
                    }
                }
                catch
                {
                    // Ignore kill errors
                }

                _process.Dispose();
                _process = null;
            }

            // Clear ports so they get reassigned on next attempt
            _config.BlobPort = null;
            _config.QueuePort = null;
            _config.TablePort = null;

            // Reset port properties
            BlobPort = null;
            QueuePort = null;
            TablePort = null;

            // Reset detection state
            _outputBuffer = new StringBuilder();
            _errorBuffer = new StringBuilder();
            _successfulServicesCount = 0;
            _portsDetected = new TaskCompletionSource<bool>();

            // Brief delay to ensure ports are released
            await Task.Delay(100);
        }

        /// <summary>
        /// Determines if an exception represents a port conflict.
        /// </summary>
        private static bool IsPortConflict(Exception ex) =>
            ex.Message.Contains("EADDRINUSE") ||
            ex.Message.Contains("address already in use") ||
            ex.Message.Contains("Port conflict") ||
            ex.Message.Contains("port is already in use");

        /// <summary>
        /// Checks for fatal errors in the output and error buffers and throws if found.
        /// Port conflict errors (EADDRINUSE) are thrown separately so they can be retried at a higher level.
        /// </summary>
        private void CheckForFatalErrors()
        {
            var errorOutput = StandardError;
            var standardOutput = StandardOutput;
            var combinedOutput = errorOutput + standardOutput;

            // Check for Node.js/V8 out-of-memory errors (fail fast - no point waiting)
            if (combinedOutput.Contains("Fatal process out of memory") ||
                combinedOutput.Contains("FATAL ERROR: CALL_AND_RETRY_LAST") ||
                combinedOutput.Contains("JavaScript heap out of memory") ||
                combinedOutput.Contains("Allocation failed"))
            {
                throw new OutOfMemoryException(
                    $"Node.js ran out of memory while starting Azurite. " +
                    $"Try setting ExtentMemoryLimitMB to a lower value or disable InMemoryPersistence.\n" +
                    $"Output: {standardOutput}\n" +
                    $"Error: {errorOutput}");
            }

            // Check for port conflict - this is a special case that can be retried
            if (combinedOutput.Contains("EADDRINUSE") ||
                combinedOutput.Contains("address already in use") ||
                combinedOutput.Contains("port is already in use") ||
                combinedOutput.Contains("listen EADDRINUSE"))
            {
                throw new InvalidOperationException(
                    $"Port conflict detected - one or more ports are already in use.\n" +
                    $"Output: {standardOutput}\n" +
                    $"Error: {errorOutput}");
            }

            // Check for npm/npx related errors
            if (combinedOutput.Contains("npm ERR!") ||
                combinedOutput.Contains("npx: ERR!") ||
                combinedOutput.Contains("command not found") ||
                combinedOutput.Contains("is not recognized") ||
                combinedOutput.Contains("ENOENT") && combinedOutput.Contains("npx"))
            {
                throw new InvalidOperationException(
                    $"npm/npx command error detected. Ensure Node.js and npm are installed and in PATH.\n" +
                    $"Output: {standardOutput}\n" +
                    $"Error: {errorOutput}");
            }

            // Check for other fatal errors
            if (errorOutput.Contains("Error:") ||
                errorOutput.Contains("ERROR") ||
                combinedOutput.Contains("EACCES") ||
                combinedOutput.Contains("ENOENT") ||
                combinedOutput.Contains("MODULE_NOT_FOUND") ||
                combinedOutput.Contains("SyntaxError") ||
                combinedOutput.Contains("ReferenceError") ||
                combinedOutput.Contains("TypeError") ||
                combinedOutput.Contains("Cannot find module") ||
                combinedOutput.Contains("Failed to start"))
            {
                throw new InvalidOperationException(
                    $"Azurite encountered a fatal error during startup.\n" +
                    $"Output: {standardOutput}\n" +
                    $"Error: {errorOutput}");
            }
        }

        /// <summary>
        /// Ensures the HTTP client is initialized.
        /// </summary>
        private HttpClient EnsureHttpClient()
        {
            _httpClient ??= new HttpClient();
            return _httpClient;
        }

        /// <summary>
        /// Computes the SharedKey authorization signature for Azure Storage REST API requests (Blob/Queue services).
        /// </summary>
        /// <param name="method">The HTTP method.</param>
        /// <param name="canonicalizedResource">The canonicalized resource string.</param>
        /// <param name="date">The RFC1123 formatted date.</param>
        /// <param name="canonicalizedHeaders">The canonicalized headers string.</param>
        /// <param name="contentLength">The content length (empty string for no body).</param>
        /// <param name="contentType">The content type (empty string for no body).</param>
        /// <returns>The authorization header value.</returns>
        private string ComputeSharedKeySignature(
            HttpMethod method,
            string canonicalizedResource,
            string date,
            string canonicalizedHeaders,
            string contentLength = "",
            string contentType = "")
        {
            // String to sign format for Blob/Queue services
            // VERB\n
            // Content-Encoding\n
            // Content-Language\n
            // Content-Length\n
            // Content-MD5\n
            // Content-Type\n
            // Date\n
            // If-Modified-Since\n
            // If-Match\n
            // If-None-Match\n
            // If-Unmodified-Since\n
            // Range\n
            // CanonicalizedHeaders
            // CanonicalizedResource

            var stringToSign = string.Join("\n",
                method.Method.ToUpperInvariant(),
                "", // Content-Encoding
                "", // Content-Language
                contentLength, // Content-Length
                "", // Content-MD5
                contentType, // Content-Type
                "", // Date (we use x-ms-date instead)
                "", // If-Modified-Since
                "", // If-Match
                "", // If-None-Match
                "", // If-Unmodified-Since
                "", // Range
                canonicalizedHeaders + canonicalizedResource);

            Debug.WriteLine($"[AzuriteInstance] StringToSign (escaped): {stringToSign.Replace("\n", "\\n")}");

            using var hmac = new HMACSHA256(Convert.FromBase64String(DefaultAccountKey));
            var signature = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(stringToSign)));
            return $"SharedKey {DefaultAccountName}:{signature}";
        }

        /// <summary>
        /// Computes the SharedKey authorization signature for Azure Table Storage REST API requests.
        /// Table service uses a different (simpler) string-to-sign format than Blob/Queue.
        /// </summary>
        /// <param name="method">The HTTP method.</param>
        /// <param name="canonicalizedResource">The canonicalized resource string.</param>
        /// <param name="date">The RFC1123 formatted date.</param>
        /// <param name="contentType">The content type (empty string for no body).</param>
        /// <returns>The authorization header value.</returns>
        private string ComputeTableSharedKeySignature(
            HttpMethod method,
            string canonicalizedResource,
            string date,
            string contentType = "")
        {
            // Table service uses a different string to sign format:
            // VERB\n
            // Content-MD5\n
            // Content-Type\n
            // Date\n
            // CanonicalizedResource

            var stringToSign = string.Join("\n",
                method.Method.ToUpperInvariant(),
                "", // Content-MD5
                contentType, // Content-Type
                date, // Date (use the date value directly, not x-ms-date)
                canonicalizedResource);

            Debug.WriteLine($"[AzuriteInstance] Table StringToSign (escaped): {stringToSign.Replace("\n", "\\n")}");

            using var hmac = new HMACSHA256(Convert.FromBase64String(DefaultAccountKey));
            var signature = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(stringToSign)));
            return $"SharedKey {DefaultAccountName}:{signature}";
        }

        /// <summary>
        /// Sends an authenticated HTTP request to the Azure Storage REST API.
        /// </summary>
        /// <param name="method">The HTTP method.</param>
        /// <param name="endpoint">The service endpoint URL.</param>
        /// <param name="path">The resource path (can include query parameters like ?comp=list).</param>
        /// <param name="additionalHeaders">Additional headers to include.</param>
        /// <param name="useTableAuth">True to use Table service authentication format.</param>
        /// <returns>The HTTP response.</returns>
        private async Task<HttpResponseMessage> SendStorageRequestAsync(
            HttpMethod method,
            string endpoint,
            string path,
            Dictionary<string, string> additionalHeaders = null,
            bool useTableAuth = false)
        {
            var client = EnsureHttpClient();
            var date = DateTime.UtcNow.ToString("R");
            var url = $"{endpoint}/{DefaultAccountName}{path}";

            using var request = new HttpRequestMessage(method, url);

            // Add required headers
            request.Headers.Add("x-ms-date", date);
            request.Headers.Add("x-ms-version", StorageApiVersion);

            // Add any additional headers
            if (additionalHeaders != null)
            {
                foreach (var header in additionalHeaders)
                {
                    request.Headers.Add(header.Key, header.Value);
                }
            }

            // Build canonicalized headers (sorted, lowercase)
            var canonicalizedHeaders = $"x-ms-date:{date}\nx-ms-version:{StorageApiVersion}\n";

            // Build canonicalized resource - separate path and query parameters
            // For path-based endpoints (Azurite), the format is:
            // /{account}/{account}{resource-path}\n{query-params}
            // The account name appears twice: once as the signing account, once as the URL path component
            string canonicalizedResource;
            var queryIndex = path.IndexOf('?');
            if (queryIndex >= 0)
            {
                // Parse path and query separately
                var pathPart = path[..queryIndex];
                var queryPart = path[(queryIndex + 1)..];

                // For path-based endpoints, resource is /account/account/path
                // The URL path is /devstoreaccount1{pathPart}, so canonicalized is /devstoreaccount1/devstoreaccount1{pathPart}
                canonicalizedResource = $"/{DefaultAccountName}/{DefaultAccountName}{pathPart}";

                // Parse and sort query parameters for canonicalization
                var queryParams = new SortedDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var param in queryPart.Split('&'))
                {
                    var parts = param.Split('=');
                    var key = parts[0].ToLowerInvariant();
                    var value = parts.Length > 1 ? parts[1] : "";
                    queryParams[key] = value;
                }

                // Append query parameters in sorted order
                foreach (var kvp in queryParams)
                {
                    canonicalizedResource += $"\n{kvp.Key}:{kvp.Value}";
                }
            }
            else
            {
                canonicalizedResource = $"/{DefaultAccountName}/{DefaultAccountName}{path}";
            }

            // Compute and add authorization
            string auth;
            if (useTableAuth)
            {
                // Table service uses a different string-to-sign format
                auth = ComputeTableSharedKeySignature(method, canonicalizedResource, date);
            }
            else
            {
                auth = ComputeSharedKeySignature(method, canonicalizedResource, date, canonicalizedHeaders);
            }
            request.Headers.Add("Authorization", auth);

            Debug.WriteLine($"[AzuriteInstance] URL: {url}");
            Debug.WriteLine($"[AzuriteInstance] Authorization: {auth}");
            Debug.WriteLine($"[AzuriteInstance] CanonicalizedResource: {canonicalizedResource.Replace("\n", "\\n")}");
            Debug.WriteLine($"[AzuriteInstance] CanonicalizedHeaders: {canonicalizedHeaders.Replace("\n", "\\n")}");

            var response = await client.SendAsync(request).ConfigureAwait(false);

            // Log error responses for debugging
            if (!response.IsSuccessStatusCode && response.StatusCode != System.Net.HttpStatusCode.NotFound)
            {
                var errorBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                Debug.WriteLine($"[AzuriteInstance] Error Response {(int)response.StatusCode}: {errorBody}");
            }

            return response;
        }

        /// <summary>
        /// Disposes the instance.
        /// </summary>
        /// <param name="disposing">Whether to dispose managed resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                StopAsync().GetAwaiter().GetResult();
                _process?.Dispose();
                _httpClient?.Dispose();
            }

            _disposed = true;
        }

        /// <summary>
        /// Asynchronously disposes managed resources.
        /// </summary>
        protected virtual async ValueTask DisposeAsyncCore()
        {
            if (_disposed)
                return;

            await StopAsync().ConfigureAwait(false);
            _process?.Dispose();
            _httpClient?.Dispose();

            _disposed = true;
        }

        #endregion

    }

}
