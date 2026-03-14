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
        private readonly AppErrorHandler errorHandler;
        private readonly TaskGuard taskGuard;
        private readonly UiDispatchGuard uiDispatchGuard;
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
            IAppLogger logger,
            AppErrorHandler errorHandler,
            TaskGuard taskGuard,
            UiDispatchGuard uiDispatchGuard)
        {
            ArgumentNullException.ThrowIfNull(presentationSessionViewModel);
            ArgumentNullException.ThrowIfNull(isWpsSupportEnabled);
            ArgumentNullException.ThrowIfNull(shouldSkipDetection);
            ArgumentNullException.ThrowIfNull(logger);
            ArgumentNullException.ThrowIfNull(errorHandler);
            ArgumentNullException.ThrowIfNull(taskGuard);
            ArgumentNullException.ThrowIfNull(uiDispatchGuard);

            this.presentationSessionViewModel = presentationSessionViewModel;
            this.isWpsSupportEnabled = isWpsSupportEnabled;
            this.shouldSkipDetection = shouldSkipDetection;
            this.logger = logger.ForCategory(nameof(PresentationSessionController));
            this.errorHandler = errorHandler;
            this.taskGuard = taskGuard;
            this.uiDispatchGuard = uiDispatchGuard;
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
            TryExecuteUserPresentationAction(
                "GoToSlide",
                () => boundApplicationObject != null && dynamicPresentationAccessor.TryGoToSlide(boundApplicationObject, slideNumber));

        public bool TryGoToPreviousSlide() =>
            TryExecuteUserPresentationAction(
                "GoToPreviousSlide",
                () => boundApplicationObject != null && dynamicPresentationAccessor.TryGoToPreviousSlide(boundApplicationObject));

        public bool TryGoToNextSlide() =>
            TryExecuteUserPresentationAction(
                "GoToNextSlide",
                () => boundApplicationObject != null && dynamicPresentationAccessor.TryGoToNextSlide(boundApplicationObject));

        public bool TryExitSlideShow() =>
            TryExecuteUserPresentationAction(
                "ExitSlideShow",
                () => boundApplicationObject != null && dynamicPresentationAccessor.TryExitSlideShow(boundApplicationObject));

        public bool TryShowSlideNavigation() =>
            TryExecuteUserPresentationAction(
                "ShowSlideNavigation",
                () => boundApplicationObject != null && dynamicPresentationAccessor.TryShowSlideNavigation(boundApplicationObject));

        public bool HasHiddenSlides() =>
            boundApplicationObject != null && dynamicPresentationAccessor.HasHiddenSlides(boundApplicationObject);

        public bool TryUnhideHiddenSlides() =>
            TryExecuteUserPresentationAction(
                "UnhideHiddenSlides",
                () => boundApplicationObject != null && dynamicPresentationAccessor.TryUnhideHiddenSlides(boundApplicationObject));

        public bool HasAutomaticAdvance() =>
            boundApplicationObject != null && dynamicPresentationAccessor.HasAutomaticAdvance(boundApplicationObject);

        public bool TryDisableAutomaticAdvance() =>
            TryExecuteUserPresentationAction(
                "DisableAutomaticAdvance",
                () => boundApplicationObject != null && dynamicPresentationAccessor.TryDisableAutomaticAdvance(boundApplicationObject));

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

            taskGuard.Forget(
                Task.Run(ProcessDetectionQueue),
                new AppErrorContext(nameof(PresentationSessionController), "ProcessDetectionQueue")
                {
                    AllowRateLimit = true
                });
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
                errorHandler.Handle(ex, CreateDetectionErrorContext("TryMonitorSession"));
            }
            catch (InvalidOperationException ex)
            {
                errorHandler.Handle(ex, CreateDetectionErrorContext("TryMonitorSession"));
            }
            catch (ArgumentException ex)
            {
                errorHandler.Handle(ex, CreateDetectionErrorContext("TryMonitorSession"));
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

            uiDispatchGuard.TryInvoke(() =>
            {
                if (!IsMonitoringEnabled() && newState.IsConnected)
                {
                    return;
                }

                ApplyViewModelState(newState);
                PublishTransitionEvents(previousState, newState);
            }, new AppErrorContext(nameof(PresentationSessionController), "PublishState")
            {
                AllowRateLimit = true
            });
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

        private bool TryExecuteUserPresentationAction(string operation, Func<bool> action)
        {
            ArgumentNullException.ThrowIfNull(action);

            try
            {
                bool succeeded = action();
                if (!succeeded)
                {
                    logger.Error($"Presentation Session | {operation} failed.");
                }

                return succeeded;
            }
            catch (COMException ex)
            {
                errorHandler.Handle(ex, new AppErrorContext(nameof(PresentationSessionController), operation));
            }
            catch (InvalidOperationException ex)
            {
                errorHandler.Handle(ex, new AppErrorContext(nameof(PresentationSessionController), operation));
            }
            catch (ArgumentException ex)
            {
                errorHandler.Handle(ex, new AppErrorContext(nameof(PresentationSessionController), operation));
            }

            return false;
        }

        private static AppErrorContext CreateDetectionErrorContext(string operation) =>
            new(nameof(PresentationSessionController), operation)
            {
                AllowRateLimit = true
            };
    }
}

