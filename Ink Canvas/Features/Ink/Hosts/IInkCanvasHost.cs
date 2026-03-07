using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using Ink_Canvas.ViewModels;

namespace Ink_Canvas.Features.Ink.Hosts
{
    internal interface IInkCanvasHost
    {
        InkCanvas InkCanvas { get; }

        Ink_Canvas.Settings Settings { get; }

        bool IsDesktopAnnotationMode { get; }

        bool IsInMultiTouchMode { get; }

        int InkColor { get; }

        bool IsShapePanelAutoHideEnabled { get; }

        void SetMultiTouchModeEnabled(bool enabled);

        void BeginShapeDrawing(ShapeToolKind tool);

        void EndShapeDrawing(bool restoreInkMode);

        void SetToolModeToPen();

        void SetCanvasManipulationEnabled(bool enabled);

        void CancelSingleFingerDragMode();

        void ToggleSingleFingerDragMode();

        bool TryGetLongPressShapeTool(object sender, out ShapeToolKind tool);

        void AnimateLongPressPreview(UIElement shapeButton);

        void ResetLongPressPreview(UIElement previewElement);

        void CollapseShapePanel(bool isLongPressSelected);

        void DrawShapePromptToPen();

        void InitializeCuboidDrawing();

        void UpdateSelectionCloneToggleVisual(bool enabled);

        void CommitSelectionCloneToBoardOrNewPage();

        void DeleteSelection(object sender, RoutedEventArgs e);

        void ChangeSelectedStrokeThickness(double multiplier);

        void RestoreSelectedStrokeThickness();

        void SaveSelectionToImage();

        void ApplySelectionMatrixTransform(int type);

        void HideSelectionCover();
    }
}

