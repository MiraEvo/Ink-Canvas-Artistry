using Ink_Canvas.ViewModels;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;

namespace Ink_Canvas.Controllers.Input
{
    public interface IInkCanvasInteractionController
    {
        void ConfigureMultiTouchMode(
            bool enabled,
            StylusDownEventHandler stylusDownHandler,
            StylusEventHandler stylusMoveHandler,
            StylusEventHandler stylusUpHandler,
            EventHandler<TouchEventArgs> multiTouchTouchDownHandler,
            EventHandler<TouchEventArgs> singleTouchTouchDownHandler);

        void ApplyCanvasInteractionMode(CanvasInteractionMode mode, double pointEraserDiameter = 50, double strokeEraserDiameter = 5);

        void EnterShapeDrawing(ShapeToolKind tool);

        void ExitShapeDrawing(bool restoreInkMode);

        void HandleMultiTouchTouchDown(
            TouchEventArgs e,
            Ink_Canvas.Settings settings,
            double boundsWidth,
            bool forceEraser,
            bool forcePointEraser,
            bool isShapeDrawing,
            Action hideSubPanels);

        void HandleStylusDown(StylusDownEventArgs e);

        void HandleStylusMove(StylusEventArgs e, IInputElement relativeTo);

        Task HandleStylusUpAsync(StylusEventArgs e, Action<Stroke> onStrokeCollected);

        InkCanvasEditingMode GetTouchDownMode(int id);

        void ResetTransientState();
    }
}

