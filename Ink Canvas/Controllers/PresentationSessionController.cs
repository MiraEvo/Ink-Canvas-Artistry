using Ink_Canvas.Helpers;
using Ink_Canvas.ViewModels;
using Microsoft.Office.Interop.PowerPoint;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Timer = System.Timers.Timer;

namespace Ink_Canvas.Controllers
{
    public sealed class PresentationSessionController : IPresentationSessionController
    {
        private const double MonitorIntervalMs = 500;

        private readonly PresentationSessionViewModel presentationSessionViewModel;
        private readonly Func<bool> isWpsSupportEnabled;
        private readonly Func<bool> shouldSkipDetection;
        private readonly Timer monitorTimer;
        private int isDetecting;

        private object? boundApplicationObject;
        private string boundPresentationIdentity = string.Empty;
        private bool areInteropEventsBound;
        private PresentationRuntimeState publishedState = PresentationRuntimeState.Disconnected;

        public PresentationSessionController(
            PresentationSessionViewModel presentationSessionViewModel,
            Func<bool> isWpsSupportEnabled,
            Func<bool> shouldSkipDetection)
        {
            this.presentationSessionViewModel = presentationSessionViewModel;
            this.isWpsSupportEnabled = isWpsSupportEnabled;
            this.shouldSkipDetection = shouldSkipDetection;

            monitorTimer = new Timer(MonitorIntervalMs);
            monitorTimer.Elapsed += MonitorTimer_Elapsed;
        }

        public event Action? PresentationConnected;

        public event Action? PresentationClosed;

        public event Action? SlideShowBegin;

        public event Action? SlideShowNextSlide;

        public event Action? SlideShowEnd;

        public Microsoft.Office.Interop.PowerPoint.Application? PowerPointApplication { get; private set; }

        public Presentation? Presentation { get; private set; }

        public Slides? Slides { get; private set; }

        public Slide? Slide { get; private set; }

        public void StartMonitoring()
        {
            monitorTimer.Start();
            QueueImmediateDetection();
        }

        public void StopMonitoring()
        {
            monitorTimer.Stop();
            ClearBinding();
            PublishState(PresentationRuntimeState.Disconnected);
        }

        public bool TryGoToSlide(int slideNumber)
        {
            return boundApplicationObject != null && DynamicPresentationAccessor.TryGoToSlide(boundApplicationObject, slideNumber);
        }

        public bool TryGoToPreviousSlide()
        {
            return boundApplicationObject != null && DynamicPresentationAccessor.TryGoToPreviousSlide(boundApplicationObject);
        }

        public bool TryGoToNextSlide()
        {
            return boundApplicationObject != null && DynamicPresentationAccessor.TryGoToNextSlide(boundApplicationObject);
        }

        public bool TryExitSlideShow()
        {
            return boundApplicationObject != null && DynamicPresentationAccessor.TryExitSlideShow(boundApplicationObject);
        }

        public bool TryShowSlideNavigation()
        {
            return boundApplicationObject != null && DynamicPresentationAccessor.TryShowSlideNavigation(boundApplicationObject);
        }

        public bool HasHiddenSlides()
        {
            return boundApplicationObject != null && DynamicPresentationAccessor.HasHiddenSlides(boundApplicationObject);
        }

        public bool TryUnhideHiddenSlides()
        {
            return boundApplicationObject != null && DynamicPresentationAccessor.TryUnhideHiddenSlides(boundApplicationObject);
        }

        public bool HasAutomaticAdvance()
        {
            return boundApplicationObject != null && DynamicPresentationAccessor.HasAutomaticAdvance(boundApplicationObject);
        }

        public bool TryDisableAutomaticAdvance()
        {
            return boundApplicationObject != null && DynamicPresentationAccessor.TryDisableAutomaticAdvance(boundApplicationObject);
        }

        private void MonitorTimer_Elapsed(object? sender, ElapsedEventArgs e)
        {
            QueueImmediateDetection();
        }

        private void QueueImmediateDetection()
        {
            _ = Task.Run(TryMonitorSession);
        }

