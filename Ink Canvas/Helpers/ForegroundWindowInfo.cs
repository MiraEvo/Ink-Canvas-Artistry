using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Text;

namespace Ink_Canvas.Helpers
{
    [SuppressMessage("Reliability", "cs/call-to-unmanaged-code", Justification = "CodeQL-AUDITED-INTEROP: required Win32/COM boundary; no managed alternative; owned by ForegroundWindowInfo.")]
    [SuppressMessage("Reliability", "cs/unmanaged-code", Justification = "CodeQL-AUDITED-INTEROP: required Win32/COM boundary; no managed alternative; owned by ForegroundWindowInfo.")]
    internal static partial class ForegroundWindowInfo
    {
        [LibraryImport("user32.dll", EntryPoint = "GetForegroundWindow")]
        private static partial IntPtr GetForegroundWindow();

        // Source-generated P/Invoke does not support StringBuilder marshalling for this Win32 signature.
        [DllImport("user32.dll", EntryPoint = "GetWindowTextW", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll", EntryPoint = "GetClassNameW", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        [LibraryImport("user32.dll", EntryPoint = "GetWindowRect")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [LibraryImport("user32.dll", EntryPoint = "GetWindowThreadProcessId")]
        private static partial uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [LibraryImport("user32.dll", EntryPoint = "EnumWindows")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool EnumWindows(EnumWindowsProc callback, IntPtr lParam);

        [LibraryImport("user32.dll", EntryPoint = "IsWindowVisible")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool IsWindowVisibleNative(IntPtr hWnd);

        private const int TextCapacity = 256;

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;

            public int Width => Right - Left;
            public int Height => Bottom - Top;
        }

        public static string WindowTitle()
        {
            return ReadWindowTitle(GetForegroundWindowHandle());
        }

        public static string WindowClassName()
        {
            IntPtr foregroundWindowHandle = GetForegroundWindow();
            if (foregroundWindowHandle == IntPtr.Zero)
            {
                return string.Empty;
            }

            StringBuilder className = new(TextCapacity);
            if (GetClassName(foregroundWindowHandle, className, TextCapacity) <= 0)
            {
                return string.Empty;
            }

            return className.ToString();
        }

        public static RECT WindowRect()
        {
            IntPtr foregroundWindowHandle = GetForegroundWindowHandle();
            if (foregroundWindowHandle == IntPtr.Zero || !GetWindowRect(foregroundWindowHandle, out RECT windowRect))
            {
                return default;
            }

            return windowRect;
        }

        public static string ProcessName()
        {
            uint processId = GetWindowProcessId(GetForegroundWindowHandle());
            if (processId == 0 || processId > int.MaxValue)
            {
                return "Unknown";
            }

            try
            {
                using Process process = Process.GetProcessById((int)processId);
                return process.ProcessName;
            }
            catch (ArgumentException)
            {
                // Process with the given ID not found
                return "Unknown";
            }
            catch (InvalidOperationException)
            {
                return "Unknown";
            }
        }

        internal static IntPtr GetForegroundWindowHandle() => GetForegroundWindow();

        internal static uint GetWindowProcessId(IntPtr windowHandle)
        {
            if (windowHandle == IntPtr.Zero)
            {
                return 0;
            }

            GetWindowThreadProcessId(windowHandle, out uint processId);
            return processId;
        }

        internal static string ReadWindowTitle(IntPtr windowHandle)
        {
            if (windowHandle == IntPtr.Zero)
            {
                return string.Empty;
            }

            StringBuilder windowTitle = new(TextCapacity);
            if (GetWindowText(windowHandle, windowTitle, TextCapacity) <= 0)
            {
                return string.Empty;
            }

            return windowTitle.ToString();
        }

        internal static bool IsWindowVisible(IntPtr windowHandle) =>
            windowHandle != IntPtr.Zero && IsWindowVisibleNative(windowHandle);

        internal static void EnumerateTopLevelWindows(Func<IntPtr, bool> callback)
        {
            ArgumentNullException.ThrowIfNull(callback);

            EnumWindows(
                (windowHandle, _) => callback(windowHandle),
                IntPtr.Zero);
        }
    }
}
