using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Text;

namespace Ink_Canvas.Helpers
{
    internal static partial class ForegroundWindowInfo
    {
        [LibraryImport("user32.dll", EntryPoint = "GetForegroundWindow")]
        private static partial IntPtr GetForegroundWindow();

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        [LibraryImport("user32.dll", EntryPoint = "GetWindowRect")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [LibraryImport("user32.dll", EntryPoint = "GetWindowThreadProcessId")]
        private static partial uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        private const int TextCapacity = 256;

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
            IntPtr foregroundWindowHandle = GetForegroundWindow();
            if (foregroundWindowHandle == IntPtr.Zero)
            {
                return string.Empty;
            }

            StringBuilder windowTitle = new StringBuilder(TextCapacity);
            if (GetWindowText(foregroundWindowHandle, windowTitle, TextCapacity) <= 0)
            {
                return string.Empty;
            }

            return windowTitle.ToString();
        }

        public static string WindowClassName()
        {
            IntPtr foregroundWindowHandle = GetForegroundWindow();
            if (foregroundWindowHandle == IntPtr.Zero)
            {
                return string.Empty;
            }

            StringBuilder className = new StringBuilder(TextCapacity);
            if (GetClassName(foregroundWindowHandle, className, TextCapacity) <= 0)
            {
                return string.Empty;
            }

            return className.ToString();
        }

        public static RECT WindowRect()
        {
            IntPtr foregroundWindowHandle = GetForegroundWindow();
            if (foregroundWindowHandle == IntPtr.Zero || !GetWindowRect(foregroundWindowHandle, out RECT windowRect))
            {
                return default;
            }

            return windowRect;
        }

        public static string ProcessName()
        {
            IntPtr foregroundWindowHandle = GetForegroundWindow();
            if (foregroundWindowHandle == IntPtr.Zero)
            {
                return "Unknown";
            }

            GetWindowThreadProcessId(foregroundWindowHandle, out uint processId);
            if (processId == 0)
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
    }
}