        private void TryMonitorSession()
        {
            if (shouldSkipDetection())
            {
                return;
            }

            if (Interlocked.Exchange(ref isDetecting, 1) == 1)
            {
                return;
            }

            try
            {
                MonitorSession();
            }
            finally
            {
                Interlocked.Exchange(ref isDetecting, 0);
            }
        }

        private void MonitorSession()
        {
            PresentationBindingCandidate? bestCandidate = null;
            try
            {
                bestCandidate = RotPresentationDiscovery.FindBestCandidate(isWpsSupportEnabled());
                if (bestCandidate == null)
                {
                    ClearBinding();
                    PublishState(PresentationRuntimeState.Disconnected);
                    return;
                }

                bool shouldRebind = boundApplicationObject == null
                    || !bestCandidate.MatchesApplication(boundApplicationObject)
                    || !PresentationIdentitiesEqual(boundPresentationIdentity, bestCandidate.State.PresentationIdentity);
                if (shouldRebind)
                {
                    BindCandidate(bestCandidate);
                    UpdateStrongInteropState();
                    PublishState(bestCandidate.State);
                    bestCandidate = null;
                    return;
                }

                boundPresentationIdentity = bestCandidate.State.PresentationIdentity;
                UpdateStrongInteropState();
                PublishState(bestCandidate.State);
            }
            finally
            {
                bestCandidate?.Dispose();
            }
        }

        private void BindCandidate(PresentationBindingCandidate candidate)
        {
            ClearBinding();

            boundApplicationObject = candidate.DetachApplicationObject();
            boundPresentationIdentity = candidate.State.PresentationIdentity;
            PowerPointApplication = boundApplicationObject as Microsoft.Office.Interop.PowerPoint.Application;

            TryBindInteropEvents();
        }

        private void UpdateStrongInteropState()
        {
            ComInteropHelper.SafeRelease(Slide);
            ComInteropHelper.SafeRelease(Slides);
            ComInteropHelper.SafeRelease(Presentation);

            Presentation = null;
            Slides = null;
            Slide = null;

            if (PowerPointApplication == null)
            {
                return;
            }

            try
            {
                Presentation = PowerPointApplication.ActivePresentation;
            }
            catch
            {
                Presentation = null;
            }

            try
            {
                Slides = Presentation?.Slides;
            }
            catch
            {
                Slides = null;
            }

            try
            {
                Slide = PowerPointApplication.SlideShowWindows.Count >= 1
                    ? PowerPointApplication.SlideShowWindows[1].View.Slide
                    : null;
            }
            catch
            {
                Slide = null;
            }
        }

        private void PublishState(PresentationRuntimeState newState)
        {
            PresentationRuntimeState previousState = publishedState;
            publishedState = newState;

            RunOnUiThread(() =>
            {
                if (!newState.IsConnected)
                {
                    presentationSessionViewModel.Disconnect();
                }
                else
                {
                    presentationSessionViewModel.SetConnection(
                        newState.Provider,
                        newState.PresentationName,
                        newState.SlideCount,
                        newState.CurrentSlideIndex,
                        newState.IsSlideShowRunning);

                    if (!newState.IsSlideShowRunning)
                    {
                        presentationSessionViewModel.SetNavigationVisibility(false, false);
                    }
                }

                bool presentationChanged = previousState.IsConnected
                    && newState.IsConnected
                    && !PresentationIdentitiesEqual(previousState.PresentationIdentity, newState.PresentationIdentity);
                if (presentationChanged)
                {
                    if (previousState.IsSlideShowRunning)
                    {
                        presentationSessionViewModel.SetNavigationVisibility(false, false);
                        SlideShowEnd?.Invoke();
                    }

                    PresentationClosed?.Invoke();
                    PresentationConnected?.Invoke();

                    if (newState.IsSlideShowRunning)
                    {
                        SlideShowBegin?.Invoke();
                    }

                    return;
                }

                if (!previousState.IsConnected && newState.IsConnected)
                {
                    PresentationConnected?.Invoke();
                }
                else if (previousState.IsConnected && !newState.IsConnected)
                {
                    if (previousState.IsSlideShowRunning)
                    {
                        presentationSessionViewModel.SetNavigationVisibility(false, false);
                        SlideShowEnd?.Invoke();
                    }

                    PresentationClosed?.Invoke();
                    return;
                }

                if (!newState.IsConnected)
                {
                    return;
                }

                if (!previousState.IsSlideShowRunning && newState.IsSlideShowRunning)
                {
                    SlideShowBegin?.Invoke();
                }
                else if (previousState.IsSlideShowRunning && !newState.IsSlideShowRunning)
                {
                    presentationSessionViewModel.SetNavigationVisibility(false, false);
                    SlideShowEnd?.Invoke();
                }
                else if (newState.IsSlideShowRunning && previousState.CurrentSlideIndex != newState.CurrentSlideIndex)
                {
                    SlideShowNextSlide?.Invoke();
                }
            });
        }

