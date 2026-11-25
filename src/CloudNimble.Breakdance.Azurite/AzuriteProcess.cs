using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CloudNimble.Breakdance.Azurite
{

    /// <summary>
    /// Owns one Azurite Node.js process: starts it, captures everything it writes, and stops it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Azurite runs as a single <c>node</c> process with no command interpreter or <c>npx</c> in between, so stopping
    /// it is one reliable kill instead of a walk over a process tree that can miss the process doing the work.
    /// </para>
    /// <para>
    /// Captured output belongs to this process alone. Each start attempt gets its own <see cref="AzuriteProcess"/>,
    /// so a late line from an attempt that was killed can never be mistaken for output of the retry.
    /// </para>
    /// </remarks>
    internal sealed class AzuriteProcess : IDisposable
    {

        #region Fields

        private static readonly TimeSpan _drainTimeout = TimeSpan.FromSeconds(2);
        private static readonly TimeSpan _exitTimeout = TimeSpan.FromSeconds(5);

        private readonly StringBuilder _errorBuffer = new();
        private readonly TaskCompletionSource<bool> _errorClosed = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly Action<string> _onOutputLine;
        private readonly StringBuilder _outputBuffer = new();
        private readonly TaskCompletionSource<bool> _outputClosed = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly Process _process;
        private readonly Lock _bufferLock = new();
        private bool _disposed;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the exit code of the process, or null while it is still running.
        /// </summary>
        public int? ExitCode => HasExited ? _process.ExitCode : null;

        /// <summary>
        /// Gets whether the process has exited.
        /// </summary>
        public bool HasExited
        {
            get
            {
                try
                {
                    return _process.HasExited;
                }
                catch (InvalidOperationException)
                {
                    // The process object has been disposed, which only happens after the process was stopped.
                    return true;
                }
            }
        }

        /// <summary>
        /// Gets everything the process has written to standard error so far.
        /// </summary>
        public string StandardError
        {
            get
            {
                lock (_bufferLock)
                {
                    return _errorBuffer.ToString();
                }
            }
        }

        /// <summary>
        /// Gets everything the process has written to standard output so far, preceded by any diagnostic notes.
        /// </summary>
        public string StandardOutput
        {
            get
            {
                lock (_bufferLock)
                {
                    return _outputBuffer.ToString();
                }
            }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="AzuriteProcess"/> class.
        /// </summary>
        /// <param name="process">The process to own. It has not been started yet.</param>
        /// <param name="onOutputLine">Called with each non-empty line the process writes to standard output.</param>
        private AzuriteProcess(Process process, Action<string> onOutputLine)
        {
            _process = process;
            _onOutputLine = onOutputLine;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Releases the process handle. The captured output stays readable.
        /// </summary>
        /// <remarks>
        /// Disposing does not stop the process. Call <see cref="Stop"/> or <see cref="StopAsync"/> first.
        /// </remarks>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _process.Dispose();
        }

        /// <summary>
        /// Starts a process and begins capturing its output.
        /// </summary>
        /// <param name="startInfo">Describes the process to start. Output redirection and window settings are set here.</param>
        /// <param name="onOutputLine">Called with each non-empty line the process writes to standard output.</param>
        /// <param name="notes">Diagnostic lines to place at the top of the captured standard output.</param>
        /// <returns>The running process.</returns>
        /// <exception cref="InvalidOperationException">The executable could not be started.</exception>
        public static AzuriteProcess Start(ProcessStartInfo startInfo, Action<string> onOutputLine, params string[] notes)
        {
            ArgumentNullException.ThrowIfNull(startInfo);
            ArgumentNullException.ThrowIfNull(onOutputLine);
            ArgumentNullException.ThrowIfNull(notes);

            startInfo.UseShellExecute = false;
            startInfo.CreateNoWindow = true;
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;
            startInfo.StandardOutputEncoding = Encoding.UTF8;
            startInfo.StandardErrorEncoding = Encoding.UTF8;
#if NET11_0_OR_GREATER
            // The operating system ends Azurite when this process ends, however that happens.
            // macOS has no equivalent, so Azurite there relies on the instance being stopped or disposed.
            if (OperatingSystem.IsWindows() || OperatingSystem.IsLinux() || OperatingSystem.IsAndroid())
            {
                startInfo.KillOnParentExit = true;
            }
#endif

            var azuriteProcess = new AzuriteProcess(new Process { StartInfo = startInfo }, onOutputLine);
            foreach (var note in notes)
            {
                azuriteProcess._outputBuffer.AppendLine(note);
            }

            try
            {
                azuriteProcess._process.Start();
            }
            catch (Win32Exception ex)
            {
                azuriteProcess._process.Dispose();
                throw new InvalidOperationException(
                    $"Failed to start '{startInfo.FileName}'. Ensure Node.js is installed and in PATH. {ex.Message}", ex);
            }

#if !NET11_0_OR_GREATER
            // Before .NET 11 there is no built-in way to tie a child's lifetime to its parent, so Windows gets a job object.
            WindowsJobObject.AssignProcess(azuriteProcess._process);
#endif
            azuriteProcess.BeginReadingOutput();

            return azuriteProcess;
        }

        /// <summary>
        /// Kills the process if it is still running, then blocks until it has exited and its output has been captured.
        /// </summary>
        public void Stop()
        {
            if (_disposed)
            {
                return;
            }

            Kill();

            try
            {
                _process.WaitForExit(_exitTimeout);
            }
            catch (Exception ex) when (ex is InvalidOperationException or Win32Exception)
            {
                Debug.WriteLine($"[AzuriteProcess] Waiting for exit failed: {ex.Message}");
            }

            Task.WhenAll(_outputClosed.Task, _errorClosed.Task).Wait(_drainTimeout);
        }

        /// <summary>
        /// Kills the process if it is still running, then waits until it has exited and its output has been captured.
        /// </summary>
        /// <returns>A task that completes when the process is gone.</returns>
        public async Task StopAsync()
        {
            if (_disposed)
            {
                return;
            }

            Kill();

            try
            {
                using var exitTimeout = new CancellationTokenSource(_exitTimeout);
                await _process.WaitForExitAsync(exitTimeout.Token).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is OperationCanceledException or InvalidOperationException or Win32Exception)
            {
                Debug.WriteLine($"[AzuriteProcess] Waiting for exit failed: {ex.Message}");
            }

            await WaitForOutputDrainAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Waits until everything the process wrote before exiting has been captured.
        /// </summary>
        /// <returns>A task that completes when both output streams have closed, or after a short timeout.</returns>
        /// <remarks>
        /// A process can be observed as exited before its last lines have been read. Anything that decides what went
        /// wrong from the captured output, such as recognizing a port conflict, has to wait for this first.
        /// </remarks>
        public async Task WaitForOutputDrainAsync()
        {
            try
            {
                await Task.WhenAll(_outputClosed.Task, _errorClosed.Task).WaitAsync(_drainTimeout).ConfigureAwait(false);
            }
            catch (TimeoutException)
            {
                Debug.WriteLine("[AzuriteProcess] Output streams did not close in time.");
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Starts capturing standard output and standard error without occupying thread pool threads.
        /// </summary>
        /// <remarks>
        /// Before .NET 11, reading a redirected stream asynchronously parks a thread pool thread in a blocking read for
        /// as long as the process lives. A parallel test run with a few dozen instances starves the pool that way, and
        /// the line announcing that Azurite is ready is then never delivered. Dedicated threads avoid that.
        /// .NET 11 reads redirected streams with real asynchronous I/O, so the built-in events are used there.
        /// </remarks>
        private void BeginReadingOutput()
        {
#if NET11_0_OR_GREATER
            _process.OutputDataReceived += (_, e) => HandleLine(e.Data, isError: false);
            _process.ErrorDataReceived += (_, e) => HandleLine(e.Data, isError: true);
            _process.BeginOutputReadLine();
            _process.BeginErrorReadLine();
#else
            StartReaderThread(_process.StandardOutput, isError: false);
            StartReaderThread(_process.StandardError, isError: true);
#endif
        }

        /// <summary>
        /// Records one line of output, or the end of a stream.
        /// </summary>
        /// <param name="line">The line that was read, or null when the stream has closed.</param>
        /// <param name="isError">True for standard error, false for standard output.</param>
        private void HandleLine(string line, bool isError)
        {
            if (line is null)
            {
                (isError ? _errorClosed : _outputClosed).TrySetResult(true);
                return;
            }

            if (line.Length == 0)
            {
                return;
            }

            lock (_bufferLock)
            {
                (isError ? _errorBuffer : _outputBuffer).AppendLine(line);
            }

            if (isError)
            {
                return;
            }

            try
            {
                _onOutputLine(line);
            }
            catch (Exception ex)
            {
                // A failing callback must not end the reader, or every later line would be lost.
                lock (_bufferLock)
                {
                    _errorBuffer.AppendLine($"Exception handling Azurite output: {ex.Message}");
                    _errorBuffer.AppendLine($"Output line: {line}");
                }
            }
        }

        /// <summary>
        /// Kills the process if it is still running.
        /// </summary>
        private void Kill()
        {
            try
            {
                if (!_process.HasExited)
                {
                    _process.Kill();
                }
            }
            catch (Exception ex) when (ex is InvalidOperationException or Win32Exception)
            {
                // The process exited on its own between the check and the kill.
                Debug.WriteLine($"[AzuriteProcess] Kill skipped: {ex.Message}");
            }
        }

#if !NET11_0_OR_GREATER
        /// <summary>
        /// Reads a redirected stream line by line on a dedicated background thread until it closes.
        /// </summary>
        /// <param name="reader">The redirected stream to read.</param>
        /// <param name="isError">True for standard error, false for standard output.</param>
        private void StartReaderThread(System.IO.StreamReader reader, bool isError)
        {
            var thread = new Thread(() =>
            {
                try
                {
                    string line;
                    while ((line = reader.ReadLine()) is not null)
                    {
                        HandleLine(line, isError);
                    }
                }
                catch (Exception ex) when (ex is System.IO.IOException or ObjectDisposedException)
                {
                    Debug.WriteLine($"[AzuriteProcess] Output reader ended: {ex.Message}");
                }
                finally
                {
                    HandleLine(null, isError);
                }
            })
            {
                IsBackground = true,
                Name = isError ? "Azurite stderr reader" : "Azurite stdout reader"
            };

            thread.Start();
        }
#endif

        #endregion

    }

}
