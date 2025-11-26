using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace CloudNimble.Breakdance.Azurite
{

    /// <summary>
    /// Manages a Windows Job Object that automatically terminates child processes when the parent process exits.
    /// This ensures that spawned Azurite processes are cleaned up even if the test process crashes,
    /// is killed, or terminated via Ctrl+C or debugger detach.
    /// </summary>
    /// <remarks>
    /// Windows Job Objects with the JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE flag provide bulletproof
    /// process cleanup that doesn't rely on finalizers, dispose patterns, or graceful shutdown.
    /// When the last handle to the job object is closed (which happens automatically when the
    /// .NET process exits), Windows terminates all processes assigned to the job.
    /// </remarks>
    internal static class WindowsJobObject
    {

        #region Private Members

#if NET9_0_OR_GREATER
        private static readonly Lock _lock = new();
#else
        private static readonly object _lock = new object();
#endif
        private static IntPtr _jobHandle = IntPtr.Zero;
        private static bool _initialized = false;

        #endregion

        #region P/Invoke Declarations

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern IntPtr CreateJobObjectW(IntPtr lpJobAttributes, string lpName);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetInformationJobObject(
            IntPtr hJob,
            JobObjectInfoType infoType,
            ref JOBOBJECT_EXTENDED_LIMIT_INFORMATION lpJobObjectInfo,
            int cbJobObjectInfoLength);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool AssignProcessToJobObject(IntPtr hJob, IntPtr hProcess);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CloseHandle(IntPtr hObject);

        #endregion

        #region Structures and Enums

        private enum JobObjectInfoType
        {
            ExtendedLimitInformation = 9
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct JOBOBJECT_BASIC_LIMIT_INFORMATION
        {
            public long PerProcessUserTimeLimit;
            public long PerJobUserTimeLimit;
            public uint LimitFlags;
            public UIntPtr MinimumWorkingSetSize;
            public UIntPtr MaximumWorkingSetSize;
            public uint ActiveProcessLimit;
            public UIntPtr Affinity;
            public uint PriorityClass;
            public uint SchedulingClass;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct IO_COUNTERS
        {
            public ulong ReadOperationCount;
            public ulong WriteOperationCount;
            public ulong OtherOperationCount;
            public ulong ReadTransferCount;
            public ulong WriteTransferCount;
            public ulong OtherTransferCount;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct JOBOBJECT_EXTENDED_LIMIT_INFORMATION
        {
            public JOBOBJECT_BASIC_LIMIT_INFORMATION BasicLimitInformation;
            public IO_COUNTERS IoInfo;
            public UIntPtr ProcessMemoryLimit;
            public UIntPtr JobMemoryLimit;
            public UIntPtr PeakProcessMemoryUsed;
            public UIntPtr PeakJobMemoryUsed;
        }

        // Job object limit flags
        private const uint JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE = 0x00002000;

        #endregion

        #region Public Methods

        /// <summary>
        /// Gets whether Windows Job Objects are supported on the current platform.
        /// </summary>
        public static bool IsSupported => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        /// <summary>
        /// Assigns a process to the shared job object.
        /// The process will be automatically terminated when the parent .NET process exits.
        /// </summary>
        /// <param name="process">The process to assign to the job object.</param>
        /// <returns>True if the process was successfully assigned, false otherwise.</returns>
        public static bool AssignProcess(Process process)
        {
            if (!IsSupported)
                return false;

            if (process == null || process.HasExited)
                return false;

            EnsureInitialized();

            if (_jobHandle == IntPtr.Zero)
                return false;

            try
            {
                return AssignProcessToJobObject(_jobHandle, process.Handle);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[WindowsJobObject] Failed to assign process: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Ensures the job object is initialized.
        /// </summary>
        private static void EnsureInitialized()
        {
            if (_initialized)
                return;

            lock (_lock)
            {
                if (_initialized)
                    return;

                try
                {
                    // Create an anonymous job object (null name)
                    _jobHandle = CreateJobObjectW(IntPtr.Zero, null);

                    if (_jobHandle == IntPtr.Zero)
                    {
                        Debug.WriteLine($"[WindowsJobObject] CreateJobObject failed: {Marshal.GetLastWin32Error()}");
                        _initialized = true;
                        return;
                    }

                    // Configure the job object to kill all processes when the job is closed
                    var info = new JOBOBJECT_EXTENDED_LIMIT_INFORMATION
                    {
                        BasicLimitInformation = new JOBOBJECT_BASIC_LIMIT_INFORMATION
                        {
                            LimitFlags = JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE
                        }
                    };

                    if (!SetInformationJobObject(
                        _jobHandle,
                        JobObjectInfoType.ExtendedLimitInformation,
                        ref info,
                        Marshal.SizeOf(typeof(JOBOBJECT_EXTENDED_LIMIT_INFORMATION))))
                    {
                        Debug.WriteLine($"[WindowsJobObject] SetInformationJobObject failed: {Marshal.GetLastWin32Error()}");
                        CloseHandle(_jobHandle);
                        _jobHandle = IntPtr.Zero;
                    }
                    else
                    {
                        Debug.WriteLine("[WindowsJobObject] Job object created successfully with KILL_ON_JOB_CLOSE flag");
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[WindowsJobObject] Initialization failed: {ex.Message}");
                    _jobHandle = IntPtr.Zero;
                }
                finally
                {
                    _initialized = true;
                }
            }
        }

        #endregion

    }

}
