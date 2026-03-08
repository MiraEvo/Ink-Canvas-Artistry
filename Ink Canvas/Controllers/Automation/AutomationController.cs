using Ink_Canvas.Services.Logging;
using Ink_Canvas.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Timers;
using Timer = System.Timers.Timer;

namespace Ink_Canvas.Controllers.Automation
{
    public sealed class AutomationController : IAutomationController
    {
        private const double AutoFoldIntervalMs = 1500;
        private const double ProcessKillIntervalMs = 5000;
        private const double SilentUpdateIntervalMs = 1000 * 60 * 60;

        private readonly SettingsViewModel settingsViewModel;
        private readonly PresentationSessionViewModel presentationSessionViewModel;
        private readonly WorkspaceSessionViewModel workspaceSessionViewModel;
        private readonly AutomationStateViewModel automationStateViewModel;
        private readonly Func<bool> isFloatingBarTransitioning;
        private readonly Func<bool> isFloatingBarFolded;
        private readonly Func<bool> canInstallSilentUpdate;
        private readonly Action requestFoldFloatingBar;
        private readonly Action requestUnfoldFloatingBar;
        private readonly Action onAutoKilledEasiNote;
        private readonly Action<string> installSilentUpdate;
        private readonly IAppLogger logger;
        private readonly Timer autoFoldTimer;
        private readonly Timer processKillTimer;
        private readonly Timer silentUpdateTimer;

        private int isCheckingAutoFold;
        private int isKillingProcesses;
        private int isCheckingSilentUpdate;

        public AutomationController(
            SettingsViewModel settingsViewModel,
            PresentationSessionViewModel presentationSessionViewModel,
            WorkspaceSessionViewModel workspaceSessionViewModel,
            AutomationStateViewModel automationStateViewModel,
            Func<bool> isFloatingBarTransitioning,
            Func<bool> isFloatingBarFolded,
            Func<bool> canInstallSilentUpdate,
            Action requestFoldFloatingBar,
            Action requestUnfoldFloatingBar,
            Action onAutoKilledEasiNote,
            Action<string> installSilentUpdate,
            IAppLogger logger)
        {
            ArgumentNullException.ThrowIfNull(settingsViewModel);
            ArgumentNullException.ThrowIfNull(presentationSessionViewModel);
            ArgumentNullException.ThrowIfNull(workspaceSessionViewModel);
            ArgumentNullException.ThrowIfNull(automationStateViewModel);
            ArgumentNullException.ThrowIfNull(isFloatingBarTransitioning);
            ArgumentNullException.ThrowIfNull(isFloatingBarFolded);
            ArgumentNullException.ThrowIfNull(canInstallSilentUpdate);
            ArgumentNullException.ThrowIfNull(requestFoldFloatingBar);
            ArgumentNullException.ThrowIfNull(requestUnfoldFloatingBar);
            ArgumentNullException.ThrowIfNull(onAutoKilledEasiNote);
            ArgumentNullException.ThrowIfNull(installSilentUpdate);
            ArgumentNullException.ThrowIfNull(logger);

            this.settingsViewModel = settingsViewModel;
            this.presentationSessionViewModel = presentationSessionViewModel;
            this.workspaceSessionViewModel = workspaceSessionViewModel;
            this.automationStateViewModel = automationStateViewModel;
            this.isFloatingBarTransitioning = isFloatingBarTransitioning;
            this.isFloatingBarFolded = isFloatingBarFolded;
            this.canInstallSilentUpdate = canInstallSilentUpdate;
            this.requestFoldFloatingBar = requestFoldFloatingBar;
            this.requestUnfoldFloatingBar = requestUnfoldFloatingBar;
            this.onAutoKilledEasiNote = onAutoKilledEasiNote;
            this.installSilentUpdate = installSilentUpdate;
            this.logger = logger.ForCategory(nameof(AutomationController));

            autoFoldTimer = CreateTimer(AutoFoldIntervalMs, AutoFoldTimer_Elapsed);
            processKillTimer = CreateTimer(ProcessKillIntervalMs, ProcessKillTimer_Elapsed);
            silentUpdateTimer = CreateTimer(SilentUpdateIntervalMs, SilentUpdateTimer_Elapsed);
        }

        public void Initialize()
        {
            RefreshAutoFoldMonitoring();
            RefreshProcessKillMonitoring();
        }

        public void RefreshAutoFoldMonitoring()
        {
            bool enabled = settingsViewModel.Model.Automation.IsEnableAutoFold;
            automationStateViewModel.SetAutoFoldMonitoring(enabled);
            StartOrStopTimer(autoFoldTimer, enabled);

            if (!enabled)
            {
                automationStateViewModel.SetFloatingBarFoldRequestedByAutomation(false);
            }
        }

        public void RefreshProcessKillMonitoring()
        {
            bool enabled = settingsViewModel.IsAutoKillEasiNote || settingsViewModel.IsAutoKillPptService;
            automationStateViewModel.SetProcessKillMonitoring(enabled);
            StartOrStopTimer(processKillTimer, enabled);
        }

