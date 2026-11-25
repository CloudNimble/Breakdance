using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace CloudNimble.Breakdance.Azurite
{

    /// <summary>
    /// Describes where the Azurite npm package is installed and which script each of its commands runs.
    /// </summary>
    /// <remarks>
    /// The installation is located once per process and shared by every <see cref="AzuriteInstance"/>, because
    /// finding it means running npm, which is far too slow to repeat for each instance a test run starts.
    /// </remarks>
    internal sealed class AzuriteInstallation
    {

        #region Fields

        private const string PackageName = "azurite";

        private static readonly Lock _locateLock = new();
        private static Task<AzuriteInstallation> _located;

        private readonly Dictionary<string, string> _commandScripts;
        private readonly string _localDirectory;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the directory the Azurite process should run in.
        /// </summary>
        /// <remarks>
        /// Azurite stores its data in the working directory when disk persistence is enabled without an explicit location.
        /// A global installation runs in the current directory, read each time so a change of directory is honored.
        /// A local installation runs in the directory that contains its <c>node_modules</c> folder.
        /// </remarks>
        public string WorkingDirectory => _localDirectory ?? Environment.CurrentDirectory;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="AzuriteInstallation"/> class.
        /// </summary>
        /// <param name="localDirectory">The directory containing the local <c>node_modules</c> folder, or null for a global installation.</param>
        /// <param name="commandScripts">The full path of the script behind each Azurite command, keyed by command name.</param>
        private AzuriteInstallation(string localDirectory, Dictionary<string, string> commandScripts)
        {
            _localDirectory = localDirectory;
            _commandScripts = commandScripts;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Gets the full path of the script that an Azurite command runs.
        /// </summary>
        /// <param name="command">The Azurite command name, such as <c>azurite</c> or <c>azurite-table</c>.</param>
        /// <returns>The full path of the script to pass to Node.js.</returns>
        /// <exception cref="InvalidOperationException">The installed Azurite package does not provide <paramref name="command"/>.</exception>
        public string GetScriptPath(string command)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(command);

            if (!_commandScripts.TryGetValue(command, out var scriptPath))
            {
                throw new InvalidOperationException(
                    $"The installed Azurite package does not provide the '{command}' command. " +
                    $"Upgrade with: npm install -g azurite@latest");
            }

            return scriptPath;
        }

        /// <summary>
        /// Locates the Azurite installation, reusing the result of an earlier successful search.
        /// </summary>
        /// <returns>The installation that Azurite processes should be started from.</returns>
        /// <exception cref="DirectoryNotFoundException">Azurite is installed neither globally nor next to this assembly.</exception>
        /// <exception cref="InvalidOperationException">The installation could not be inspected.</exception>
        /// <remarks>
        /// A globally installed Azurite takes precedence over a <c>node_modules</c> folder next to this assembly.
        /// A failed search is not cached, so installing Azurite and starting another instance works without restarting the process.
        /// </remarks>
        public static Task<AzuriteInstallation> LocateAsync()
        {
            lock (_locateLock)
            {
                if (_located is null || _located.IsFaulted || _located.IsCanceled)
                {
                    _located = LocateCoreAsync();
                }

                return _located;
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Finds the global npm <c>node_modules</c> folder.
        /// </summary>
        /// <returns>The folder path, or null when npm is unavailable or reports no usable folder.</returns>
        private static async Task<string> GetGlobalModulesDirectoryAsync()
        {
            // npm is a batch file on Windows, which only the command interpreter can start.
            var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            var startInfo = new ProcessStartInfo
            {
                FileName = isWindows ? "cmd.exe" : "npm",
                Arguments = isWindows ? "/d /c npm root -g" : "root -g",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            try
            {
                using var npm = Process.Start(startInfo);
                if (npm is null)
                {
                    return null;
                }

                // Both streams are read to the end so npm can never block on a full pipe.
                var outputTask = npm.StandardOutput.ReadToEndAsync();
                var errorTask = npm.StandardError.ReadToEndAsync();
                await Task.WhenAll(outputTask, errorTask).ConfigureAwait(false);
                await npm.WaitForExitAsync().ConfigureAwait(false);

                var modulesDirectory = (await outputTask.ConfigureAwait(false)).Trim();
                return npm.ExitCode == 0 && !string.IsNullOrWhiteSpace(modulesDirectory) ? modulesDirectory : null;
            }
            catch (Win32Exception)
            {
                // npm is not installed or not on PATH.
                return null;
            }
        }

        /// <summary>
        /// Searches for the Azurite package and reads its command table.
        /// </summary>
        /// <returns>The located installation.</returns>
        private static async Task<AzuriteInstallation> LocateCoreAsync()
        {
            try
            {
                var globalModulesDirectory = await GetGlobalModulesDirectoryAsync().ConfigureAwait(false);
                if (globalModulesDirectory is not null)
                {
                    var globalPackageDirectory = Path.Combine(globalModulesDirectory, PackageName);
                    if (Directory.Exists(globalPackageDirectory))
                    {
                        return new AzuriteInstallation(localDirectory: null, ReadCommandScripts(globalPackageDirectory));
                    }
                }

                var assemblyDirectory = Path.GetDirectoryName(typeof(AzuriteInstallation).Assembly.Location);
                var localPackageDirectory = Path.Combine(assemblyDirectory, "node_modules", PackageName);
                if (Directory.Exists(localPackageDirectory))
                {
                    return new AzuriteInstallation(assemblyDirectory, ReadCommandScripts(localPackageDirectory));
                }

                throw new DirectoryNotFoundException(
                    $"Azurite not found. Neither a global install nor '{localPackageDirectory}' exists.\n" +
                    $"Please install Azurite globally: npm install -g azurite\n" +
                    $"Or ensure the Breakdance.Azurite NuGet package build target ran successfully.");
            }
            catch (Exception ex) when (ex is not (DirectoryNotFoundException or InvalidOperationException))
            {
                throw new InvalidOperationException($"Failed to determine Azurite directory: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Reads the <c>bin</c> table of the Azurite package, which maps each command name to the script it runs.
        /// </summary>
        /// <param name="packageDirectory">The directory containing Azurite's <c>package.json</c>.</param>
        /// <returns>The full script path for each command.</returns>
        private static Dictionary<string, string> ReadCommandScripts(string packageDirectory)
        {
            var manifestPath = Path.Combine(packageDirectory, "package.json");
            if (!File.Exists(manifestPath))
            {
                throw new InvalidOperationException($"The Azurite package at '{packageDirectory}' has no package.json. Reinstall with: npm install -g azurite");
            }

            using var manifest = JsonDocument.Parse(File.ReadAllText(manifestPath));
            if (!manifest.RootElement.TryGetProperty("bin", out var bin) || bin.ValueKind != JsonValueKind.Object)
            {
                throw new InvalidOperationException($"'{manifestPath}' does not declare any commands. Reinstall with: npm install -g azurite");
            }

            var commandScripts = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var command in bin.EnumerateObject())
            {
                var relativePath = command.Value.GetString();
                if (string.IsNullOrWhiteSpace(relativePath))
                {
                    continue;
                }

                var scriptPath = Path.GetFullPath(Path.Combine(packageDirectory, relativePath));
                if (!File.Exists(scriptPath))
                {
                    throw new InvalidOperationException($"Azurite command '{command.Name}' points to '{scriptPath}', which does not exist. Reinstall with: npm install -g azurite");
                }

                commandScripts[command.Name] = scriptPath;
            }

            return commandScripts;
        }

        #endregion

    }

}
