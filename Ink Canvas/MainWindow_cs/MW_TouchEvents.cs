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
            inkCanvasInteractionController.ConfigureMultiTouchMode(
                !isInMultiTouchMode,
                MainWindow_StylusDown,
                MainWindow_StylusMove,
                MainWindow_StylusUp,
                MainWindow_TouchDown,
                Main_Grid_TouchDown);
        }

        private void MainWindow_TouchDown(object sender, TouchEventArgs e)
        {
            inkCanvasInteractionController.HandleMultiTouchTouchDown(
                e,
                Settings,
                BoundsWidth,
                forceEraser,
                forcePointEraser,
                drawingShapeMode != 0,
                () =>
                {
                    if (!isHidingSubPanelsWhenInking)
                    {
                        isHidingSubPanelsWhenInking = true;
                        HideSubPanels();
                    }
                });
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

        int lastTouchDownTime = 0, lastTouchUpTime = 0;

        Point iniP = new Point(0, 0);
        bool isLastTouchEraser = false;

        private void Main_Grid_TouchDown(object sender, TouchEventArgs e)
        {
            if (!isHidingSubPanelsWhenInking)
            {
                isHidingSubPanelsWhenInking = true;
                HideSubPanels(); // 书写时自动隐藏二级菜单
            }

            if (NeedUpdateIniP())
            {
                iniP = e.GetTouchPoint(inkCanvas).Position;
            }
            if (drawingShapeMode == 9 && isFirstTouchCuboid == false)
            {
                MouseTouchMove(iniP);
            }
            inkCanvas.Opacity = 1;
            double boundsWidth = GetTouchBoundWidth(e);
            if ((Settings.Advanced.TouchMultiplier != 0 || !Settings.Advanced.IsSpecialScreen) //启用特殊屏幕且触摸倍数为 0 时禁用橡皮
                && (boundsWidth > BoundsWidth))
            {
                isLastTouchEraser = true;
                if (drawingShapeMode == 0 && forceEraser) return;
                double EraserThresholdValue = Settings.Startup.IsEnableNibMode ? Settings.Advanced.NibModeBoundsWidthThresholdValue : Settings.Advanced.FingerModeBoundsWidthThresholdValue;
                if (boundsWidth > BoundsWidth * EraserThresholdValue)
                {
                    boundsWidth *= (Settings.Startup.IsEnableNibMode ? Settings.Advanced.NibModeBoundsWidthEraserSize : Settings.Advanced.FingerModeBoundsWidthEraserSize);
                    if (Settings.Advanced.IsSpecialScreen) boundsWidth *= Settings.Advanced.TouchMultiplier;
                    ApplyCanvasInteractionMode(CanvasInteractionMode.EraseByPoint, boundsWidth);
                }
                else
                {
                    if (IsPresentationSlideShowRunning && inkCanvas.Strokes.Count == 0 && Settings.PowerPointSettings.IsEnableFingerGestureSlideShowControl)
                    {
                        isLastTouchEraser = false;
                        ApplyCanvasInteractionMode(CanvasInteractionMode.GestureOnly);
                        inkCanvas.Opacity = 0.1;
                    }
                    else
                    {
                        ApplyCanvasInteractionMode(CanvasInteractionMode.EraseByStroke, strokeEraserDiameter: 5);
                    }
                }
            }
            else
            {
                isLastTouchEraser = false;
                inkCanvas.EraserShape = forcePointEraser ? new EllipseStylusShape(50, 50) : new EllipseStylusShape(5, 5);
                if (forceEraser) return;
                ApplyCanvasInteractionMode(CanvasInteractionMode.Ink);
            }
        }

        public double GetTouchBoundWidth(TouchEventArgs e)
        {
            var args = e.GetTouchPoint(null).Bounds;
            if (!Settings.Advanced.IsQuadIR) return args.Width;
            else return Math.Sqrt(args.Width * args.Height); //四边红外
        }

        //记录触摸设备ID
        private List<int> dec = new List<int>();
        //中心点
        Point centerPoint;
        InkCanvasEditingMode lastInkCanvasEditingMode = InkCanvasEditingMode.Ink;
        bool isSingleFingerDragMode = false;

        private void inkCanvas_PreviewTouchDown(object sender, TouchEventArgs e)
        {
            dec.Add(e.TouchDevice.Id);
            //设备1个的时候，记录中心点
            if (dec.Count == 1)
            {
                TouchPoint touchPoint = e.GetTouchPoint(inkCanvas);
                centerPoint = touchPoint.Position;

                //记录第一根手指点击时的 StrokeCollection
                lastTouchDownStrokeCollection = inkCanvas.Strokes.Clone();
            }
            //设备两个及两个以上，将画笔功能关闭
            if (dec.Count > 1 || isSingleFingerDragMode || !Settings.Gesture.IsEnableTwoFingerGesture)
            {
                if (isInMultiTouchMode || !Settings.Gesture.IsEnableTwoFingerGesture) return;
                if (inkCanvas.EditingMode != InkCanvasEditingMode.None && inkCanvas.EditingMode != InkCanvasEditingMode.Select)
                {
                    lastInkCanvasEditingMode = inkCanvas.EditingMode;
                    ApplyCanvasInteractionMode(CanvasInteractionMode.Suspended);
                }
            }
        }

        private void inkCanvas_PreviewTouchUp(object sender, TouchEventArgs e)
        {
            //手势完成后切回之前的状态
            if (dec.Count > 1)
            {
                if (inkCanvas.EditingMode == InkCanvasEditingMode.None)
                {
                    inkCanvas.EditingMode = lastInkCanvasEditingMode;
                }
            }
            dec.Remove(e.TouchDevice.Id);
            inkCanvas.Opacity = 1;
            if (dec.Count == 0)
            {
                if (lastTouchDownStrokeCollection.Count() != inkCanvas.Strokes.Count() &&
                    !(drawingShapeMode == 9 && !isFirstTouchCuboid))
                {
                    int whiteboardIndex = CurrentWhiteboardIndex;
                    if (ShellViewModel.IsDesktopAnnotationMode)
                    {
                        whiteboardIndex = 0;
                    }
                    strokeCollections[whiteboardIndex] = lastTouchDownStrokeCollection;
                }
            }
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
            if (e.Manipulators.Count() == 0)
            {
                if (forceEraser) return;
                ApplyCanvasInteractionMode(CanvasInteractionMode.Ink);
            }
        }

        private void Main_Grid_ManipulationDelta(object sender, ManipulationDeltaEventArgs e)
        {
            if (isInMultiTouchMode || !Settings.Gesture.IsEnableTwoFingerGesture) return;
            if ((dec.Count >= 2 && (Settings.PowerPointSettings.IsEnableTwoFingerGestureInPresentationMode || !IsPresentationSlideShowRunning)) || isSingleFingerDragMode)
            {
                Matrix m = new Matrix();
                ManipulationDelta md = e.DeltaManipulation;
                // Translation
                Vector trans = md.Translation;
                // Rotate, Scale
                if (Settings.Gesture.IsEnableTwoFingerGestureTranslateOrRotation)
                {
                    double rotate = md.Rotation;
                    Vector scale = md.Scale;
                    Point center = GetMatrixTransformCenterPoint(e.ManipulationOrigin, e.Source as FrameworkElement);
                    if (Settings.Gesture.IsEnableTwoFingerZoom)
                        m.ScaleAt(scale.X, scale.Y, center.X, center.Y);
                    if (Settings.Gesture.IsEnableTwoFingerRotation)
                        m.RotateAt(rotate, center.X, center.Y);
                    if (Settings.Gesture.IsEnableTwoFingerTranslate)
                        m.Translate(trans.X, trans.Y);
                    // handle Elements
                    List<UIElement> elements = InkCanvasElementsHelper.GetAllElements(inkCanvas);
                    foreach (UIElement element in elements)
                    {
                        if (Settings.Gesture.IsEnableTwoFingerTranslate)
                        {
                            ApplyElementMatrixTransform(element, m);
                        }
                        else
                        {
                            ApplyElementMatrixTransform(element, m);
                        }
                    }
                }
                // handle strokes
                if (Settings.Gesture.IsEnableTwoFingerZoom)
                {
                    foreach (Stroke stroke in inkCanvas.Strokes)
                    {
                        stroke.Transform(m, false);
                        try
                        {
                            stroke.DrawingAttributes.Width *= md.Scale.X;
                            stroke.DrawingAttributes.Height *= md.Scale.Y;
                        }
                        catch { }
                    };
                }
                else
                {
                    foreach (Stroke stroke in inkCanvas.Strokes)
                    {
                        stroke.Transform(m, false);
                    };
                }
                foreach (Circle circle in circles)
                {
                    circle.R = GetDistance(circle.Stroke.StylusPoints[0].ToPoint(), circle.Stroke.StylusPoints[circle.Stroke.StylusPoints.Count / 2].ToPoint()) / 2;
                    circle.Centroid = new Point(
                        (circle.Stroke.StylusPoints[0].X + circle.Stroke.StylusPoints[circle.Stroke.StylusPoints.Count / 2].X) / 2,
                        (circle.Stroke.StylusPoints[0].Y + circle.Stroke.StylusPoints[circle.Stroke.StylusPoints.Count / 2].Y) / 2
                    );
                }
            }
        }
    }
}