        public void ScheduleSilentUpdate(string? version)
        {
            if (string.IsNullOrWhiteSpace(version))
            {
                CancelSilentUpdate();
                return;
            }

            automationStateViewModel.SetPendingUpdateVersion(version);
            automationStateViewModel.SetSilentUpdateWaiting(true);
            RestartTimer(silentUpdateTimer);
        }

        public void CancelSilentUpdate()
        {
            silentUpdateTimer.Stop();
            automationStateViewModel.SetSilentUpdateWaiting(false);
            automationStateViewModel.SetPendingUpdateVersion(string.Empty);
        }

        public void Dispose()
        {
            DisposeTimer(autoFoldTimer);
            DisposeTimer(processKillTimer);
            DisposeTimer(silentUpdateTimer);
        }

        private void AutoFoldTimer_Elapsed(object? sender, ElapsedEventArgs e) => RunGuarded(ref isCheckingAutoFold, EvaluateAutoFold);

        private void ProcessKillTimer_Elapsed(object? sender, ElapsedEventArgs e) => RunGuarded(ref isKillingProcesses, EvaluateProcessKill);

        private void SilentUpdateTimer_Elapsed(object? sender, ElapsedEventArgs e) => RunGuarded(ref isCheckingSilentUpdate, TryInstallSilentUpdate);

        private void EvaluateAutoFold()
        {
            if (!automationStateViewModel.IsAutoFoldMonitoring || isFloatingBarTransitioning())
            {
                return;
            }

            ForegroundWindowState foregroundWindow = ReadForegroundWindowState();
            UpdateForegroundWindowState(foregroundWindow);

            if (ShouldFoldForForegroundWindow(foregroundWindow))
            {
                automationStateViewModel.SetFloatingBarFoldRequestedByAutomation(true);
                TryRequestFloatingBarTransition(
                    !automationStateViewModel.IsFloatingBarUnfoldedByUser && !isFloatingBarFolded(),
                    requestFoldFloatingBar,
                    "Automation | Failed to fold floating bar on the UI thread");
                return;
            }

            automationStateViewModel.SetFloatingBarFoldRequestedByAutomation(false);

            if (presentationSessionViewModel.IsSlideShowRunning)
            {
                bool shouldUnfoldDuringSlideShow =
                    !settingsViewModel.IsAutoFoldInPPTSlideShow
                    && isFloatingBarFolded()
                    && !automationStateViewModel.IsFloatingBarFoldedByUser;

                TryRequestFloatingBarTransition(
                    shouldUnfoldDuringSlideShow,
                    requestUnfoldFloatingBar,
                    "Automation | Failed to unfold floating bar during slideshow");
                return;
            }

            bool shouldUnfoldOnDesktop =
                workspaceSessionViewModel.IsDesktopSession
                && isFloatingBarFolded()
                && !automationStateViewModel.IsFloatingBarFoldedByUser;

            TryRequestFloatingBarTransition(
                shouldUnfoldOnDesktop,
                requestUnfoldFloatingBar,
                "Automation | Failed to unfold floating bar on desktop");

            automationStateViewModel.SetFloatingBarUnfoldedByUser(false);
        }

        private void EvaluateProcessKill()
        {
            if (!automationStateViewModel.IsProcessKillMonitoring)
            {
                return;
            }

            List<string> imageNames = [];
            bool killedEasiNote = false;

            if (settingsViewModel.IsAutoKillPptService)
            {
                AddProcessImageIfRunning(imageNames, "PPTService");

                if (IsProcessRunning("SeewoIwbAssistant"))
                {
                    imageNames.Add("SeewoIwbAssistant.exe");
                    imageNames.Add("Sia.Guard.exe");
                }
            }

            if (settingsViewModel.IsAutoKillEasiNote && IsProcessRunning("EasiNote"))
            {
                imageNames.Add("EasiNote.exe");
                killedEasiNote = true;
            }

            if (imageNames.Count == 0)
            {
                return;
            }

            try
            {
                using Process process = new()
                {
                    StartInfo = new ProcessStartInfo("taskkill", BuildTaskKillArguments(imageNames))
                    {
                        CreateNoWindow = true,
                        UseShellExecute = false,
                        WindowStyle = ProcessWindowStyle.Hidden
                    }
                };
                process.Start();

                if (killedEasiNote)
                {
                    RunOnUiThread(onAutoKilledEasiNote, "Automation | Failed to notify after auto-killing EasiNote", logger);
                }
            }
            catch (Win32Exception ex)
            {
                logger.Error(ex, "Automation | Failed to start taskkill");
            }
            catch (InvalidOperationException ex)
            {
                logger.Error(ex, "Automation | Failed to execute process cleanup");
            }
        }

