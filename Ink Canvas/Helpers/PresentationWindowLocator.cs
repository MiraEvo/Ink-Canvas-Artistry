using Ink_Canvas.ViewModels;
using Microsoft.Office.Interop.PowerPoint;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace Ink_Canvas.Helpers
{
    internal static class PresentationWindowLocator
    {
        private const int TextCapacity = 512;

        internal static PresentationProvider DetectProvider(string? presentationIdentity, string? applicationName)
        {
            IntPtr matchedWindowHandle = TryFindPresentationWindowHandle(presentationIdentity, applicationName, out string matchedProcessName);
            if (matchedWindowHandle != IntPtr.Zero
                && (matchedProcessName.StartsWith("wpp", StringComparison.OrdinalIgnoreCase)
                    || matchedProcessName.StartsWith("wps", StringComparison.OrdinalIgnoreCase)))
            {
                return PresentationProvider.Wps;
            }

            if (!string.IsNullOrWhiteSpace(applicationName)
                && applicationName.IndexOf("WPS", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return PresentationProvider.Wps;
            }

            return PresentationProvider.PowerPoint;
        }

        internal static bool IsPresentationForeground(
            object? slideShowWindowObject,
            string? presentationIdentity,
            string? applicationName,
            PresentationProvider provider)
        {
            IntPtr foregroundWindowHandle = GetForegroundWindow();
            if (foregroundWindowHandle == IntPtr.Zero)
            {
                return false;
            }

            GetWindowThreadProcessId(foregroundWindowHandle, out uint foregroundProcessId);
            string foregroundProcessName = TryGetProcessName(foregroundProcessId);

            IntPtr targetWindowHandle = TryGetSlideShowWindowHandle(slideShowWindowObject);
            string targetProcessName = string.Empty;
            if (targetWindowHandle == IntPtr.Zero)
            {
                targetWindowHandle = TryFindPresentationWindowHandle(presentationIdentity, applicationName, out targetProcessName);
            }

            if (targetWindowHandle == IntPtr.Zero)
            {
                return provider is PresentationProvider.Wps
                    && foregroundProcessName.StartsWith("wps", StringComparison.OrdinalIgnoreCase);
            }

            GetWindowThreadProcessId(targetWindowHandle, out uint targetProcessId);
            if (string.IsNullOrWhiteSpace(targetProcessName))
            {
                targetProcessName = TryGetProcessName(targetProcessId);
            }

            if (foregroundProcessId != 0 && foregroundProcessId == targetProcessId)
            {
                return true;
            }

            return provider is PresentationProvider.Wps
                && foregroundProcessName.StartsWith("wps", StringComparison.OrdinalIgnoreCase)
                && targetProcessName.StartsWith("wpp", StringComparison.OrdinalIgnoreCase);
        }

        private static IntPtr TryFindPresentationWindowHandle(
            string? presentationIdentity,
            string? applicationName,
            out string processName)
        {
            processName = string.Empty;
            string presentationFileName = Path.GetFileName(presentationIdentity ?? string.Empty);
            if (string.IsNullOrWhiteSpace(presentationFileName))
            {
                presentationFileName = presentationIdentity ?? string.Empty;
            }

            if (string.IsNullOrWhiteSpace(presentationFileName))
            {
                return IntPtr.Zero;
            }

            HashSet<string> titleKeywords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "PowerPoint",
                "WPS"
            };

            if (!string.IsNullOrWhiteSpace(applicationName))
            {
                titleKeywords.Add(applicationName);
            }

            List<IntPtr> candidateWindowHandles = new List<IntPtr>();
            EnumWindows((windowHandle, _) =>
            {
                try
                {
                    if (!IsWindowVisible(windowHandle))
                    {
                        return true;
                    }

                    int textLength = GetWindowTextLength(windowHandle);
                    if (textLength == 0)
                    {
                        return true;
                    }

                    StringBuilder title = new StringBuilder(textLength + 1);
                    if (GetWindowText(windowHandle, title, title.Capacity) <= 0)
                    {
                        return true;
                    }

                    string windowTitle = title.ToString();
                    if (windowTitle.IndexOf(presentationFileName, StringComparison.OrdinalIgnoreCase) < 0)
                    {
                        return true;
                    }

                    foreach (string keyword in titleKeywords)
                    {
                        if (windowTitle.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            candidateWindowHandles.Add(windowHandle);
                            break;
                        }
                    }
                }
                catch
                {
                }

                return true;
            }, IntPtr.Zero);

            if (candidateWindowHandles.Count != 1)
            {
                return IntPtr.Zero;
            }

            GetWindowThreadProcessId(candidateWindowHandles[0], out uint processId);
            processName = TryGetProcessName(processId);
            return candidateWindowHandles[0];
        }

        private static IntPtr TryGetSlideShowWindowHandle(object? slideShowWindowObject)
        {
            if (slideShowWindowObject is not SlideShowWindow slideShowWindow)
            {
                return IntPtr.Zero;
            }

            try
            {
                return new IntPtr(slideShowWindow.HWND);
            }
            catch
            {
                return IntPtr.Zero;
            }
        }

        private static string TryGetProcessName(uint processId)
        {
            if (processId == 0)
            {
                return string.Empty;
            }

            try
            {
                using Process process = Process.GetProcessById((int)processId);
                return process.ProcessName;
            }
            catch
            {
                return string.Empty;
            }
        }

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int maxCount);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetWindowTextLength(IntPtr hWnd);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool EnumWindows(EnumWindowsProc callback, IntPtr lParam);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsWindowVisible(IntPtr hWnd);
    }
}
