using Ink_Canvas.Services.Logging;
using Ink_Canvas.ViewModels;
using Microsoft.Office.Interop.PowerPoint;
using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Ink_Canvas.Controllers.Presentation
{
    internal sealed class DynamicPresentationAccessor
    {
        private readonly IAppLogger logger;

        public DynamicPresentationAccessor(IAppLogger logger)
        {
            this.logger = (logger ?? throw new ArgumentNullException(nameof(logger))).ForCategory(nameof(DynamicPresentationAccessor));
        }

        internal bool TryBuildCandidate(object applicationObject, bool isWpsSupportEnabled, out PresentationBindingCandidate? candidate)
        {
            candidate = null;
            if (!TryReadState(applicationObject, isWpsSupportEnabled, out PresentationRuntimeState state, out int priority))
            {
                return false;
            }

            candidate = new PresentationBindingCandidate(applicationObject, state, priority);
            return true;
        }

        internal bool TryReadState(
            object applicationObject,
            bool isWpsSupportEnabled,
            out PresentationRuntimeState state,
            out int priority)
        {
            state = PresentationRuntimeState.Disconnected;
            priority = 0;

            if (applicationObject == null)
            {
                return false;
            }

            object? presentationObject = null;
            object? slideShowWindowObject = null;
            try
            {
                if (!TryGetPropertyObject(applicationObject, "ActivePresentation", out presentationObject) || presentationObject == null)
                {
                    return false;
                }

                string presentationIdentity = TryGetStringProperty(presentationObject, "FullName");
                string presentationName = TryGetStringProperty(presentationObject, "Name");
                if (string.IsNullOrWhiteSpace(presentationName))
                {
                    presentationName = Path.GetFileName(presentationIdentity) ?? string.Empty;
                }

                int slideCount = GetTotalSlideCount(presentationObject);
                slideShowWindowObject = TryGetSlideShowWindow(applicationObject, presentationObject);
                bool isSlideShowRunning = slideShowWindowObject != null || GetSlideShowWindowsCount(applicationObject) > 0;
                int currentSlideIndex = isSlideShowRunning ? GetCurrentSlideIndex(slideShowWindowObject) : 0;

                string applicationName = TryGetStringProperty(applicationObject, "Name");
                if (string.IsNullOrWhiteSpace(applicationName))
                {
                    applicationName = TryGetStringProperty(applicationObject, "Caption");
                }

                PresentationProvider provider = PresentationWindowLocator.DetectProvider(presentationIdentity, applicationName);
                if (provider is PresentationProvider.Wps && !isWpsSupportEnabled)
                {
                    return false;
                }

                priority = 1;
                if (isSlideShowRunning)
                {
                    priority = 2;
                    if (PresentationWindowLocator.IsPresentationForeground(
                        slideShowWindowObject,
                        presentationIdentity,
                        applicationName,
                        provider))
                    {
                        priority = 3;
                    }
                }

                state = new PresentationRuntimeState(
                    true,
                    provider,
                    presentationIdentity ?? string.Empty,
                    presentationName ?? string.Empty,
                    slideCount,
                    currentSlideIndex,
                    isSlideShowRunning);
                return true;
            }
            catch (COMException ex)
            {
                logger.Error(ex, "Presentation Session | Dynamic state read failed");
                return false;
            }
            catch (TargetInvocationException ex)
            {
                logger.Error(ex, "Presentation Session | Dynamic state invocation failed");
                return false;
            }
            finally
            {
                ComInteropHelper.SafeRelease(slideShowWindowObject);
                ComInteropHelper.SafeRelease(presentationObject);
            }
        }

        internal bool TryGoToSlide(object applicationObject, int slideNumber)
        {
            object? presentationObject = null;
            object? slideShowWindowObject = null;
            object? windowsObject = null;
            object? windowObject = null;
            object? viewObject = null;
            try
            {
                if (!TryGetPropertyObject(applicationObject, "ActivePresentation", out presentationObject) || presentationObject == null)
                {
                    return false;
                }

                slideShowWindowObject = TryGetSlideShowWindow(applicationObject, presentationObject);
                if (slideShowWindowObject != null)
                {
                    TryActivateSlideShowWindow(slideShowWindowObject);
                    if (TryGetPropertyObject(slideShowWindowObject, "View", out viewObject) && viewObject != null)
                    {
                        return TryInvokeMethod(viewObject, "GotoSlide", [slideNumber], out _);
                    }
                }

                if (!TryGetPropertyObject(presentationObject, "Windows", out windowsObject) || windowsObject == null)
                {
                    return false;
                }

                if (!TryGetCollectionItem(windowsObject, 1, out windowObject) || windowObject == null)
                {
                    return false;
                }

                if (!TryGetPropertyObject(windowObject, "View", out viewObject) || viewObject == null)
                {
                    return false;
                }

                return TryInvokeMethod(viewObject, "GotoSlide", [slideNumber], out _);
            }
            finally
            {
                ComInteropHelper.SafeRelease(viewObject);
                ComInteropHelper.SafeRelease(windowObject);
                ComInteropHelper.SafeRelease(windowsObject);
                ComInteropHelper.SafeRelease(slideShowWindowObject);
                ComInteropHelper.SafeRelease(presentationObject);
            }
        }

        internal bool TryGoToPreviousSlide(object applicationObject)
        {
            return TryExecuteSlideShowViewAction(applicationObject, "Previous");
        }

        internal bool TryGoToNextSlide(object applicationObject)
        {
            return TryExecuteSlideShowViewAction(applicationObject, "Next");
        }

        internal bool TryExitSlideShow(object applicationObject)
        {
            return TryExecuteSlideShowViewAction(applicationObject, "Exit");
        }

        internal bool TryShowSlideNavigation(object applicationObject)
        {
            object? presentationObject = null;
            object? slideShowWindowObject = null;
            object? slideNavigationObject = null;
            try
            {
                if (!TryGetPropertyObject(applicationObject, "ActivePresentation", out presentationObject) || presentationObject == null)
                {
                    return false;
                }

                slideShowWindowObject = TryGetSlideShowWindow(applicationObject, presentationObject);
                if (slideShowWindowObject == null)
                {
                    return false;
                }

                if (!TryGetPropertyObject(slideShowWindowObject, "SlideNavigation", out slideNavigationObject) || slideNavigationObject == null)
                {
                    return false;
                }

                return TrySetProperty(slideNavigationObject, "Visible", true);
            }
            finally
            {
                ComInteropHelper.SafeRelease(slideNavigationObject);
                ComInteropHelper.SafeRelease(slideShowWindowObject);
                ComInteropHelper.SafeRelease(presentationObject);
            }
        }

        internal bool HasHiddenSlides(object applicationObject)
        {
            return VisitSlides(applicationObject, slideObject => IsSlideHidden(slideObject), false);
        }

        internal bool TryUnhideHiddenSlides(object applicationObject)
        {
            return VisitSlides(
                applicationObject,
                slideObject =>
                {
                    if (!IsSlideHidden(slideObject))
                    {
                        return false;
                    }

                    SetSlideHidden(slideObject, false);
                    return true;
                },
                true);
        }

        internal bool HasAutomaticAdvance(object applicationObject)
        {
            return VisitSlides(applicationObject, slideObject => SlideHasAutomaticAdvance(slideObject), false);
        }

        internal bool TryDisableAutomaticAdvance(object applicationObject)
        {
            object? presentationObject = null;
            object? slideShowSettingsObject = null;
            try
            {
                if (!TryGetPropertyObject(applicationObject, "ActivePresentation", out presentationObject) || presentationObject == null)
                {
                    return false;
                }

                if (!TryGetPropertyObject(presentationObject, "SlideShowSettings", out slideShowSettingsObject) || slideShowSettingsObject == null)
                {
                    return false;
                }

                PropertyInfo? advanceModeProperty = slideShowSettingsObject.GetType().GetProperty("AdvanceMode");
                if (advanceModeProperty == null || !advanceModeProperty.CanWrite)
                {
                    return false;
                }

                object manualAdvanceValue = Enum.ToObject(
                    advanceModeProperty.PropertyType,
                    (int)PpSlideShowAdvanceMode.ppSlideShowManualAdvance);
                advanceModeProperty.SetValue(slideShowSettingsObject, manualAdvanceValue);
                return true;
            }
            catch (COMException ex)
            {
                logger.Error(ex, "Presentation Session | Failed to disable automatic advance");
                return false;
            }
            finally
            {
                ComInteropHelper.SafeRelease(slideShowSettingsObject);
                ComInteropHelper.SafeRelease(presentationObject);
            }
        }

        internal static bool TryGetPropertyObject(object target, string propertyName, out object? value)
        {
            value = null;
            try
            {
                value = target.GetType().InvokeMember(
                    propertyName,
                    BindingFlags.GetProperty,
                    null,
                    target,
                    null);
                return value != null;
            }
            catch
            {
                return false;
            }
        }

        private static bool VisitSlides(object applicationObject, Func<object, bool> action, bool continueAfterMatch)
        {
            object? presentationObject = null;
            object? slidesObject = null;
            object? slideObject = null;
            bool hasMatch = false;
            try
            {
                if (!TryGetPropertyObject(applicationObject, "ActivePresentation", out presentationObject) || presentationObject == null)
                {
                    return false;
                }

                if (!TryGetPropertyObject(presentationObject, "Slides", out slidesObject) || slidesObject == null)
                {
                    return false;
                }

                int slideCount = GetCollectionCount(slidesObject);
                for (int slideIndex = 1; slideIndex <= slideCount; slideIndex++)
                {
                    if (!TryGetCollectionItem(slidesObject, slideIndex, out slideObject) || slideObject == null)
                    {
                        continue;
                    }

                    hasMatch |= action(slideObject);
                    ComInteropHelper.SafeRelease(slideObject);
                    slideObject = null;

                    if (hasMatch && !continueAfterMatch)
                    {
                        return true;
                    }
                }

                return hasMatch;
            }
            finally
            {
                ComInteropHelper.SafeRelease(slideObject);
                ComInteropHelper.SafeRelease(slidesObject);
                ComInteropHelper.SafeRelease(presentationObject);
            }
        }

        private static bool TryExecuteSlideShowViewAction(object applicationObject, string methodName)
        {
            object? presentationObject = null;
            object? slideShowWindowObject = null;
            object? viewObject = null;
            try
            {
                if (!TryGetPropertyObject(applicationObject, "ActivePresentation", out presentationObject) || presentationObject == null)
                {
                    return false;
                }

                slideShowWindowObject = TryGetSlideShowWindow(applicationObject, presentationObject);
                if (slideShowWindowObject == null)
                {
                    return false;
                }

                TryActivateSlideShowWindow(slideShowWindowObject);
                if (!TryGetPropertyObject(slideShowWindowObject, "View", out viewObject) || viewObject == null)
                {
                    return false;
                }

                return TryInvokeMethod(viewObject, methodName, null, out _);
            }
            finally
            {
                ComInteropHelper.SafeRelease(viewObject);
                ComInteropHelper.SafeRelease(slideShowWindowObject);
                ComInteropHelper.SafeRelease(presentationObject);
            }
        }

        private static bool TryActivateSlideShowWindow(object slideShowWindowObject)
        {
            return TryInvokeMethod(slideShowWindowObject, "Activate", null, out _);
        }

        private static object? TryGetSlideShowWindow(object applicationObject, object presentationObject)
        {
            if (TryGetPropertyObject(presentationObject, "SlideShowWindow", out object? slideShowWindowObject)
                && slideShowWindowObject != null)
            {
                return slideShowWindowObject;
            }

            if (!TryGetPropertyObject(applicationObject, "SlideShowWindows", out object? slideShowWindowsObject)
                || slideShowWindowsObject == null)
            {
                return null;
            }

            try
            {
                if (GetCollectionCount(slideShowWindowsObject) <= 0)
                {
                    return null;
                }

                return TryGetCollectionItem(slideShowWindowsObject, 1, out slideShowWindowObject)
                    ? slideShowWindowObject
                    : null;
            }
            finally
            {
                ComInteropHelper.SafeRelease(slideShowWindowsObject);
            }
        }

        private static int GetCurrentSlideIndex(object? slideShowWindowObject)
        {
            if (slideShowWindowObject == null)
            {
                return 0;
            }

            object? viewObject = null;
            object? slideObject = null;
            try
            {
                if (!TryGetPropertyObject(slideShowWindowObject, "View", out viewObject) || viewObject == null)
                {
                    return 0;
                }

                if (TryGetIntProperty(viewObject, "CurrentShowPosition", out int currentShowPosition))
                {
                    return currentShowPosition;
                }

                if (!TryGetPropertyObject(viewObject, "Slide", out slideObject) || slideObject == null)
                {
                    return 0;
                }

                if (TryGetIntProperty(slideObject, "SlideNumber", out int slideNumber))
                {
                    return slideNumber;
                }

                return TryGetIntProperty(slideObject, "SlideIndex", out int slideIndex)
                    ? slideIndex
                    : 0;
            }
            finally
            {
                ComInteropHelper.SafeRelease(slideObject);
                ComInteropHelper.SafeRelease(viewObject);
            }
        }

        private static int GetTotalSlideCount(object presentationObject)
        {
            object? slidesObject = null;
            try
            {
                if (!TryGetPropertyObject(presentationObject, "Slides", out slidesObject) || slidesObject == null)
                {
                    return 0;
                }

                return GetCollectionCount(slidesObject);
            }
            finally
            {
                ComInteropHelper.SafeRelease(slidesObject);
            }
        }

        private static int GetSlideShowWindowsCount(object applicationObject)
        {
            object? slideShowWindowsObject = null;
            try
            {
                if (!TryGetPropertyObject(applicationObject, "SlideShowWindows", out slideShowWindowsObject)
                    || slideShowWindowsObject == null)
                {
                    return 0;
                }

                return GetCollectionCount(slideShowWindowsObject);
            }
            finally
            {
                ComInteropHelper.SafeRelease(slideShowWindowsObject);
            }
        }

        private static int GetCollectionCount(object collectionObject)
        {
            return TryGetIntProperty(collectionObject, "Count", out int count) ? count : 0;
        }

        private static bool IsSlideHidden(object slideObject)
        {
            return IsOfficeTrueState(GetSlideShowTransitionPropertyValue(slideObject, "Hidden"));
        }

        private static void SetSlideHidden(object slideObject, bool isHidden)
        {
            object? transitionObject = null;
            try
            {
                if (!TryGetPropertyObject(slideObject, "SlideShowTransition", out transitionObject) || transitionObject == null)
                {
                    return;
                }

                PropertyInfo? hiddenProperty = transitionObject.GetType().GetProperty("Hidden");
                if (hiddenProperty == null || !hiddenProperty.CanWrite)
                {
                    return;
                }

                object hiddenValue = Enum.ToObject(hiddenProperty.PropertyType, isHidden ? -1 : 0);
                hiddenProperty.SetValue(transitionObject, hiddenValue);
            }
            finally
            {
                ComInteropHelper.SafeRelease(transitionObject);
            }
        }

        private static bool SlideHasAutomaticAdvance(object slideObject)
        {
            object? advanceOnTime = GetSlideShowTransitionPropertyValue(slideObject, "AdvanceOnTime");
            object? advanceTime = GetSlideShowTransitionPropertyValue(slideObject, "AdvanceTime");
            return IsOfficeTrueState(advanceOnTime) && Convert.ToDouble(advanceTime) > 0;
        }

        private static object? GetSlideShowTransitionPropertyValue(object slideObject, string propertyName)
        {
            object? transitionObject = null;
            try
            {
                if (!TryGetPropertyObject(slideObject, "SlideShowTransition", out transitionObject) || transitionObject == null)
                {
                    return null;
                }

                PropertyInfo? property = transitionObject.GetType().GetProperty(propertyName);
                return property?.GetValue(transitionObject);
            }
            finally
            {
                ComInteropHelper.SafeRelease(transitionObject);
            }
        }

        private static bool IsOfficeTrueState(object? value)
        {
            return value switch
            {
                bool boolValue => boolValue,
                null => false,
                _ => Convert.ToInt32(value) == -1
            };
        }

        private static bool TrySetProperty(object target, string propertyName, object? value)
        {
            try
            {
                target.GetType().InvokeMember(
                    propertyName,
                    BindingFlags.SetProperty,
                    null,
                    target,
                    [value]);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool TryInvokeMethod(object target, string methodName, object[]? parameters, out object? result)
        {
            result = null;
            try
            {
                result = target.GetType().InvokeMember(
                    methodName,
                    BindingFlags.InvokeMethod,
                    null,
                    target,
                    parameters);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool TryGetCollectionItem(object collection, int index, out object? item)
        {
            item = null;
            try
            {
                item = collection.GetType().InvokeMember(
                    "Item",
                    BindingFlags.GetProperty,
                    null,
                    collection,
                    [index]);
                return item != null;
            }
            catch
            {
                try
                {
                    item = collection.GetType().InvokeMember(
                        string.Empty,
                        BindingFlags.GetProperty,
                        null,
                        collection,
                        [index]);
                    return item != null;
                }
                catch
                {
                    return false;
                }
            }
        }

        private static bool TryGetIntProperty(object target, string propertyName, out int value)
        {
            value = 0;
            if (!TryGetPropertyObject(target, propertyName, out object? propertyValue) || propertyValue == null)
            {
                return false;
            }

            try
            {
                value = Convert.ToInt32(propertyValue);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static string TryGetStringProperty(object target, string propertyName)
        {
            if (!TryGetPropertyObject(target, propertyName, out object? propertyValue) || propertyValue == null)
            {
                return string.Empty;
            }

            return propertyValue.ToString() ?? string.Empty;
        }
    }
}

