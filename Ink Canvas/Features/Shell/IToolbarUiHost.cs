using System;
using System.Threading.Tasks;
using System.Windows;

namespace Ink_Canvas.Features.Shell
{
    internal interface IToolbarUiHost
    {
        bool IsPresentationSlideShowRunning { get; }

        bool IsCanvasControlsVisible { get; }

        Task AnimateFloatingBarMarginAfterDelayAsync(TimeSpan delay);

        void HideSubPanels(string? mode = null, bool autoAlignCenter = false);

        void HideSubPanelsImmediately();

        void BeginFloatingBarDrag(Point pointerPosition);

        void UpdateFloatingBarDrag(Point pointerPosition);

        void EndFloatingBarDrag(Point? pointerPosition);

        Task FoldFloatingBarAsync(bool userInitiated);

        Task UnfoldFloatingBarAsync(bool userInitiated);

        void DeleteSelectionOrClear();

        void UndoHistory();

        void RedoHistory();

        void ClearCanvas();

        void ToggleBlackboardSession();

        void ToggleInkCanvasVisibility();

        void ToggleColorTheme();

        void ToggleSingleFingerDragMode();

        void ApplyCurrentWorkspaceVisualState();

        void ExitBlackboardSession();
    }
}
