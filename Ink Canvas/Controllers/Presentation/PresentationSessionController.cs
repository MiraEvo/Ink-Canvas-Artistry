using Ink_Canvas.Services.Logging;
using Ink_Canvas.ViewModels;
using Microsoft.Office.Interop.PowerPoint;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using PowerPointPresentation = Microsoft.Office.Interop.PowerPoint.Presentation;
using Timer = System.Timers.Timer;

namespace Ink_Canvas.Controllers.Presentation
{
    public sealed class PresentationSessionController : IPresentationSessionController
    {
        private const double MonitorIntervalMs = 500;

        private readonly PresentationSessionViewModel presentationSessionViewModel;
        private readonly Func<bool> isWpsSupportEnabled;
        private readonly Func<bool> shouldSkipDetection;
        private readonly IAppLogger logger;
        private readonly DynamicPresentationAccessor dynamicPresentationAccessor;
        private readonly RotPresentationDiscovery rotPresentationDiscovery;
        private readonly Timer monitorTimer;

        private int isMonitoring;
        private int isDetectionQueued;
        private int isDetectionWorkerRunning;

        private object? boundApplicationObject;
        private string boundPresentationIdentity = string.Empty;
        private bool areInteropEventsBound;
        private PresentationRuntimeState publishedState = PresentationRuntimeState.Disconnected;

        public PresentationSessionController(
            PresentationSessionViewModel presentationSessionViewModel,
            Func<bool> isWpsSupportEnabled,
            Func<bool> shouldSkipDetection,
            IAppLogger logger)
        {
            ArgumentNullException.ThrowIfNull(presentationSessionViewModel);
            ArgumentNullException.ThrowIfNull(isWpsSupportEnabled);
            ArgumentNullException.ThrowIfNull(shouldSkipDetection);
            ArgumentNullException.ThrowIfNull(logger);

            this.presentationSessionViewModel = presentationSessionViewModel;
            this.isWpsSupportEnabled = isWpsSupportEnabled;
            this.shouldSkipDetection = shouldSkipDetection;
            this.logger = logger.ForCategory(nameof(PresentationSessionController));
            dynamicPresentationAccessor = new DynamicPresentationAccessor(this.logger);
            rotPresentationDiscovery = new RotPresentationDiscovery(dynamicPresentationAccessor, this.logger);

            monitorTimer = CreateTimer(MonitorIntervalMs, MonitorTimer_Elapsed);
        }

        public event Action? PresentationConnected;

        public event Action? PresentationClosed;

        public event Action? SlideShowBegin;

        public event Action? SlideShowNextSlide;

        public event Action? SlideShowEnd;

        public Microsoft.Office.Interop.PowerPoint.Application? PowerPointApplication { get; private set; }

        public PowerPointPresentation? Presentation { get; private set; }

        public Slides? Slides { get; private set; }

        public Slide? Slide { get; private set; }

        public void StartMonitoring()
        {
            Interlocked.Exchange(ref isMonitoring, 1);
            monitorTimer.Start();
            QueueImmediateDetection();
        }

        public void StopMonitoring()
        {
            Interlocked.Exchange(ref isMonitoring, 0);
            Interlocked.Exchange(ref isDetectionQueued, 0);
            monitorTimer.Stop();
            ClearBinding();
            PublishState(PresentationRuntimeState.Disconnected);
        }

        public bool TryGoToSlide(int slideNumber) =>
            boundApplicationObject != null && dynamicPresentationAccessor.TryGoToSlide(boundApplicationObject, slideNumber);

        public bool TryGoToPreviousSlide() =>
            boundApplicationObject != null && dynamicPresentationAccessor.TryGoToPreviousSlide(boundApplicationObject);

        public bool TryGoToNextSlide() =>
            boundApplicationObject != null && dynamicPresentationAccessor.TryGoToNextSlide(boundApplicationObject);

        public bool TryExitSlideShow() =>
            boundApplicationObject != null && dynamicPresentationAccessor.TryExitSlideShow(boundApplicationObject);

