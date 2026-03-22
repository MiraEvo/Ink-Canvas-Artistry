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

        [LibraryImport("user32.dll", EntryPoint = "GetWindowTextW", SetLastError = true)]
        private static partial int GetWindowText(IntPtr hWnd, IntPtr lpString, int nMaxCount);

        [LibraryImport("user32.dll", EntryPoint = "GetClassNameW", SetLastError = true)]
        private static partial int GetClassName(IntPtr hWnd, IntPtr lpClassName, int nMaxCount);

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

        [SuppressMessage("Reliability", "cs/call-to-unmanaged-code", Justification = "CodeQL-AUDITED-INTEROP: Win32 class-name lookup has no managed equivalent.")]
        public static string WindowClassName()
        {
            IntPtr foregroundWindowHandle = GetForegroundWindow();
            if (foregroundWindowHandle == IntPtr.Zero)
            {
                return string.Empty;
            }

            IntPtr classNameBuffer = Marshal.AllocHGlobal(TextCapacity * sizeof(char));
            try
            {
                int classNameLength = GetClassName(foregroundWindowHandle, classNameBuffer, TextCapacity);
                if (classNameLength <= 0)
                {
                    return string.Empty;
                }

                return Marshal.PtrToStringUni(classNameBuffer, classNameLength) ?? string.Empty;
            }
            finally
            {
                Marshal.FreeHGlobal(classNameBuffer);
            }
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

        [SuppressMessage("Reliability", "cs/call-to-unmanaged-code", Justification = "CodeQL-AUDITED-INTEROP: Win32 window-title lookup has no managed equivalent.")]
        internal static string ReadWindowTitle(IntPtr windowHandle)
        {
            if (windowHandle == IntPtr.Zero)
            {
                return string.Empty;
            }

            IntPtr windowTitleBuffer = Marshal.AllocHGlobal(TextCapacity * sizeof(char));
            try
            {
                int windowTitleLength = GetWindowText(windowHandle, windowTitleBuffer, TextCapacity);
                if (windowTitleLength <= 0)
                {
                    return string.Empty;
                }

                return Marshal.PtrToStringUni(windowTitleBuffer, windowTitleLength) ?? string.Empty;
            }
            finally
            {
                Marshal.FreeHGlobal(windowTitleBuffer);
            }
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
