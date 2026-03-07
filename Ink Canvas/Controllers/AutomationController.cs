using Ink_Canvas.Helpers;
using Ink_Canvas.ViewModels;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using System.Timers;
using Timer = System.Timers.Timer;

namespace Ink_Canvas.Controllers
{
    public sealed class AutomationController : IAutomationController
    {
        private const double AutoFoldIntervalMs = 1500;
        private const double ProcessKillIntervalMs = 5000;
        private const double SilentUpdateIntervalMs = 1000 * 60 * 60;

        private readonly SettingsViewModel settingsViewModel;
        private readonly ShellViewModel shellViewModel;
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
        private readonly Timer autoFoldTimer;
        private readonly Timer processKillTimer;
        private readonly Timer silentUpdateTimer;

        private int isCheckingAutoFold;
        private int isKillingProcesses;
        private int isCheckingSilentUpdate;

        public AutomationController(
            SettingsViewModel settingsViewModel,
            ShellViewModel shellViewModel,
            PresentationSessionViewModel presentationSessionViewModel,
            WorkspaceSessionViewModel workspaceSessionViewModel,
            AutomationStateViewModel automationStateViewModel,
            Func<bool> isFloatingBarTransitioning,
            Func<bool> isFloatingBarFolded,
            Func<bool> canInstallSilentUpdate,
            Action requestFoldFloatingBar,
            Action requestUnfoldFloatingBar,
            Action onAutoKilledEasiNote,
            Action<string> installSilentUpdate)
        {
            this.settingsViewModel = settingsViewModel;
            this.shellViewModel = shellViewModel;
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

            autoFoldTimer = new Timer(AutoFoldIntervalMs);
            autoFoldTimer.Elapsed += AutoFoldTimer_Elapsed;

            processKillTimer = new Timer(ProcessKillIntervalMs);
            processKillTimer.Elapsed += ProcessKillTimer_Elapsed;

            silentUpdateTimer = new Timer(SilentUpdateIntervalMs);
            silentUpdateTimer.Elapsed += SilentUpdateTimer_Elapsed;
        }

        public void Initialize()
        {
            RefreshAutoFoldMonitoring();
            RefreshProcessKillMonitoring();
        }

        public void RefreshAutoFoldMonitoring()
        {
            bool enabled = settingsViewModel.Model?.Automation?.IsEnableAutoFold == true;
            automationStateViewModel.SetAutoFoldMonitoring(enabled);

            if (enabled)
            {
                autoFoldTimer.Start();
            }
            else
            {
                autoFoldTimer.Stop();
                automationStateViewModel.SetFloatingBarFoldRequestedByAutomation(false);
            }
        }

        public void RefreshProcessKillMonitoring()
        {
            bool enabled = settingsViewModel.IsAutoKillEasiNote || settingsViewModel.IsAutoKillPptService;
            automationStateViewModel.SetProcessKillMonitoring(enabled);

            if (enabled)
            {
                processKillTimer.Start();
            }
            else
            {
                processKillTimer.Stop();
            }
        }

        public void ScheduleSilentUpdate(string version)
        {
            if (string.IsNullOrWhiteSpace(version))
            {
                CancelSilentUpdate();
                return;
            }

            automationStateViewModel.SetPendingUpdateVersion(version);
            automationStateViewModel.SetSilentUpdateWaiting(true);
            silentUpdateTimer.Stop();
            silentUpdateTimer.Start();
        }

        public void CancelSilentUpdate()
        {
            silentUpdateTimer.Stop();
            automationStateViewModel.SetSilentUpdateWaiting(false);
            automationStateViewModel.SetPendingUpdateVersion(string.Empty);
        }

        public void Dispose()
        {
            autoFoldTimer.Stop();
            processKillTimer.Stop();
            silentUpdateTimer.Stop();
            autoFoldTimer.Dispose();
            processKillTimer.Dispose();
            silentUpdateTimer.Dispose();
        }

        private void AutoFoldTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (Interlocked.Exchange(ref isCheckingAutoFold, 1) == 1)
            {
                return;
            }

            try
            {
                EvaluateAutoFold();
            }
            finally
            {
                Interlocked.Exchange(ref isCheckingAutoFold, 0);
            }
        }

