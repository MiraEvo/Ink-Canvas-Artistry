using Ink_Canvas.Helpers;
using Ink_Canvas.ViewModels;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;

namespace Ink_Canvas.Controllers
{
    public sealed class InkCanvasInteractionController : IInkCanvasInteractionController
    {
        private readonly InkCanvas inkCanvas;
        private readonly InputStateViewModel inputStateViewModel;
        private readonly Dictionary<int, InkCanvasEditingMode> touchDownPointsList = new Dictionary<int, InkCanvasEditingMode>();
        private readonly Dictionary<int, StrokeVisual> strokeVisualList = new Dictionary<int, StrokeVisual>();
        private readonly Dictionary<int, VisualCanvas> visualCanvasList = new Dictionary<int, VisualCanvas>();

        public InkCanvasInteractionController(InkCanvas inkCanvas, InputStateViewModel inputStateViewModel)
        {
            this.inkCanvas = inkCanvas;
            this.inputStateViewModel = inputStateViewModel;
        }

        public void ConfigureMultiTouchMode(
            bool enabled,
            StylusDownEventHandler stylusDownHandler,
            StylusEventHandler stylusMoveHandler,
            StylusEventHandler stylusUpHandler,
            EventHandler<TouchEventArgs> multiTouchTouchDownHandler,
            EventHandler<TouchEventArgs> singleTouchTouchDownHandler)
        {
            if (enabled)
            {
                inkCanvas.StylusDown += stylusDownHandler;
                inkCanvas.StylusMove += stylusMoveHandler;
                inkCanvas.StylusUp += stylusUpHandler;
                inkCanvas.TouchDown += multiTouchTouchDownHandler;
                inkCanvas.TouchDown -= singleTouchTouchDownHandler;
                ApplyCanvasInteractionMode(CanvasInteractionMode.Ink);
            }
            else
            {
                inkCanvas.StylusDown -= stylusDownHandler;
                inkCanvas.StylusMove -= stylusMoveHandler;
                inkCanvas.StylusUp -= stylusUpHandler;
                inkCanvas.TouchDown -= multiTouchTouchDownHandler;
                inkCanvas.TouchDown += singleTouchTouchDownHandler;
                ApplyCanvasInteractionMode(CanvasInteractionMode.Ink);
                ResetTransientState();
            }

            inputStateViewModel.SetMultiTouchMode(enabled, false);
        }

        public void ApplyCanvasInteractionMode(CanvasInteractionMode mode, double pointEraserDiameter = 50, double strokeEraserDiameter = 5)
        {
            switch (mode)
            {
                case CanvasInteractionMode.Ink:
                    inkCanvas.EditingMode = InkCanvasEditingMode.Ink;
                    break;
                case CanvasInteractionMode.Select:
                    inkCanvas.EditingMode = InkCanvasEditingMode.Select;
                    break;
                case CanvasInteractionMode.EraseByPoint:
                    inkCanvas.EraserShape = new EllipseStylusShape(pointEraserDiameter, pointEraserDiameter);
                    inkCanvas.EditingMode = InkCanvasEditingMode.EraseByPoint;
                    break;
                case CanvasInteractionMode.EraseByStroke:
                    inkCanvas.EraserShape = new EllipseStylusShape(strokeEraserDiameter, strokeEraserDiameter);
                    inkCanvas.EditingMode = InkCanvasEditingMode.EraseByStroke;
                    break;
                case CanvasInteractionMode.GestureOnly:
                    inkCanvas.EditingMode = InkCanvasEditingMode.GestureOnly;
                    break;
                case CanvasInteractionMode.ShapeDrawing:
                case CanvasInteractionMode.Suspended:
                    inkCanvas.EditingMode = InkCanvasEditingMode.None;
                    break;
                case CanvasInteractionMode.Cursor:
                    break;
            }

            inputStateViewModel.SetCanvasInteractionMode(mode, false, true);
        }

        public void EnterShapeDrawing(ShapeToolKind tool)
        {
            inputStateViewModel.SetForceEraser(true, false);
            inputStateViewModel.SetActiveShapeTool(tool, false);
            ApplyCanvasInteractionMode(CanvasInteractionMode.ShapeDrawing);
        }

        public void ExitShapeDrawing(bool restoreInkMode)
        {
            inputStateViewModel.SetActiveShapeTool(ShapeToolKind.None, false);
            inputStateViewModel.SetForceEraser(false, false);
            if (restoreInkMode)
            {
                ApplyCanvasInteractionMode(CanvasInteractionMode.Ink);
            }
            else
            {
                ApplyCanvasInteractionMode(CanvasInteractionMode.Suspended);
            }
        }

