using Ink_Canvas.Controllers;
using Ink_Canvas.Helpers;
using Ink_Canvas.Services.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

namespace Ink_Canvas.Controllers.Presentation
{
    [SuppressMessage("Reliability", "cs/call-to-unmanaged-code", Justification = "受限 Win32/COM 边界，无托管替代，调用已集中封装并受保护。")]
    internal sealed partial class RotPresentationDiscovery
    {
        private const string PowerPointApplicationMoniker = "!{91493441-5A91-11CF-8700-00AA0060263B}";
        private static readonly string[] PresentationExtensions =
        [
            ".pptx",
            ".pptm",
            ".ppt",
            ".ppsx",
            ".ppsm",
            ".pps",
            ".potx",
            ".potm",
            ".pot",
            ".dps",
            ".dpt"
        ];

        private readonly DynamicPresentationAccessor dynamicPresentationAccessor;
        private readonly IAppLogger logger;

        public RotPresentationDiscovery(DynamicPresentationAccessor dynamicPresentationAccessor, IAppLogger logger)
        {
            this.dynamicPresentationAccessor = dynamicPresentationAccessor ?? throw new ArgumentNullException(nameof(dynamicPresentationAccessor));
            this.logger = (logger ?? throw new ArgumentNullException(nameof(logger))).ForCategory(nameof(RotPresentationDiscovery));
        }

        internal PresentationBindingCandidate? FindBestCandidate(bool isWpsSupportEnabled)
        {
            IRunningObjectTable? runningObjectTable = null;
            IEnumMoniker? enumMoniker = null;
            List<object> scannedApplications = [];
            PresentationBindingCandidate? bestCandidate = null;

            try
            {
                int result = GetRunningObjectTable(0, out runningObjectTable);
                if (result != 0 || runningObjectTable == null)
                {
                    return null;
                }

                runningObjectTable.EnumRunning(out enumMoniker);
                if (enumMoniker == null)
                {
                    return null;
                }

                IMoniker[] monikers = new IMoniker[1];
                while (enumMoniker.Next(1, monikers, IntPtr.Zero) == 0)
                {
                    IBindCtx? bindContext = null;
                    IMoniker? moniker = monikers[0];
                    object? runningObject = null;
                    object? applicationObject = null;
                    bool keepApplicationAlive = false;

                    try
                    {
                        int bindContextResult = CreateBindCtx(0, out bindContext);
                        if (bindContextResult != 0 || bindContext == null || moniker == null)
                        {
                            continue;
                        }

                        moniker.GetDisplayName(bindContext, null, out string displayName);
                        if (!LooksLikePresentationMoniker(displayName))
                        {
                            continue;
                        }

                        runningObjectTable.GetObject(moniker, out runningObject);
                        if (runningObject == null)
                        {
                            continue;
                        }

                        applicationObject = GetApplicationObject(runningObject);
                        if (applicationObject == null)
                        {
                            continue;
                        }

                        if (scannedApplications.Exists(scannedApplication =>
                                ComInteropHelper.AreSameComObjects(scannedApplication, applicationObject)))
                        {
                            continue;
                        }

                        scannedApplications.Add(applicationObject);
                        keepApplicationAlive = true;

                        if (!dynamicPresentationAccessor.TryBuildCandidate(applicationObject, isWpsSupportEnabled, out PresentationBindingCandidate? candidate)
                            || candidate == null)
                        {
                            continue;
                        }

                        if (bestCandidate == null || candidate.Priority > bestCandidate.Priority)
                        {
                            bestCandidate = candidate;
                            continue;
                        }
                    }
                    catch (COMException ex)
                    {
                        logger.Error(ex, "Presentation Session | ROT scan item failed");
                    }
                    catch (TargetInvocationException ex)
                    {
                        logger.Error(ex, "Presentation Session | ROT invocation failed");
                    }
                    finally
                    {
                        if (!keepApplicationAlive)
                        {
                            ComInteropHelper.SafeRelease(applicationObject);
                        }

                        if (!ComInteropHelper.AreSameComObjects(runningObject, applicationObject))
                        {
                            ComInteropHelper.SafeRelease(runningObject);
                        }

                        ComInteropHelper.SafeRelease(bindContext);
                        ComInteropHelper.SafeRelease(moniker);
                        monikers[0] = null!;
                    }
                }
            }
            finally
            {
                foreach (object scannedApplication in scannedApplications)
                {
                    if (bestCandidate != null && bestCandidate.MatchesApplication(scannedApplication))
                    {
                        continue;
                    }

                    ComInteropHelper.SafeRelease(scannedApplication);
                }

                ComInteropHelper.SafeRelease(enumMoniker);
                ComInteropHelper.SafeRelease(runningObjectTable);
            }

            return bestCandidate;
        }

        private static object? GetApplicationObject(object runningObject)
        {
            if (DynamicPresentationAccessor.TryGetPropertyObject(runningObject, "Application", out object? applicationObject)
                && applicationObject != null)
            {
                return applicationObject;
            }

            return runningObject;
        }

        private static bool LooksLikePresentationMoniker(string? displayName)
        {
            if (string.IsNullOrWhiteSpace(displayName))
            {
                return false;
            }

            if (string.Equals(displayName, PowerPointApplicationMoniker, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return PresentationExtensions.Any(extension =>
                displayName.IndexOf(extension, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        // Source-generated COM marshalling does not currently cover these classic COM interface outputs well.
        [DllImport("ole32.dll", EntryPoint = "GetRunningObjectTable")]
        private static extern int GetRunningObjectTable(int reserved, out IRunningObjectTable? prot);

        [DllImport("ole32.dll", EntryPoint = "CreateBindCtx")]
        private static extern int CreateBindCtx(int reserved, out IBindCtx? bindContext);
    }
}