        private void EvaluateAutoFold()
        {
            if (!automationStateViewModel.IsAutoFoldMonitoring || isFloatingBarTransitioning())
            {
                return;
            }

            string windowProcessName = ForegroundWindowInfo.ProcessName();
            string windowTitle = ForegroundWindowInfo.WindowTitle();
            ForegroundWindowInfo.RECT windowRect = ForegroundWindowInfo.WindowRect();

            automationStateViewModel.SetForegroundProcessName(windowProcessName);
            automationStateViewModel.SetForegroundWindowTitle(windowTitle);

            bool shouldFoldForForegroundWindow =
                settingsViewModel.IsAutoFoldInEasiNote
                && windowProcessName == "EasiNote"
                && (!(windowTitle.Length == 0 && windowRect.Height < 500) || !settingsViewModel.IsAutoFoldInEasiNoteIgnoreDesktopAnno)
                || settingsViewModel.IsAutoFoldInEasiCamera && windowProcessName == "EasiCamera"
                || settingsViewModel.IsAutoFoldInEasiNote3C && windowProcessName == "EasiNote"
                || settingsViewModel.IsAutoFoldInSeewoPincoTeacher && (windowProcessName == "BoardService" || windowProcessName == "seewoPincoTeacher")
                || settingsViewModel.IsAutoFoldInHiteCamera && windowProcessName == "HiteCamera"
                || settingsViewModel.IsAutoFoldInHiteTouchPro && windowProcessName == "HiteTouchPro"
                || settingsViewModel.IsAutoFoldInWxBoardMain && windowProcessName == "WxBoardMain"
                || settingsViewModel.IsAutoFoldInMSWhiteboard && (windowProcessName == "MicrosoftWhiteboard" || windowProcessName == "msedgewebview2")
                || settingsViewModel.IsAutoFoldInOldZyBoard
                && (WinTabWindowsChecker.IsWindowExisted("WhiteBoard - DrawingWindow")
                    || WinTabWindowsChecker.IsWindowExisted("InstantAnnotationWindow"));

            if (shouldFoldForForegroundWindow)
            {
                automationStateViewModel.SetFloatingBarFoldRequestedByAutomation(true);
                if (!automationStateViewModel.IsFloatingBarUnfoldedByUser && !isFloatingBarFolded())
                {
                    RunOnUiThread(requestFoldFloatingBar);
                }
                return;
            }

            automationStateViewModel.SetFloatingBarFoldRequestedByAutomation(false);

            if (presentationSessionViewModel.IsSlideShowRunning)
            {
                if (!settingsViewModel.IsAutoFoldInPPTSlideShow
                    && isFloatingBarFolded()
                    && !automationStateViewModel.IsFloatingBarFoldedByUser)
                {
                    RunOnUiThread(requestUnfoldFloatingBar);
                }

                return;
            }

            if (workspaceSessionViewModel.IsDesktopSession
                && isFloatingBarFolded()
                && !automationStateViewModel.IsFloatingBarFoldedByUser)
            {
                RunOnUiThread(requestUnfoldFloatingBar);
            }

            automationStateViewModel.SetFloatingBarUnfoldedByUser(false);
        }

        private void ProcessKillTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (Interlocked.Exchange(ref isKillingProcesses, 1) == 1)
            {
                return;
            }

            try
            {
                EvaluateProcessKill();
            }
            finally
            {
                Interlocked.Exchange(ref isKillingProcesses, 0);
            }
        }

        private void EvaluateProcessKill()
        {
            if (!automationStateViewModel.IsProcessKillMonitoring)
            {
                return;
            }

            string arguments = "/F";
            bool killedEasiNote = false;

            if (settingsViewModel.IsAutoKillPptService)
            {
                if (Process.GetProcessesByName("PPTService").Length > 0)
                {
                    arguments += " /IM PPTService.exe";
                }

                if (Process.GetProcessesByName("SeewoIwbAssistant").Length > 0)
                {
                    arguments += " /IM SeewoIwbAssistant.exe /IM Sia.Guard.exe";
                }
            }

            if (settingsViewModel.IsAutoKillEasiNote && Process.GetProcessesByName("EasiNote").Length > 0)
            {
                arguments += " /IM EasiNote.exe";
                killedEasiNote = true;
            }

            if (arguments == "/F")
            {
                return;
            }

            try
            {
                using Process process = new Process
                {
                    StartInfo = new ProcessStartInfo("taskkill", arguments)
                    {
                        WindowStyle = ProcessWindowStyle.Hidden
                    }
                };
                process.Start();

                if (killedEasiNote)
                {
                    RunOnUiThread(onAutoKilledEasiNote);
                }
            }
            catch (Win32Exception ex)
            {
                LogHelper.WriteLogToFile(ex, "Automation | Failed to start taskkill");
            }
            catch (InvalidOperationException ex)
            {
                LogHelper.WriteLogToFile(ex, "Automation | Failed to execute process cleanup");
            }
        }

        private void SilentUpdateTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (Interlocked.Exchange(ref isCheckingSilentUpdate, 1) == 1)
            {
                return;
            }

            try
            {
                TryInstallSilentUpdate();
            }
            finally
            {
                Interlocked.Exchange(ref isCheckingSilentUpdate, 0);
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
            RunOnUiThread(() => installSilentUpdate?.Invoke(version));
        }

        private static void RunOnUiThread(Action action)
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