        public void HandleMultiTouchTouchDown(
            TouchEventArgs e,
            Ink_Canvas.Settings settings,
            double boundsWidthThreshold,
            bool forceEraser,
            bool forcePointEraser,
            bool isShapeDrawing,
            Action hideSubPanels)
        {
            if (inkCanvas.EditingMode == InkCanvasEditingMode.EraseByPoint
                || inkCanvas.EditingMode == InkCanvasEditingMode.EraseByStroke
                || inkCanvas.EditingMode == InkCanvasEditingMode.Select)
            {
                return;
            }

            hideSubPanels?.Invoke();

            double touchBoundsWidth = e.GetTouchPoint(null).Bounds.Width;
            if ((settings.Advanced.TouchMultiplier != 0 || !settings.Advanced.IsSpecialScreen)
                && touchBoundsWidth > boundsWidthThreshold)
            {
                if (!isShapeDrawing && forceEraser)
                {
                    return;
                }

                double eraserThresholdValue = settings.Startup.IsEnableNibMode
                    ? settings.Advanced.NibModeBoundsWidthThresholdValue
                    : settings.Advanced.FingerModeBoundsWidthThresholdValue;

                if (touchBoundsWidth > boundsWidthThreshold * eraserThresholdValue)
                {
                    double diameter = touchBoundsWidth * (settings.Startup.IsEnableNibMode
                        ? settings.Advanced.NibModeBoundsWidthEraserSize
                        : settings.Advanced.FingerModeBoundsWidthEraserSize);

                    if (settings.Advanced.IsSpecialScreen)
                    {
                        diameter *= settings.Advanced.TouchMultiplier;
                    }

                    touchDownPointsList[e.TouchDevice.Id] = InkCanvasEditingMode.EraseByPoint;
                    ApplyCanvasInteractionMode(CanvasInteractionMode.EraseByPoint, diameter);
                }
                else
                {
                    ApplyCanvasInteractionMode(CanvasInteractionMode.EraseByStroke, strokeEraserDiameter: 5);
                }
            }
            else
            {
                touchDownPointsList[e.TouchDevice.Id] = InkCanvasEditingMode.None;
                inkCanvas.EraserShape = forcePointEraser ? new EllipseStylusShape(50, 50) : new EllipseStylusShape(5, 5);
                ApplyCanvasInteractionMode(CanvasInteractionMode.Suspended);
            }
        }

        public void HandleStylusDown(StylusDownEventArgs e)
        {
            if (inkCanvas.EditingMode == InkCanvasEditingMode.EraseByPoint
                || inkCanvas.EditingMode == InkCanvasEditingMode.EraseByStroke
                || inkCanvas.EditingMode == InkCanvasEditingMode.Select)
            {
                return;
            }

            touchDownPointsList[e.StylusDevice.Id] = InkCanvasEditingMode.None;
        }

        public void HandleStylusMove(StylusEventArgs e, IInputElement relativeTo)
        {
            if (GetTouchDownMode(e.StylusDevice.Id) != InkCanvasEditingMode.None)
            {
                return;
            }

            if (e.StylusDevice.StylusButtons.Count > 1
                && e.StylusDevice.StylusButtons[1].StylusButtonState == StylusButtonState.Down)
            {
                return;
            }

            try
            {
                StrokeVisual strokeVisual = GetStrokeVisual(e.StylusDevice.Id);
                StylusPointCollection stylusPointCollection = e.GetStylusPoints(relativeTo);
                foreach (StylusPoint stylusPoint in stylusPointCollection)
                {
                    strokeVisual.Add(new StylusPoint(stylusPoint.X, stylusPoint.Y, stylusPoint.PressureFactor));
                }

                strokeVisual.Redraw();
            }
            catch (ArgumentException ex)
            {
                LogHelper.WriteLogToFile(ex, "Ink Interaction | Invalid stylus point data during move");
            }
            catch (InvalidOperationException ex)
            {
                LogHelper.WriteLogToFile(ex, "Ink Interaction | Failed to redraw stroke preview during move");
            }
        }

        public async Task HandleStylusUpAsync(StylusEventArgs e, Action<Stroke> onStrokeCollected)
        {
            if (e.StylusDevice.TabletDevice.Type != TabletDeviceType.Stylus)
            {
                try
                {
                    StrokeVisual strokeVisual = GetStrokeVisual(e.StylusDevice.Id);
                    inkCanvas.Strokes.Add(strokeVisual.Stroke);
                    await Task.Delay(5);
                    VisualCanvas visualCanvas = GetVisualCanvas(e.StylusDevice.Id);
                    if (visualCanvas != null)
                    {
                        inkCanvas.Children.Remove(visualCanvas);
                    }

                    onStrokeCollected?.Invoke(strokeVisual.Stroke);
                }
                catch (ArgumentException ex)
                {
                    LogHelper.WriteLogToFile(ex, "Ink Interaction | Invalid stylus point data during stylus up");
                }
                catch (InvalidOperationException ex)
                {
                    LogHelper.WriteLogToFile(ex, "Ink Interaction | Failed to finalize stroke preview during stylus up");
                }
            }

            strokeVisualList.Remove(e.StylusDevice.Id);
            visualCanvasList.Remove(e.StylusDevice.Id);
            touchDownPointsList.Remove(e.StylusDevice.Id);
            if (strokeVisualList.Count == 0 || visualCanvasList.Count == 0 || touchDownPointsList.Count == 0)
            {
                ResetTransientState();
            }
        }

        public InkCanvasEditingMode GetTouchDownMode(int id)
        {
            if (touchDownPointsList.TryGetValue(id, out InkCanvasEditingMode inkCanvasEditingMode))
            {
                return inkCanvasEditingMode;
            }

            return inkCanvas.EditingMode;
        }

        public void ResetTransientState()
        {
            touchDownPointsList.Clear();
            strokeVisualList.Clear();
            visualCanvasList.Clear();
        }

        private StrokeVisual GetStrokeVisual(int id)
        {
            if (strokeVisualList.TryGetValue(id, out StrokeVisual visual))
            {
                return visual;
            }

            StrokeVisual strokeVisual = new StrokeVisual(inkCanvas.DefaultDrawingAttributes.Clone());
            strokeVisualList[id] = strokeVisual;
            VisualCanvas visualCanvas = new VisualCanvas(strokeVisual);
            visualCanvasList[id] = visualCanvas;
            inkCanvas.Children.Add(visualCanvas);
            return strokeVisual;
        }

        private VisualCanvas GetVisualCanvas(int id)
        {
            if (visualCanvasList.TryGetValue(id, out VisualCanvas visualCanvas))
            {
                return visualCanvas;
            }

            return null;
        }
    }
}
