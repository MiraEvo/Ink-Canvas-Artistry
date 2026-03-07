using Ink_Canvas.Controllers;
using Ink_Canvas.Helpers;
using System;
using System.Threading.Tasks;
using System.Windows;
using MessageBox = System.Windows.MessageBox;

namespace Ink_Canvas
{
    public partial class MainWindow
    {
        private IAutomationController? automationController;
        private IHotkeyController? hotkeyController;

        private void InitializeAutomationControllers()
        {
            automationController = new AutomationController(
                SettingsViewModel,
                PresentationViewModel,
                WorkspaceSessionViewModel,
                AutomationViewModel,
                () => ShellViewModel.IsFloatingBarTransitioning,
                () => ShellViewModel.IsFloatingBarFolded,
                CanInstallSilentUpdate,
                () => FoldFloatingBar_Click(null, null),
                () => UnFoldFloatingBar_MouseUp(null, null),
                HandleAutoKilledEasiNote,
                version => AutoUpdateHelper.InstallNewVersionApp(version, true));

            hotkeyController = new HotkeyController(
                () => BtnPPTSlideShowEnd_Click(null, null),
                () => SymbolIconDelete_MouseUp(null, null),
                SaveScreenShotToDesktop,
                () => SymbolIconEmoji_MouseUp(null, null),
                () => PenIcon_Click(null, null),
                ExitDrawModeFromHotkey,
                () => ImageBlackboard_Click(null, null));
        }

        private void InitTimers()
        {
            automationController?.Initialize();
        }

        private void RefreshAutoFoldMonitoring()
        {
            automationController?.RefreshAutoFoldMonitoring();
        }

        private void RefreshProcessKillMonitoring()
        {
            automationController?.RefreshProcessKillMonitoring();
        }

        private void ScheduleSilentUpdate(string? version)
        {
            automationController?.ScheduleSilentUpdate(version);
        }

        private void CancelSilentUpdate()
        {
            automationController?.CancelSilentUpdate();
        }

        private bool CanInstallSilentUpdate()
        {
            try
            {
                return Dispatcher.CheckAccess()
                    ? Topmost && inkCanvas.Strokes.Count == 0
                    : Dispatcher.Invoke(() => Topmost && inkCanvas.Strokes.Count == 0);
            }
            catch (TaskCanceledException ex)
            {
                LogHelper.WriteLogToFile(ex, "Automation | Dispatcher call was canceled while checking silent update readiness");
                return false;
            }
            catch (InvalidOperationException ex)
            {
                LogHelper.WriteLogToFile(ex, "Automation | Dispatcher call failed while checking silent update readiness");
                return false;
            }
        }

        private void HandleAutoKilledEasiNote()
        {
            if (ShellViewModel.IsBlackboardMode)
            {
                ExitBlackboardSession();
            }

            MessageBox.Show("“希沃白板 5”已自动关闭");
        }

        private void ExitDrawModeFromHotkey()
        {
            if (ShellViewModel.IsBlackboardMode)
            {
                ExitBlackboardSession();
            }

            CursorIcon_Click(null, null);
        }

        private void DisposeAutomationControllers()
        {
            automationController?.Dispose();
            automationController = null;
            hotkeyController = null;
        }
    }
}
