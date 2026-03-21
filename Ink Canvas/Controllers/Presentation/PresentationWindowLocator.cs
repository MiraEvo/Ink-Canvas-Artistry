using Ink_Canvas.ViewModels;
using Microsoft.Office.Interop.PowerPoint;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices;
using Ink_Canvas.Helpers;

namespace Ink_Canvas.Controllers.Presentation
{
    [SuppressMessage("Reliability", "cs/call-to-unmanaged-code", Justification = "CodeQL-AUDITED-INTEROP: required Win32/COM boundary; no managed alternative; owned by PresentationWindowLocator.")]
    internal static partial class PresentationWindowLocator
    {
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
            IntPtr foregroundWindowHandle = ForegroundWindowInfo.GetForegroundWindowHandle();
            if (foregroundWindowHandle == IntPtr.Zero)
            {
                return false;
            }

            uint foregroundProcessId = ForegroundWindowInfo.GetWindowProcessId(foregroundWindowHandle);
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

            uint targetProcessId = ForegroundWindowInfo.GetWindowProcessId(targetWindowHandle);
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
            ForegroundWindowInfo.EnumerateTopLevelWindows(windowHandle =>
                TryCollectPresentationWindowCandidate(
                    windowHandle,
                    presentationFileName,
                    titleKeywords,
                    candidateWindowHandles));

            if (candidateWindowHandles.Count != 1)
            {
                return IntPtr.Zero;
            }

            uint processId = ForegroundWindowInfo.GetWindowProcessId(candidateWindowHandles[0]);
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
            if (!ForegroundWindowInfo.IsWindowVisible(windowHandle))
            {
                return true;
            }

            string windowTitle = ForegroundWindowInfo.ReadWindowTitle(windowHandle);
            if (string.IsNullOrWhiteSpace(windowTitle))
            {
                return true;
            }

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
    }
}

