using Ink_Canvas.Controllers;
using Ink_Canvas.Services.Logging;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

namespace Ink_Canvas.Controllers.Presentation
{
    internal sealed class RotPresentationDiscovery
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
            List<object> scannedApplications = new List<object>();
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
                    object? runningObject = null;
                    object? applicationObject = null;
                    bool keepApplicationAlive = false;

                    try
                    {
                        CreateBindCtx(0, out bindContext);
                        monikers[0].GetDisplayName(bindContext, null, out string displayName);
                        if (!LooksLikePresentationMoniker(displayName))
                        {
                            continue;
                        }

                        runningObjectTable.GetObject(monikers[0], out runningObject);
                        if (runningObject == null)
                        {
                            continue;
                        }

                        applicationObject = GetApplicationObject(runningObject);
                        if (applicationObject == null)
                        {
                            continue;
                        }

                        bool isDuplicate = false;
                        foreach (object scannedApplication in scannedApplications)
                        {
                            if (ComInteropHelper.AreSameComObjects(scannedApplication, applicationObject))
                            {
                                isDuplicate = true;
                                break;
                            }
                        }

                        if (isDuplicate)
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

                        if (bindContext != null)
                        {
                            Marshal.ReleaseComObject(bindContext);
                        }

                        if (monikers[0] != null)
                        {
                            Marshal.ReleaseComObject(monikers[0]);
                        }
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

                if (enumMoniker != null)
                {
                    Marshal.ReleaseComObject(enumMoniker);
                }

                if (runningObjectTable != null)
                {
                    Marshal.ReleaseComObject(runningObjectTable);
                }
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

            foreach (string extension in PresentationExtensions)
            {
                if (displayName.IndexOf(extension, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            return false;
        }

        [DllImport("ole32.dll")]
        private static extern int GetRunningObjectTable(int reserved, out IRunningObjectTable? prot);

        [DllImport("ole32.dll")]
        private static extern int CreateBindCtx(int reserved, out IBindCtx? bindContext);
    }
}

