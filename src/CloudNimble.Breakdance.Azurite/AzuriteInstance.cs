using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

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
        private readonly StringBuilder _outputBuffer = new StringBuilder();
        private readonly StringBuilder _errorBuffer = new StringBuilder();
        private readonly TaskCompletionSource<bool> _portsDetected = new TaskCompletionSource<bool>();
        private int _successfulServicesCount = 0;

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
        /// </summary>
        /// <returns>A task that completes when Azurite is ready to accept connections.</returns>
        public async Task StartAsync()
        {
            // RWM: If it's already running, no need to start it.
            if (_isRunning) return;

            // Build command arguments
            var args = BuildArguments();

            // Determine which Azurite command to use based on services requested
            var command = GetAzuriteCommand();

            // Get working directory
            var workingDirectory = GetAzuriteDirectory();
            var fullCommand = $"npx {command} {args}";

            // Use cmd.exe /k (instead of /c) to keep cmd.exe alive.
            // This maintains the process tree so Kill(entireProcessTree: true) works correctly.
            // With /c, cmd.exe exits immediately after spawning npx, orphaning the Node processes.
            _process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/k {fullCommand}",
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
            _outputBuffer.AppendLine($"[DEBUG] Starting process: cmd.exe /k {fullCommand}");
            _outputBuffer.AppendLine($"[DEBUG] Working directory: {workingDirectory}");

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
            _process.BeginOutputReadLine();
            _process.BeginErrorReadLine();

            try
            {
                // Wait for services to be ready
                await WaitForReadyAsync();

                // Only mark as running if startup was successful
                _isRunning = true;
            }
            catch
            {
                // If startup fails, clean up the process
                try
                {
                    if (!_process.HasExited)
                    {
                        _process.Kill(entireProcessTree: true);
                    }
                }
                catch
                {
                    // Ignore cleanup errors
                }

                // Re-throw the original exception for retry logic
                throw;
            }
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
                AzuriteServiceType.Table => "azurite-table",
                _ => "azurite" // Default to all services
            };
        }

        /// <summary>
        /// Parses port information from Azurite's standard output.
        /// Azurite outputs: "Azurite [Service] service is successfully listening at http://127.0.0.1:[port]"
        /// </summary>
        /// <param name="output">A line of output from Azurite.</param>
        private void ParsePortFromOutput(string output)
        {
            try
            {
                // Check for "success" to detect service ready - handles various output formats
                // e.g., "successfully listens on", "successfully listening at", etc.
                if (output.Contains("success", StringComparison.OrdinalIgnoreCase))
                {
                    _successfulServicesCount++;

                    // Parse the port from this line - support multiple URL formats
                    var match = System.Text.RegularExpressions.Regex.Match(output, @"https?://[^:]+:(\d+)");
                    if (!match.Success)
                    {
                        // Try alternative pattern for different output formats
                        match = System.Text.RegularExpressions.Regex.Match(output, @"port\s*(\d+)");
                    }
                    
                    if (match.Success && int.TryParse(match.Groups[1].Value, out var port))
                    {
                        if (output.Contains("Blob") || output.Contains("blob"))
                            BlobPort = port;
                        else if (output.Contains("Queue") || output.Contains("queue"))
                            QueuePort = port;
                        else if (output.Contains("Table") || output.Contains("table"))
                            TablePort = port;
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
        /// Checks for fatal errors in the output and error buffers and throws if found.
        /// Port conflict errors (EADDRINUSE) are thrown separately so they can be retried at a higher level.
        /// </summary>
        private void CheckForFatalErrors()
        {
            var errorOutput = StandardError;
            var standardOutput = StandardOutput;
            var combinedOutput = errorOutput + standardOutput;

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
        /// Checks if all requested services are ready.
        /// </summary>
        /// <param name="httpClient">The HTTP client to use for health checks.</param>
        /// <returns>True if all services are ready, false otherwise.</returns>
        private async Task<bool> AreServicesReadyAsync(HttpClient httpClient)
        {
            try
            {
                var tasks = new List<Task<bool>>();

                if (BlobPort.HasValue)
                    tasks.Add(IsServiceReadyAsync(httpClient, BlobEndpoint));

                if (QueuePort.HasValue)
                    tasks.Add(IsServiceReadyAsync(httpClient, QueueEndpoint));

                if (TablePort.HasValue)
                    tasks.Add(IsServiceReadyAsync(httpClient, TableEndpoint));

                var results = await Task.WhenAll(tasks);
                return results.All(r => r);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Checks if a specific service endpoint is ready.
        /// </summary>
        /// <param name="httpClient">The HTTP client.</param>
        /// <param name="endpoint">The endpoint URL.</param>
        /// <returns>True if the service is ready, false otherwise.</returns>
        private async Task<bool> IsServiceReadyAsync(HttpClient httpClient, string endpoint)
        {
            try
            {
                var response = await httpClient.GetAsync($"{endpoint}/devstoreaccount1?comp=list");
                return response.IsSuccessStatusCode || (int)response.StatusCode == 400; // 400 is ok, means service is up
            }
            catch
            {
                return false;
            }
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

            _disposed = true;
        }

        #endregion

    }

}