        private void TryInstallSilentUpdate()
        {
            if (!automationStateViewModel.IsSilentUpdateWaiting
                || string.IsNullOrWhiteSpace(automationStateViewModel.PendingUpdateVersion))
            {
                return;
            }

            if (!canInstallSilentUpdate())
            {
                return;
            }

            if (!AutoUpdateWithSilenceTimeComboBox.CheckIsInSilencePeriod(
                    settingsViewModel.AutoUpdateWithSilenceStartTime,
                    settingsViewModel.AutoUpdateWithSilenceEndTime))
            {
                return;
            }

            string version = automationStateViewModel.PendingUpdateVersion;
            CancelSilentUpdate();
            RunOnUiThread(() => installSilentUpdate(version), "Automation | Failed to install silent update on the UI thread", logger);
        }

        private bool ShouldFoldForForegroundWindow(ForegroundWindowState foregroundWindow)
        {
            return ShouldFoldInEasiNote(foregroundWindow)
                || settingsViewModel.IsAutoFoldInEasiCamera && foregroundWindow.ProcessName == "EasiCamera"
                || settingsViewModel.IsAutoFoldInEasiNote3C && foregroundWindow.ProcessName == "EasiNote"
                || settingsViewModel.IsAutoFoldInSeewoPincoTeacher
                    && (foregroundWindow.ProcessName == "BoardService" || foregroundWindow.ProcessName == "seewoPincoTeacher")
                || settingsViewModel.IsAutoFoldInHiteCamera && foregroundWindow.ProcessName == "HiteCamera"
                || settingsViewModel.IsAutoFoldInHiteTouchPro && foregroundWindow.ProcessName == "HiteTouchPro"
                || settingsViewModel.IsAutoFoldInWxBoardMain && foregroundWindow.ProcessName == "WxBoardMain"
                || settingsViewModel.IsAutoFoldInMSWhiteboard
                    && (foregroundWindow.ProcessName == "MicrosoftWhiteboard" || foregroundWindow.ProcessName == "msedgewebview2")
                || settingsViewModel.IsAutoFoldInOldZyBoard
                    && (WinTabWindowsChecker.IsWindowExisted("WhiteBoard - DrawingWindow")
                        || WinTabWindowsChecker.IsWindowExisted("InstantAnnotationWindow"));
        }

        private bool ShouldFoldInEasiNote(ForegroundWindowState foregroundWindow)
        {
            return settingsViewModel.IsAutoFoldInEasiNote
                && foregroundWindow.ProcessName == "EasiNote"
                && (!settingsViewModel.IsAutoFoldInEasiNoteIgnoreDesktopAnno || !IsCompactDesktopAnnotationWindow(foregroundWindow));
        }

        private static bool IsCompactDesktopAnnotationWindow(ForegroundWindowState foregroundWindow) =>
            string.IsNullOrEmpty(foregroundWindow.WindowTitle) && foregroundWindow.WindowRect.Height < 500;

        private void UpdateForegroundWindowState(ForegroundWindowState foregroundWindow)
        {
            automationStateViewModel.SetForegroundProcessName(foregroundWindow.ProcessName);
            automationStateViewModel.SetForegroundWindowTitle(foregroundWindow.WindowTitle);
        }

        private static ForegroundWindowState ReadForegroundWindowState() =>
            new(
                ForegroundWindowInfo.ProcessName(),
                ForegroundWindowInfo.WindowTitle(),
                ForegroundWindowInfo.WindowRect());

        private static Timer CreateTimer(double interval, ElapsedEventHandler handler)
        {
            Timer timer = new(interval)
            {
                AutoReset = true
            };
            timer.Elapsed += handler;
            return timer;
        }

        private static void RunGuarded(ref int guardFlag, Action action)
        {
            if (Interlocked.Exchange(ref guardFlag, 1) == 1)
            {
                return;
            }

            try
            {
                action();
            }
            finally
            {
                Interlocked.Exchange(ref guardFlag, 0);
            }
        }

        private static void StartOrStopTimer(Timer timer, bool enabled)
        {
            if (enabled)
            {
                timer.Start();
            }
            else
            {
                timer.Stop();
            }
        }

        private static void RestartTimer(Timer timer)
        {
            timer.Stop();
            timer.Start();
        }

        private static void DisposeTimer(Timer timer)
        {
            timer.Stop();
            timer.Dispose();
        }

        private void TryRequestFloatingBarTransition(bool shouldRun, Action action, string failureContext)
        {
            if (!shouldRun)
            {
                return;
            }

            RunOnUiThread(action, failureContext, logger);
        }

        private static string BuildTaskKillArguments(IEnumerable<string> imageNames) =>
            $"/F {string.Join(" ", imageNames.Select(imageName => $"/IM {imageName}"))}";

        private static void AddProcessImageIfRunning(ICollection<string> imageNames, string processName)
        {
            if (IsProcessRunning(processName))
            {
                imageNames.Add($"{processName}.exe");
            }
        }

        private static bool IsProcessRunning(string processName) => Process.GetProcessesByName(processName).Length > 0;

        private static void RunOnUiThread(Action action, string failureContext, IAppLogger logger)
        {
            ArgumentNullException.ThrowIfNull(action);
            ArgumentNullException.ThrowIfNull(logger);

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

        private readonly record struct ForegroundWindowState(
            string ProcessName,
            string WindowTitle,
            ForegroundWindowInfo.RECT WindowRect);
    }
}

