using Ink_Canvas.Helpers;
using System;
using Ink_Canvas.ViewModels;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Linq;
using Point = System.Windows.Point;

namespace Ink_Canvas
{
    public partial class MainWindow : Window
    {
        #region Multi-Touch

        private void BorderMultiTouchMode_MouseUp(object sender, MouseButtonEventArgs e)
        {
            inkGestureCoordinator?.HandleMultiTouchToggle();
        }

        private void MainWindow_TouchDown(object sender, TouchEventArgs e)
        {
            inkGestureCoordinator?.HandleMainWindowTouchDown(e);
        }

        private void MainWindow_StylusDown(object sender, StylusDownEventArgs e)
        {
            inkCanvasInteractionController.HandleStylusDown(e);
        }

        private async void MainWindow_StylusUp(object sender, StylusEventArgs e)
        {
            await inkCanvasInteractionController.HandleStylusUpAsync(
                e,
                stroke => inkCanvas_StrokeCollected(inkCanvas, new InkCanvasStrokeCollectedEventArgs(stroke)));
        }

        private void MainWindow_StylusMove(object sender, StylusEventArgs e)
        {
            inkCanvasInteractionController.HandleStylusMove(e, this);
        }

        #endregion
        private void Main_Grid_TouchDown(object sender, TouchEventArgs e)
        {
            inkGestureCoordinator?.HandleGridTouchDown(e);
        }

        public double GetTouchBoundWidth(TouchEventArgs e)
        {
            var args = e.GetTouchPoint(null).Bounds;
            if (!Settings.Advanced.IsQuadIR) return args.Width;
            else return Math.Sqrt(args.Width * args.Height); //四边红外
        }

        private void inkCanvas_PreviewTouchDown(object sender, TouchEventArgs e)
        {
            inkGestureCoordinator?.HandlePreviewTouchDown(e);
        }

        private void inkCanvas_PreviewTouchUp(object sender, TouchEventArgs e)
        {
            inkGestureCoordinator?.HandlePreviewTouchUp(e);
        }
        private void inkCanvas_ManipulationStarting(object sender, ManipulationStartingEventArgs e)
        {
            e.Mode = ManipulationModes.All;
        }

        private void inkCanvas_ManipulationInertiaStarting(object sender, ManipulationInertiaStartingEventArgs e)
        {

        }

        private void Main_Grid_ManipulationCompleted(object sender, ManipulationCompletedEventArgs e)
        {
            inkGestureCoordinator?.HandleManipulationCompleted(e);
        }

        private void Main_Grid_ManipulationDelta(object sender, ManipulationDeltaEventArgs e)
        {
            inkGestureCoordinator?.HandleManipulationDelta(e);
        }
    }
}
