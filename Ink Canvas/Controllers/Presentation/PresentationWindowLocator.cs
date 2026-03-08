using Ink_Canvas.ViewModels;
using Microsoft.Office.Interop.PowerPoint;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace Ink_Canvas.Controllers.Presentation
{
    [SuppressMessage("Reliability", "cs/call-to-unmanaged-code", Justification = "受限 Win32/COM 边界，无托管替代，调用已集中封装并受保护。")]
    internal static partial class PresentationWindowLocator
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

            List<IntPtr> candidateWindowHandles = [];
            EnumWindows(
                (windowHandle, _) => TryCollectPresentationWindowCandidate(
                    windowHandle,
                    presentationFileName,
                    titleKeywords,
                    candidateWindowHandles),
                IntPtr.Zero);

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
            catch (COMException)
            {
                return IntPtr.Zero;
            }
            catch (InvalidOperationException)
            {
                return IntPtr.Zero;
            }
            catch (ArgumentException)
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

            if (processId > int.MaxValue)
            {
                return string.Empty;
            }

            try
            {
                using Process process = Process.GetProcessById((int)processId);
                return process.ProcessName;
            }
            catch (ArgumentException)
            {
                return string.Empty;
            }
            catch (InvalidOperationException)
            {
                return string.Empty;
            }
        }

        private static bool TryCollectPresentationWindowCandidate(
            IntPtr windowHandle,
            string presentationFileName,
            HashSet<string> titleKeywords,
            ICollection<IntPtr> candidateWindowHandles)
        {
            if (!IsWindowVisible(windowHandle))
            {
                return true;
            }

            int textLength = GetWindowTextLength(windowHandle);
            if (textLength <= 0)
            {
                return true;
            }

            StringBuilder title = new(textLength + 1);
            if (GetWindowText(windowHandle, title, title.Capacity) <= 0)
            {
                return true;
            }

            string windowTitle = title.ToString();
            if (windowTitle.IndexOf(presentationFileName, StringComparison.OrdinalIgnoreCase) < 0)
            {
                return true;
            }

            if (titleKeywords.Any(keyword =>
                windowTitle.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0))
            {
                candidateWindowHandles.Add(windowHandle);
            }

            return true;
        }

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [LibraryImport("user32.dll", EntryPoint = "GetForegroundWindow")]
        private static partial IntPtr GetForegroundWindow();

        [LibraryImport("user32.dll", EntryPoint = "GetWindowThreadProcessId")]
        private static partial uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

        // Source-generated P/Invoke does not support StringBuilder marshalling for this Win32 signature.
        [DllImport("user32.dll", EntryPoint = "GetWindowTextW", CharSet = CharSet.Unicode)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int maxCount);

        [DllImport("user32.dll", EntryPoint = "GetWindowTextLengthW", CharSet = CharSet.Unicode)]
        private static extern int GetWindowTextLength(IntPtr hWnd);

        [LibraryImport("user32.dll", EntryPoint = "EnumWindows")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool EnumWindows(EnumWindowsProc callback, IntPtr lParam);

        [LibraryImport("user32.dll", EntryPoint = "IsWindowVisible")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool IsWindowVisible(IntPtr hWnd);
    }
}

