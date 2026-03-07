using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace Ink_Canvas.ViewModels.Ink
{
    public sealed class InputStateViewModel : ObservableObject
    {
        private static readonly string[] CanvasInteractionModeDependentProperties =
        [
            nameof(IsShapeDrawing),
            nameof(IsSelectionEditing),
            nameof(IsInkEditing),
            nameof(IsEraserEditing)
        ];

        private static readonly string[] ActiveShapeToolDependentProperties =
        [
            nameof(IsShapeDrawing)
        ];

        private CanvasInteractionMode canvasInteractionMode = CanvasInteractionMode.Ink;
        private bool isMultiTouchMode;
        private bool forceEraser;
        private bool forcePointEraser = true;
        private ShapeToolKind activeShapeTool = ShapeToolKind.None;
        private bool isTwoFingerGestureTemporarilySuspended;

        public event Action<CanvasInteractionMode>? CanvasInteractionModeChanged;

        public event Action<bool>? MultiTouchModeChanged;

        public event Action<ShapeToolKind>? ActiveShapeToolChanged;

        public CanvasInteractionMode CanvasInteractionMode => canvasInteractionMode;

        public bool IsMultiTouchMode => isMultiTouchMode;

        public bool ForceEraser => forceEraser;

        public bool ForcePointEraser => forcePointEraser;

        public ShapeToolKind ActiveShapeTool => activeShapeTool;

        public bool IsTwoFingerGestureTemporarilySuspended => isTwoFingerGestureTemporarilySuspended;

        public bool IsShapeDrawing => activeShapeTool is not ShapeToolKind.None
            || canvasInteractionMode is CanvasInteractionMode.ShapeDrawing;

        public bool IsSelectionEditing => canvasInteractionMode is CanvasInteractionMode.Select;

        public bool IsInkEditing => canvasInteractionMode is CanvasInteractionMode.Ink;

        public bool IsEraserEditing => canvasInteractionMode is CanvasInteractionMode.EraseByPoint
            or CanvasInteractionMode.EraseByStroke;

        public bool SetCanvasInteractionMode(CanvasInteractionMode mode, bool notify = true, bool force = false)
        {
            bool changed = SetState(ref canvasInteractionMode, mode, CanvasInteractionModeDependentProperties);
            NotifyListener(CanvasInteractionModeChanged, mode, notify, changed, force);
            return changed;
        }

        public bool SetMultiTouchMode(bool enabled, bool notify = true)
        {
            bool changed = SetProperty(ref isMultiTouchMode, enabled);
            NotifyListener(MultiTouchModeChanged, enabled, notify, changed);
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
            bool changed = SetState(ref activeShapeTool, tool, ActiveShapeToolDependentProperties);
            NotifyListener(ActiveShapeToolChanged, tool, notify, changed);
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

        private bool SetState<T>(ref T field, T value, string[] dependentProperties)
        {
            bool changed = SetProperty(ref field, value);
            if (changed)
            {
                NotifyPropertiesChanged(dependentProperties);
            }

            return changed;
        }

        private void NotifyPropertiesChanged(string[] propertyNames)
        {
            foreach (string propertyName in propertyNames)
            {
                OnPropertyChanged(propertyName);
            }
        }

        private static void NotifyListener<T>(Action<T>? listener, T value, bool notify, bool changed, bool force = false)
        {
            if (notify && (changed || force))
            {
                listener?.Invoke(value);
            }
        }
    }
}

