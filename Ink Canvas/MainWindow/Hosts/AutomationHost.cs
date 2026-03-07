using Ink_Canvas.Controllers;
using Ink_Canvas.Features.Automation;
using System;
using System.Windows;
using MessageBox = System.Windows.MessageBox;

namespace Ink_Canvas
{
    public partial class MainWindow : IAutomationUiHost
    {
        private IAutomationController? automationController;
        private IHotkeyController? hotkeyController;
        private AutomationExperienceCoordinator automationExperienceCoordinator = null!;

        private void InitializeAutomationControllers()
        {
            automationExperienceCoordinator = new AutomationExperienceCoordinator(this);
            automationController = new AutomationController(
                SettingsViewModel,
                PresentationViewModel,
                WorkspaceSessionViewModel,
                AutomationViewModel,
                () => ShellViewModel.IsFloatingBarTransitioning,
                () => ShellViewModel.IsFloatingBarFolded,
                automationExperienceCoordinator.CanInstallSilentUpdate,
                automationExperienceCoordinator.HandleRequestFoldFloatingBar,
                automationExperienceCoordinator.HandleRequestUnfoldFloatingBar,
                automationExperienceCoordinator.HandleAutoKilledEasiNote,
                automationExperienceCoordinator.HandleInstallSilentUpdate,
                appLogger);

            hotkeyController = new HotkeyController(
                automationExperienceCoordinator.HandleExitPresentation,
                automationExperienceCoordinator.HandleClearCanvas,
                automationExperienceCoordinator.HandleCaptureScreen,
                automationExperienceCoordinator.HandleToggleCanvasVisibility,
                automationExperienceCoordinator.HandleActivatePen,
                automationExperienceCoordinator.HandleExitDrawMode,
                automationExperienceCoordinator.HandleToggleBlackboard);
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

        bool IAutomationUiHost.IsBlackboardMode => ShellViewModel.IsBlackboardMode;

        bool IAutomationUiHost.CanInstallSilentUpdate()
        {
            try
            {
                return Dispatcher.CheckAccess()
                    ? Topmost && inkCanvas.Strokes.Count == 0
                    : Dispatcher.Invoke(() => Topmost && inkCanvas.Strokes.Count == 0);
            }
            catch (TaskCanceledException ex)
            {
                mainWindowLogger.Error(ex, "Automation | Dispatcher call was canceled while checking silent update readiness");
                return false;
            }
            catch (InvalidOperationException ex)
            {
                mainWindowLogger.Error(ex, "Automation | Dispatcher call failed while checking silent update readiness");
                return false;
            }
        }

        void IAutomationUiHost.RequestFoldFloatingBar() => _ = toolbarExperienceCoordinator.HandleFoldFloatingBarAsync(false);

        void IAutomationUiHost.RequestUnfoldFloatingBar() => _ = toolbarExperienceCoordinator.HandleUnfoldFloatingBarAsync(false);

        void IAutomationUiHost.EndPresentation() => BtnPPTSlideShowEnd_Click(null, null);

        void IAutomationUiHost.RequestClearCanvas() => toolbarExperienceCoordinator.HandleDeleteRequested();

        void IAutomationUiHost.CaptureScreenToDesktop() => SaveScreenShotToDesktop();

        void IAutomationUiHost.ToggleCanvasVisibility() => toolbarExperienceCoordinator.HandleHideInkCanvasRequested();

        void IAutomationUiHost.ActivatePenTool() => toolbarExperienceCoordinator.HandlePenRequested();

        void IAutomationUiHost.ActivateCursorTool() => _ = toolbarExperienceCoordinator.HandleCursorRequestedAsync();

        void IAutomationUiHost.ToggleBlackboard() => toolbarExperienceCoordinator.HandleToggleBlackboardRequested();

        void IAutomationUiHost.ExitBlackboardSession() => ExitBlackboardSession();

        void IAutomationUiHost.ShowAutoKilledEasiNoteMessage() => MessageBox.Show("“希沃白板 5”已自动关闭");

        void IAutomationUiHost.InstallSilentUpdate(string version) => autoUpdateHelper.InstallNewVersionApp(version, true);

        private void DisposeAutomationControllers()
        {
            automationController?.Dispose();
            automationController = null;
            hotkeyController = null;
        }
    }
}
