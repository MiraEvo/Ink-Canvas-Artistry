using System;

namespace Ink_Canvas.Features.Automation
{
    internal sealed class AutomationExperienceCoordinator
    {
        private readonly IAutomationUiHost host;

        public AutomationExperienceCoordinator(IAutomationUiHost host)
        {
            this.host = host ?? throw new ArgumentNullException(nameof(host));
        }

        public bool CanInstallSilentUpdate()
        {
            return host.CanInstallSilentUpdate();
        }

        public void HandleRequestFoldFloatingBar()
        {
            host.RequestFoldFloatingBar();
        }

        public void HandleRequestUnfoldFloatingBar()
        {
            host.RequestUnfoldFloatingBar();
        }

        public void HandleAutoKilledEasiNote()
        {
            if (host.IsBlackboardMode)
            {
                host.ExitBlackboardSession();
            }

            host.ShowAutoKilledEasiNoteMessage();
        }

        public void HandleInstallSilentUpdate(string version)
        {
            host.InstallSilentUpdate(version);
        }

        public void HandleExitPresentation()
        {
            host.EndPresentation();
        }

        public void HandleClearCanvas()
        {
            host.RequestClearCanvas();
        }

        public void HandleCaptureScreen()
        {
            host.CaptureScreenToDesktop();
        }

        public void HandleToggleCanvasVisibility()
        {
            host.ToggleCanvasVisibility();
        }

        public void HandleActivatePen()
        {
            host.ActivatePenTool();
        }

        public void HandleExitDrawMode()
        {
            if (host.IsBlackboardMode)
            {
                host.ExitBlackboardSession();
            }

            host.ActivateCursorTool();
        }

        public void HandleToggleBlackboard()
        {
            host.ToggleBlackboard();
        }
    }
}