        public bool TryShowSlideNavigation() =>
            boundApplicationObject != null && dynamicPresentationAccessor.TryShowSlideNavigation(boundApplicationObject);

        public bool HasHiddenSlides() =>
            boundApplicationObject != null && dynamicPresentationAccessor.HasHiddenSlides(boundApplicationObject);

        public bool TryUnhideHiddenSlides() =>
            boundApplicationObject != null && dynamicPresentationAccessor.TryUnhideHiddenSlides(boundApplicationObject);

        public bool HasAutomaticAdvance() =>
            boundApplicationObject != null && dynamicPresentationAccessor.HasAutomaticAdvance(boundApplicationObject);

        public bool TryDisableAutomaticAdvance() =>
            boundApplicationObject != null && dynamicPresentationAccessor.TryDisableAutomaticAdvance(boundApplicationObject);

        private void MonitorTimer_Elapsed(object? sender, ElapsedEventArgs e) => QueueImmediateDetection();

        private void QueueImmediateDetection()
        {
            if (!IsMonitoringEnabled())
            {
                return;
            }

            Interlocked.Exchange(ref isDetectionQueued, 1);
            if (Interlocked.CompareExchange(ref isDetectionWorkerRunning, 1, 0) != 0)
            {
                return;
            }

            _ = Task.Run(ProcessDetectionQueue);
        }

        private void ProcessDetectionQueue()
        {
            try
            {
                while (IsMonitoringEnabled() && Interlocked.Exchange(ref isDetectionQueued, 0) == 1)
                {
                    TryMonitorSession();
                }
            }
            finally
            {
                Interlocked.Exchange(ref isDetectionWorkerRunning, 0);

                if (IsMonitoringEnabled() && Volatile.Read(ref isDetectionQueued) == 1)
                {
                    QueueImmediateDetection();
                }
            }
        }

        private void TryMonitorSession()
        {
            if (!IsMonitoringEnabled() || shouldSkipDetection())
            {
                return;
            }

            try
            {
                MonitorSession();
            }
            catch (COMException ex)
            {
                logger.Error(ex, "Presentation Session | Detection failed during COM access");
            }
            catch (InvalidOperationException ex)
            {
                logger.Error(ex, "Presentation Session | Detection failed due to invalid state");
            }
            catch (ArgumentException ex)
            {
                logger.Error(ex, "Presentation Session | Detection failed due to invalid arguments");
            }
        }

        private void MonitorSession()
        {
            PresentationBindingCandidate? bestCandidate = null;
            try
            {
                bestCandidate = rotPresentationDiscovery.FindBestCandidate(isWpsSupportEnabled());
                if (!IsMonitoringEnabled())
                {
                    return;
                }

                if (bestCandidate == null)
                {
                    ClearBinding();
                    PublishState(PresentationRuntimeState.Disconnected);
                    return;
                }

                if (ShouldRebind(bestCandidate))
                {
                    BindCandidate(bestCandidate);
                    RefreshStrongInteropState();
                    PublishState(bestCandidate.State);
                    bestCandidate = null;
                    return;
                }

                boundPresentationIdentity = bestCandidate.State.PresentationIdentity;
                RefreshStrongInteropState();
                PublishState(bestCandidate.State);
            }
            finally
            {
                bestCandidate?.Dispose();
            }
        }

        private bool ShouldRebind(PresentationBindingCandidate candidate)
        {
            return boundApplicationObject == null
                || !candidate.MatchesApplication(boundApplicationObject)
                || !PresentationIdentitiesEqual(boundPresentationIdentity, candidate.State.PresentationIdentity);
        }

        private void BindCandidate(PresentationBindingCandidate candidate)
        {
            ClearBinding();

            boundApplicationObject = candidate.DetachApplicationObject();
            boundPresentationIdentity = candidate.State.PresentationIdentity;
            PowerPointApplication = boundApplicationObject as Microsoft.Office.Interop.PowerPoint.Application;

            TryBindInteropEvents();
        }

