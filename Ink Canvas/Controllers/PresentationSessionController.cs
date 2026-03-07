using Ink_Canvas.Helpers;
using Ink_Canvas.ViewModels;
using Microsoft.Office.Interop.PowerPoint;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Timers;
namespace Ink_Canvas.Controllers
{
    public sealed class PresentationSessionController : IPresentationSessionController
    {
        private const double DisconnectedMonitorIntervalMs = 250;

        private readonly PresentationSessionViewModel presentationSessionViewModel;
        private readonly Func<bool> isWpsSupportEnabled;
        private readonly Func<bool> shouldSkipDetection;
        private readonly Timer monitorTimer;
        private int isDetecting;

        public PresentationSessionController(
            PresentationSessionViewModel presentationSessionViewModel,
            Func<bool> isWpsSupportEnabled,
            Func<bool> shouldSkipDetection)
        {
            this.presentationSessionViewModel = presentationSessionViewModel;
            this.isWpsSupportEnabled = isWpsSupportEnabled;
            this.shouldSkipDetection = shouldSkipDetection;

            monitorTimer = new Timer(DisconnectedMonitorIntervalMs);
            monitorTimer.Elapsed += MonitorTimer_Elapsed;
        }

        public event Action<Presentation> PresentationConnected;

        public event Action<Presentation> PresentationClosed;

        public event Action<SlideShowWindow> SlideShowBegin;

        public event Action<SlideShowWindow> SlideShowNextSlide;

        public event Action<Presentation> SlideShowEnd;

        public Microsoft.Office.Interop.PowerPoint.Application PowerPointApplication { get; private set; }

        public Presentation Presentation { get; private set; }

        public Slides Slides { get; private set; }

        public Slide Slide { get; private set; }

        public void StartMonitoring()
        {
            monitorTimer.Start();
            QueueImmediateDetection();
        }

        public void StopMonitoring()
        {
            monitorTimer.Stop();
            Disconnect();
        }

        private void MonitorTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            TryConnect();
        }

        private void QueueImmediateDetection()
        {
            _ = Task.Run(() => TryConnect());
        }

        private void TryConnect()
        {
            if (shouldSkipDetection())
            {
                return;
            }

            if (System.Threading.Interlocked.Exchange(ref isDetecting, 1) == 1)
            {
                return;
            }

            try
            {
                if (!isWpsSupportEnabled() && Process.GetProcessesByName("wpp").Length > 0)
                {
                    return;
                }

                Microsoft.Office.Interop.PowerPoint.Application application =
                    ComInteropHelper.GetActiveObject<Microsoft.Office.Interop.PowerPoint.Application>("PowerPoint.Application");
                if (application == null)
                {
                    return;
                }

                Connect(application);
            }
            catch
            {
                ClearConnectionState();
                monitorTimer.Start();
            }
            finally
            {
                System.Threading.Interlocked.Exchange(ref isDetecting, 0);
            }
        }

        private void Connect(Microsoft.Office.Interop.PowerPoint.Application application)
        {
            if (PowerPointApplication != null)
            {
                return;
            }

            Presentation presentation = application.ActivePresentation;
            if (presentation == null)
            {
                monitorTimer.Start();
                return;
            }

            Slides slides = presentation.Slides;
            Slide slide;

            try
            {
                slide = GetCurrentSlide(application, slides);
            }
            catch
            {
                slide = null;
            }

            monitorTimer.Stop();

            PowerPointApplication = application;
            Presentation = presentation;
            Slides = slides;
            Slide = slide;

            application.PresentationClose += OnPresentationClose;
            application.SlideShowBegin += OnSlideShowBegin;
            application.SlideShowNextSlide += OnSlideShowNextSlide;
            application.SlideShowEnd += OnSlideShowEnd;

            PresentationProvider provider = DetectProvider();
            RunOnUiThread(() =>
            {
                presentationSessionViewModel.SetConnection(
                    provider,
                    presentation.Name,
                    slides?.Count ?? 0,
                    slide?.SlideNumber ?? 0,
                    application.SlideShowWindows.Count >= 1);

                PresentationConnected?.Invoke(presentation);
            });

            if (application.SlideShowWindows.Count >= 1)
            {
                OnSlideShowBegin(application.SlideShowWindows[1]);
            }
        }

        private void Disconnect()
        {
            if (PowerPointApplication != null)
            {
                try
                {
                    PowerPointApplication.PresentationClose -= OnPresentationClose;
                    PowerPointApplication.SlideShowBegin -= OnSlideShowBegin;
                    PowerPointApplication.SlideShowNextSlide -= OnSlideShowNextSlide;
                    PowerPointApplication.SlideShowEnd -= OnSlideShowEnd;
                }
                catch
                {
                }
            }

            ClearConnectionState();
            RunOnUiThread(() => presentationSessionViewModel.Disconnect());
        }

        private void ClearConnectionState()
        {
            PowerPointApplication = null;
            Presentation = null;
            Slides = null;
            Slide = null;
        }

        private void OnPresentationClose(Presentation presentation)
        {
            Disconnect();
            RunOnUiThread(() => PresentationClosed?.Invoke(presentation));
            monitorTimer.Start();
            QueueImmediateDetection();
        }

        private void OnSlideShowBegin(SlideShowWindow window)
        {
            Presentation = window.Presentation;
            Slides = window.Presentation?.Slides;
            Slide = window.View?.Slide;

            RunOnUiThread(() =>
            {
                presentationSessionViewModel.SetConnection(
                    DetectProvider(),
                    window.Presentation?.Name,
                    window.Presentation?.Slides?.Count ?? 0,
                    window.View?.CurrentShowPosition ?? 0,
                    true);

                SlideShowBegin?.Invoke(window);
            });
        }

        private void OnSlideShowNextSlide(SlideShowWindow window)
        {
            Presentation = window.Presentation;
            Slides = window.Presentation?.Slides;
            Slide = window.View?.Slide;

            RunOnUiThread(() =>
            {
                presentationSessionViewModel.SetCurrentSlide(
                    window.View?.CurrentShowPosition ?? 0,
                    window.Presentation?.Slides?.Count ?? 0);

                SlideShowNextSlide?.Invoke(window);
            });
        }

        private void OnSlideShowEnd(Presentation presentation)
        {
            Presentation = presentation;
            Slides = presentation?.Slides;
            Slide = null;

            RunOnUiThread(() =>
            {
                presentationSessionViewModel.SetConnection(
                    DetectProvider(),
                    presentation?.Name,
                    presentation?.Slides?.Count ?? 0,
                    0,
                    false);
                presentationSessionViewModel.SetNavigationVisibility(false, false);

                SlideShowEnd?.Invoke(presentation);
            });
        }

        private PresentationProvider DetectProvider()
        {
            return Process.GetProcessesByName("wpp").Length > 0
                ? PresentationProvider.Wps
                : PresentationProvider.PowerPoint;
        }

        private static Slide GetCurrentSlide(Microsoft.Office.Interop.PowerPoint.Application application, Slides slides)
        {
            try
            {
                return slides?[application.ActiveWindow.Selection.SlideRange.SlideNumber];
            }
            catch
            {
                return application.SlideShowWindows.Count >= 1
                    ? application.SlideShowWindows[1].View.Slide
                    : null;
            }
        }

        private static void RunOnUiThread(Action action)
        {
            if (action == null)
            {
                return;
            }

            if (System.Windows.Application.Current?.Dispatcher == null || System.Windows.Application.Current.Dispatcher.CheckAccess())
            {
                action();
                return;
            }

            System.Windows.Application.Current.Dispatcher.Invoke(action);
        }
    }
}
