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
                appLogger,
                errorHandler,
                uiDispatchGuard);

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
            return uiDispatchGuard.Invoke(
                () => Topmost && inkCanvas.Strokes.Count == 0,
                false,
                new AppErrorContext(nameof(MainWindow), "CanInstallSilentUpdate"));
        }

        void IAutomationUiHost.RequestFoldFloatingBar() =>
            taskGuard.Forget(
                toolbarExperienceCoordinator.HandleFoldFloatingBarAsync(false),
                new AppErrorContext(nameof(MainWindow), "RequestFoldFloatingBar"));

        void IAutomationUiHost.RequestUnfoldFloatingBar() =>
            taskGuard.Forget(
                toolbarExperienceCoordinator.HandleUnfoldFloatingBarAsync(false),
                new AppErrorContext(nameof(MainWindow), "RequestUnfoldFloatingBar"));

        void IAutomationUiHost.EndPresentation() => BtnPPTSlideShowEnd_Click(null, null);

        void IAutomationUiHost.RequestClearCanvas() => toolbarExperienceCoordinator.HandleDeleteRequested();

        void IAutomationUiHost.CaptureScreenToDesktop() => SaveScreenShotToDesktop();

        void IAutomationUiHost.ToggleCanvasVisibility() => toolbarExperienceCoordinator.HandleHideInkCanvasRequested();

        void IAutomationUiHost.ActivatePenTool() => toolbarExperienceCoordinator.HandlePenRequested();

        void IAutomationUiHost.ActivateCursorTool() =>
            taskGuard.Forget(
                toolbarExperienceCoordinator.HandleCursorRequestedAsync(),
                new AppErrorContext(nameof(MainWindow), "ActivateCursorTool"));

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
