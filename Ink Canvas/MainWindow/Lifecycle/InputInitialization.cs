using Ink_Canvas.Controllers;
using Ink_Canvas.Features.Ink.Engine;
using Ink_Canvas.Features.Ink;
using Ink_Canvas.ViewModels;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;

namespace Ink_Canvas
{
    public partial class MainWindow
    {
        private IInkCanvasInteractionController inkCanvasInteractionController;
        private InkEngineCoordinator inkEngineCoordinator = null!;

        private InputStateViewModel InputStateViewModel => mainWindowViewModel.Input;

        private bool isInMultiTouchMode
        {
            get => InputStateViewModel?.IsMultiTouchMode ?? false;
            set => InputStateViewModel?.SetMultiTouchMode(value, false);
        }

        private bool forceEraser
        {
            get => InputStateViewModel?.ForceEraser ?? false;
            set => InputStateViewModel?.SetForceEraser(value, false);
        }

        private bool forcePointEraser
        {
            get => InputStateViewModel?.ForcePointEraser ?? true;
            set => InputStateViewModel?.SetForcePointEraser(value, false);
        }

        private int drawingShapeMode
        {
            get => (int)(InputStateViewModel?.ActiveShapeTool ?? ShapeToolKind.None);
            set => InputStateViewModel?.SetActiveShapeTool((ShapeToolKind)value, false);
        }

        private bool lastIsInMultiTouchMode
        {
            get => InputStateViewModel?.IsTwoFingerGestureTemporarilySuspended ?? false;
            set => InputStateViewModel?.SetTwoFingerGestureTemporarilySuspended(value);
        }

        private void InitializeInputController()
        {
            inkEngineCoordinator = new InkEngineCoordinator(appLogger);
            inkEngineCoordinator.AttachHost(this, new InkEngineOptions(EnablePressure: true, EnableRealtimePreview: true));
            inkCanvasInteractionController = new InkCanvasInteractionController(
                inkCanvas,
                mainWindowViewModel.Input,
                appLogger,
                sample => inkEngineCoordinator.ProcessInput(sample));
            InitializeInkFeature();
        }

        private void ApplyInkRuntimeRouting(string reason)
        {
            if (inkEngineCoordinator == null)
            {
                return;
            }

            InkRuntimeRoutingChange change = inkEngineCoordinator.ApplySettings(Settings);
            if (change.BackendChanged)
            {
                inkHistoryCoordinator?.ClearHistory();
                inkHistoryCoordinator?.RecordSessionReset(reason);
            }
        }

        private void EmitTouchInputSample(System.Windows.Input.TouchEventArgs e, InkInputPhase phase)
        {
            if (inkEngineCoordinator == null)
            {
                return;
            }

            Point position = e.GetTouchPoint(inkCanvas).Position;
            inkEngineCoordinator.ProcessInput(new InkInputSample(
                e.TouchDevice.Id,
                InkInputDeviceKind.Touch,
                phase,
                DateTimeOffset.UtcNow,
                [new InkInputPoint((float)position.X, (float)position.Y, 0.5f)]));
        }

        private void EmitMouseInputSample(System.Windows.Input.MouseEventArgs e, InkInputPhase phase)
        {
            if (inkEngineCoordinator == null)
            {
                return;
            }

            Point position = e.GetPosition(inkCanvas);
            inkEngineCoordinator.ProcessInput(new InkInputSample(
                -1,
                InkInputDeviceKind.Mouse,
                phase,
                DateTimeOffset.UtcNow,
                [new InkInputPoint((float)position.X, (float)position.Y, 0.5f)]));
        }

        private void ApplyCanvasInteractionMode(CanvasInteractionMode mode, double pointEraserDiameter = 50, double strokeEraserDiameter = 5)
        {
            inkCanvasInteractionController?.ApplyCanvasInteractionMode(mode, pointEraserDiameter, strokeEraserDiameter);
        }

        private void EnterShapeDrawingMode(ShapeToolKind tool)
        {
            inkCanvasInteractionController?.EnterShapeDrawing(tool);
        }

        private void ExitShapeDrawingMode(bool restoreInkMode)
        {
            inkCanvasInteractionController?.ExitShapeDrawing(restoreInkMode);
        }

        private void SetCursorInteractionMode()
        {
            InputStateViewModel?.SetCanvasInteractionMode(CanvasInteractionMode.Cursor, false, true);
        }

        private void SyncInputInteractionMode()
        {
            if (InputStateViewModel == null)
            {
                return;
            }

            CanvasInteractionMode mode = inkCanvas.EditingMode switch
            {
                InkCanvasEditingMode.Ink => CanvasInteractionMode.Ink,
                InkCanvasEditingMode.Select => CanvasInteractionMode.Select,
                InkCanvasEditingMode.EraseByPoint => CanvasInteractionMode.EraseByPoint,
                InkCanvasEditingMode.EraseByStroke => CanvasInteractionMode.EraseByStroke,
                InkCanvasEditingMode.GestureOnly => CanvasInteractionMode.GestureOnly,
                InkCanvasEditingMode.None when drawingShapeMode != 0 => CanvasInteractionMode.ShapeDrawing,
                InkCanvasEditingMode.None => CanvasInteractionMode.Suspended,
                _ => InputStateViewModel.CanvasInteractionMode
            };

            InputStateViewModel.SetCanvasInteractionMode(mode, false, true);
        }
    }
}
