using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace Ink_Canvas.ViewModels
{
    public sealed class InputStateViewModel : ObservableObject
    {
        private CanvasInteractionMode canvasInteractionMode = CanvasInteractionMode.Ink;
        private bool isMultiTouchMode;
        private bool forceEraser;
        private bool forcePointEraser = true;
        private ShapeToolKind activeShapeTool = ShapeToolKind.None;
        private bool isTwoFingerGestureTemporarilySuspended;

        public event Action<CanvasInteractionMode> CanvasInteractionModeChanged;

        public event Action<bool> MultiTouchModeChanged;

        public event Action<ShapeToolKind> ActiveShapeToolChanged;

        public CanvasInteractionMode CanvasInteractionMode => canvasInteractionMode;

        public bool IsMultiTouchMode => isMultiTouchMode;

        public bool ForceEraser => forceEraser;

        public bool ForcePointEraser => forcePointEraser;

        public ShapeToolKind ActiveShapeTool => activeShapeTool;

        public bool IsTwoFingerGestureTemporarilySuspended => isTwoFingerGestureTemporarilySuspended;

        public bool IsShapeDrawing => activeShapeTool != ShapeToolKind.None
            || canvasInteractionMode == CanvasInteractionMode.ShapeDrawing;

        public bool IsSelectionEditing => canvasInteractionMode == CanvasInteractionMode.Select;

        public bool IsInkEditing => canvasInteractionMode == CanvasInteractionMode.Ink;

        public bool IsEraserEditing => canvasInteractionMode == CanvasInteractionMode.EraseByPoint
            || canvasInteractionMode == CanvasInteractionMode.EraseByStroke;

        public bool SetCanvasInteractionMode(CanvasInteractionMode mode, bool notify = true, bool force = false)
        {
            bool changed = SetProperty(ref canvasInteractionMode, mode);
            if (changed)
            {
                OnPropertyChanged(nameof(IsShapeDrawing));
                OnPropertyChanged(nameof(IsSelectionEditing));
                OnPropertyChanged(nameof(IsInkEditing));
                OnPropertyChanged(nameof(IsEraserEditing));
            }

            if (notify && (changed || force))
            {
                CanvasInteractionModeChanged?.Invoke(mode);
            }

            return changed;
        }

        public bool SetMultiTouchMode(bool enabled, bool notify = true)
        {
            bool changed = SetProperty(ref isMultiTouchMode, enabled);
            if (notify && changed)
            {
                MultiTouchModeChanged?.Invoke(enabled);
            }

            return changed;
        }

        public bool SetForceEraser(bool enabled, bool notify = true)
        {
            return SetProperty(ref forceEraser, enabled);
        }

        public bool SetForcePointEraser(bool enabled, bool notify = true)
        {
            return SetProperty(ref forcePointEraser, enabled);
        }

        public bool SetActiveShapeTool(ShapeToolKind tool, bool notify = true)
        {
            bool changed = SetProperty(ref activeShapeTool, tool);
            if (changed)
            {
                OnPropertyChanged(nameof(IsShapeDrawing));
            }

            if (notify && changed)
            {
                ActiveShapeToolChanged?.Invoke(tool);
            }

            return changed;
        }

        public bool SetTwoFingerGestureTemporarilySuspended(bool enabled)
        {
            return SetProperty(ref isTwoFingerGestureTemporarilySuspended, enabled);
        }

        public void ResetTransientOverrides()
        {
            SetTwoFingerGestureTemporarilySuspended(false);
            SetActiveShapeTool(ShapeToolKind.None, false);
            SetForceEraser(false, false);
            SetForcePointEraser(true, false);
        }
    }
}