        private void TryBindInteropEvents()
        {
            UnbindInteropEvents();
            if (PowerPointApplication == null)
            {
                return;
            }

            try
            {
                PowerPointApplication.PresentationClose += OnPresentationClose;
                PowerPointApplication.SlideShowBegin += OnSlideShowBegin;
                PowerPointApplication.SlideShowNextSlide += OnSlideShowNextSlide;
                PowerPointApplication.SlideShowEnd += OnSlideShowEnd;
                areInteropEventsBound = true;
            }
            catch (COMException ex)
            {
                LogHelper.WriteLogToFile(ex, "Presentation Session | Failed to bind interop events");
                areInteropEventsBound = false;
            }
            catch (InvalidOperationException ex)
            {
                LogHelper.WriteLogToFile(ex, "Presentation Session | Invalid state while binding interop events");
                areInteropEventsBound = false;
            }
        }

        private void UnbindInteropEvents()
        {
            if (!areInteropEventsBound || PowerPointApplication == null)
            {
                return;
            }

            try
            {
                PowerPointApplication.PresentationClose -= OnPresentationClose;
                PowerPointApplication.SlideShowBegin -= OnSlideShowBegin;
                PowerPointApplication.SlideShowNextSlide -= OnSlideShowNextSlide;
                PowerPointApplication.SlideShowEnd -= OnSlideShowEnd;
            }
            catch (COMException ex)
            {
                LogHelper.WriteLogToFile(ex, "Presentation Session | Failed to unbind interop events");
            }
            catch (InvalidOperationException ex)
            {
                LogHelper.WriteLogToFile(ex, "Presentation Session | Invalid state while unbinding interop events");
            }
            finally
            {
                areInteropEventsBound = false;
            }
        }

        private void ClearBinding()
        {
            UnbindInteropEvents();

            ComInteropHelper.SafeRelease(Slide);
            ComInteropHelper.SafeRelease(Slides);
            ComInteropHelper.SafeRelease(Presentation);

            Slide = null;
            Slides = null;
            Presentation = null;
            PowerPointApplication = null;

            ComInteropHelper.SafeFinalRelease(boundApplicationObject);
            boundApplicationObject = null;
            boundPresentationIdentity = string.Empty;
        }

        private void OnPresentationClose(Presentation presentation)
        {
            QueueImmediateDetection();
        }

        private void OnSlideShowBegin(SlideShowWindow window)
        {
            QueueImmediateDetection();
        }

        private void OnSlideShowNextSlide(SlideShowWindow window)
        {
            QueueImmediateDetection();
        }

        private void OnSlideShowEnd(Presentation presentation)
        {
            QueueImmediateDetection();
        }

        private static bool PresentationIdentitiesEqual(string? left, string? right)
        {
            return string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
        }

        private static void RunOnUiThread(Action? action)
        {
            if (action == null)
            {
                return;
            }

            if (System.Windows.Application.Current?.Dispatcher == null
                || System.Windows.Application.Current.Dispatcher.CheckAccess())
            {
                action();
                return;
            }

            System.Windows.Application.Current.Dispatcher.Invoke(action);
        }
    }
}
