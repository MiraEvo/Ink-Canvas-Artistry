using System;
using System.Threading.Tasks;
using Ink_Canvas.ViewModels;

namespace Ink_Canvas.Features.Shell
{
    internal interface IShellUiHost
    {
        bool IsCanvasWritingVisible { get; }

        bool IsPresentationSlideShowRunning { get; }

        bool IsBlackboardMode { get; }

        bool IsFloatingBarFolded { get; }

        bool IsSelectionEditingMode { get; }

        bool IsPenToolActive { get; }

        int CurrentInkStrokeCount { get; }

        Task AnimateFloatingBarMarginAfterDelayAsync(TimeSpan delay);

        void AnimateFloatingBarMargin();

        void HidePresentationNavigation();

        void ShowPresentationNavigationIfEnabled();

        void HideSubPanelsImmediately();

        void HideSubPanels(string? mode = null, bool autoAlignCenter = false);

        void ApplySubPanelState(SubPanelKind panel);

        void ApplyWorkspaceVisualState(WorkspaceMode workspaceMode);

        void ApplyCursorToolModeVisuals();

        void ApplyPenToolModeVisuals();

        void ApplyPointEraserToolModeVisuals(double eraserDiameter);

        void ApplyStrokeEraserToolModeVisuals();

        void ApplySelectionToolModeVisuals();

        void CompleteBlackboardTransition();

        void ApplyShellThemeRefresh();

        void SaveScreenshotForCurrentContext();

        void RequestDefaultDesktopFloatingBarPosition();
    }
}
