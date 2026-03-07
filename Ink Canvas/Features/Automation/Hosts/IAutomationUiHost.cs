namespace Ink_Canvas.Features.Automation.Hosts
{
    internal interface IAutomationUiHost
    {
        bool IsBlackboardMode { get; }

        bool CanInstallSilentUpdate();

        void RequestFoldFloatingBar();

        void RequestUnfoldFloatingBar();

        void EndPresentation();

        void RequestClearCanvas();

        void CaptureScreenToDesktop();

        void ToggleCanvasVisibility();

        void ActivatePenTool();

        void ActivateCursorTool();

        void ToggleBlackboard();

        void ExitBlackboardSession();

        void ShowAutoKilledEasiNoteMessage();

        void InstallSilentUpdate(string version);
    }
}

