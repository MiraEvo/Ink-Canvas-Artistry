using Ink_Canvas.Helpers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;

namespace Ink_Canvas.Features.Ink.Hosts
{
    internal interface IInkHistoryHost
    {
        InkCanvas InkCanvas { get; }

        bool IsDesktopAnnotationMode { get; }

        bool IsSelectionCoverDragging { get; }

        bool IsHidingSubPanelsWhenInking { get; set; }

        void HideSubPanels();

        void BackupCurrentStrokes();

        void SetCommitReason(CommitReason reason);

        void ApplyHistoryItem(TimeMachineHistory item);

        void ClearCanvasVisuals();

        void ClearCanvasElements();

        void HideSelectionCover();

        void ClearSelection();

        void UpdateWhiteboardIndexDisplay(int currentIndex, int totalCount);
    }
}