        private void RefreshStrongInteropState()
        {
            ReleaseStrongInteropState();

            if (PowerPointApplication == null)
            {
                return;
            }

            Presentation = TryReadStrongInterop(() => PowerPointApplication.ActivePresentation);
            Slides = TryReadStrongInterop(() => Presentation?.Slides);
            Slide = TryReadStrongInterop(() =>
                PowerPointApplication.SlideShowWindows.Count >= 1
                    ? PowerPointApplication.SlideShowWindows[1].View.Slide
                    : null);
        }

        private void PublishState(PresentationRuntimeState newState)
        {
            if (!IsMonitoringEnabled() && newState.IsConnected)
            {
                return;
            }

            PresentationRuntimeState previousState = publishedState;
            publishedState = newState;

            RunOnUiThread(() =>
            {
                if (!IsMonitoringEnabled() && newState.IsConnected)
                {
                    return;
                }

                ApplyViewModelState(newState);
                PublishTransitionEvents(previousState, newState);
            }, "Presentation Session | Failed to publish session state");
        }

        private void ApplyViewModelState(PresentationRuntimeState newState)
        {
            if (!newState.IsConnected)
            {
                presentationSessionViewModel.Disconnect();
                return;
            }

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

        private void PublishTransitionEvents(PresentationRuntimeState previousState, PresentationRuntimeState newState)
        {
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
                logger.Error(ex, "Presentation Session | Failed to bind interop events");
                areInteropEventsBound = false;
            }
            catch (InvalidOperationException ex)
            {
                logger.Error(ex, "Presentation Session | Invalid state while binding interop events");
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
                logger.Error(ex, "Presentation Session | Failed to unbind interop events");
            }
            catch (InvalidOperationException ex)
            {
                logger.Error(ex, "Presentation Session | Invalid state while unbinding interop events");
            }
            finally
            {
                areInteropEventsBound = false;
            }
        }

        private void ClearBinding()
        {
            UnbindInteropEvents();
            ReleaseStrongInteropState();

            PowerPointApplication = null;

            ComInteropHelper.SafeFinalRelease(boundApplicationObject);
            boundApplicationObject = null;
            boundPresentationIdentity = string.Empty;
        }

        private void ReleaseStrongInteropState()
        {
            ComInteropHelper.SafeRelease(Slide);
            ComInteropHelper.SafeRelease(Slides);
            ComInteropHelper.SafeRelease(Presentation);

            Slide = null;
            Slides = null;
            Presentation = null;
        }

        private static T? TryReadStrongInterop<T>(Func<T?> accessor) where T : class
        {
            try
            {
                return accessor();
            }
            catch (COMException)
            {
                return null;
            }
            catch (InvalidOperationException)
            {
                return null;
            }
            catch (ArgumentException)
            {
                return null;
            }
        }

        private void OnPresentationClose(PowerPointPresentation presentation) => QueueImmediateDetection();

        private void OnSlideShowBegin(SlideShowWindow window) => QueueImmediateDetection();

        private void OnSlideShowNextSlide(SlideShowWindow window) => QueueImmediateDetection();

        private void OnSlideShowEnd(PowerPointPresentation presentation) => QueueImmediateDetection();

        private bool IsMonitoringEnabled() => Volatile.Read(ref isMonitoring) == 1;

        private static bool PresentationIdentitiesEqual(string? left, string? right) =>
            string.Equals(left, right, StringComparison.OrdinalIgnoreCase);

        private static Timer CreateTimer(double interval, ElapsedEventHandler elapsedHandler)
        {
            Timer timer = new(interval)
            {
                AutoReset = true
            };
            timer.Elapsed += elapsedHandler;
            return timer;
        }

        private void RunOnUiThread(Action action, string failureContext)
        {
            ArgumentNullException.ThrowIfNull(action);

            if (System.Windows.Application.Current?.Dispatcher is not { } dispatcher)
            {
                action();
                return;
            }

            try
            {
                if (dispatcher.CheckAccess())
                {
                    action();
                    return;
                }

                dispatcher.Invoke(action);
            }
            catch (TaskCanceledException ex)
            {
                logger.Error(ex, failureContext);
            }
            catch (InvalidOperationException ex)
            {
                logger.Error(ex, failureContext);
            }
        }
    }
}

