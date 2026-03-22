using Ink_Canvas.Helpers;
using Microsoft.Win32;
using Ink_Canvas.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Point = System.Windows.Point;

namespace Ink_Canvas
{
    public partial class MainWindow : Window
    {
        #region Floating Control

        private void Border_MouseDown(object sender, MouseButtonEventArgs e)
        {
            inkInteractionCoordinator?.HandleSelectionBorderMouseDown(sender);
        }

        private void BorderStrokeSelectionClone_Click(object sender, RoutedEventArgs e)
        {
            inkInteractionCoordinator?.ToggleSelectionClone();
        }

        private void BorderStrokeSelectionCloneToBoardOrNewPage_Click(object sender, RoutedEventArgs e)
        {
            inkInteractionCoordinator?.HandleSelectionCloneToBoardOrNewPage();
        }

        private void GridPenWidthDecrease_Click(object sender, RoutedEventArgs e)
        {
            inkInteractionCoordinator?.HandleSelectionThicknessChanged(0.8);
        }

        private void GridPenWidthIncrease_Click(object sender, RoutedEventArgs e)
        {
            inkInteractionCoordinator?.HandleSelectionThicknessChanged(1.25);
        }

        private void GridPenWidthRestore_Click(object sender, RoutedEventArgs e)
        {
            inkInteractionCoordinator?.HandleSelectionThicknessRestore();
        }

        private void BorderStrokeSelectionDelete_Click(object sender, RoutedEventArgs e)
        {
            inkInteractionCoordinator?.HandleSelectionDelete(sender, e);
        }

        private void BtnStrokeSelectionSaveToImage_Click(object sender, RoutedEventArgs e)
        {
            inkInteractionCoordinator?.HandleSelectionSaveToImage();
        }

        private void ChangeSelectedStrokeThickness(double multipler)
        {
            foreach (Stroke stroke in inkCanvas.GetSelectedStrokes())
            {
                var newWidth = stroke.DrawingAttributes.Width * multipler;
                var newHeight = stroke.DrawingAttributes.Height * multipler;
                if (newWidth >= DrawingAttributes.MinWidth && newWidth <= DrawingAttributes.MaxWidth
                    && newHeight >= DrawingAttributes.MinHeight && newHeight <= DrawingAttributes.MaxHeight)
                {
                    stroke.DrawingAttributes.Width = newWidth;
                    stroke.DrawingAttributes.Height = newHeight;
                }
            }
            CommitDrawingAttributesHistoryIfNeeded();
        }

        private void MatrixTransform(int type)
        {
            Matrix m = new Matrix();
            Rect bounds = inkCanvas.GetSelectionBounds();
            Point center = new Point(bounds.Left + bounds.Width / 2, bounds.Top + bounds.Height / 2);

            switch (type)
            {
                case 1: // Flip Horizontal
                    m.ScaleAt(-1, 1, center.X, center.Y);
                    break;
                case 2: // Flip Vertical
                    m.ScaleAt(1, -1, center.X, center.Y);
                    break;
                default: // Rotate
                    m.RotateAt(type, center.X, center.Y);
                    break;
            }

            List<UIElement> selectedElements = InkCanvasElementsHelper.GetSelectedElements(inkCanvas);
            foreach (UIElement element in selectedElements)
            {
                ApplyElementMatrixTransform(element, m);
            }

            StrokeCollection targetStrokes = inkCanvas.GetSelectedStrokes();
            foreach (Stroke stroke in targetStrokes)
            {
                stroke.Transform(m, false);
            }

            CommitDrawingAttributesHistoryIfNeeded();
            ToCommitStrokeManipulationHistoryAfterMouseUp();
        }

        private void ApplyElementMatrixTransform(UIElement element, Matrix matrix)
        {
            if (element is not FrameworkElement frameworkElement)
            {
                return;
            }

            TransformGroup transformGroup = frameworkElement.RenderTransform as TransformGroup;
            if (transformGroup == null)
            {
                transformGroup = new TransformGroup();
                frameworkElement.RenderTransform = transformGroup;
            }

            if (!ElementsInitialHistory.ContainsKey(frameworkElement.Name))
            {
                ElementsInitialHistory[frameworkElement.Name] = transformGroup.Clone();
            }

            TransformGroup centeredTransformGroup = new TransformGroup();
            centeredTransformGroup.Children.Add(new MatrixTransform(matrix));
            transformGroup.Children.Add(centeredTransformGroup);

            if (ElementsManipulationHistory == null)
            {
                ElementsManipulationHistory = new Dictionary<string, Tuple<object, TransformGroup>>();
            }
            ElementsManipulationHistory[frameworkElement.Name] =
                new Tuple<object, TransformGroup>(ElementsInitialHistory[frameworkElement.Name], transformGroup.Clone());
        }

        private static void ScaleStrokeDrawingAttributes(Stroke stroke, double scaleX, double scaleY)
        {
            if (stroke == null)
            {
                return;
            }

            DrawingAttributes drawingAttributes = stroke.DrawingAttributes;
            drawingAttributes.Width = ClampDrawingAttributeDimension(drawingAttributes.Width * Math.Abs(scaleX), DrawingAttributes.MinWidth, DrawingAttributes.MaxWidth);
            drawingAttributes.Height = ClampDrawingAttributeDimension(drawingAttributes.Height * Math.Abs(scaleY), DrawingAttributes.MinHeight, DrawingAttributes.MaxHeight);
        }

        private static double ClampDrawingAttributeDimension(double value, double minValue, double maxValue)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
            {
                return minValue;
            }

            if (value < minValue)
            {
                return minValue;
            }

            return value > maxValue ? maxValue : value;
        }

        private void BtnFlipHorizontal_Click(object sender, RoutedEventArgs e)
        {
            inkInteractionCoordinator?.HandleSelectionMatrixTransform(1);
        }

        private void BtnFlipVertical_Click(object sender, RoutedEventArgs e)
        {
            inkInteractionCoordinator?.HandleSelectionMatrixTransform(2);
        }

        private void BtnAnticlockwiseRotate15_Click(object sender, RoutedEventArgs e)
        {
            inkInteractionCoordinator?.HandleSelectionMatrixTransform(-15);
        }

        private void BtnAnticlockwiseRotate45_Click(object sender, RoutedEventArgs e)
        {
            inkInteractionCoordinator?.HandleSelectionMatrixTransform(-45);
        }

        private void BtnAnticlockwiseRotate90_Click(object sender, RoutedEventArgs e)
        {
            inkInteractionCoordinator?.HandleSelectionMatrixTransform(-90);
        }

        private void BtnClockwiseRotate15_Click(object sender, RoutedEventArgs e)
        {
            inkInteractionCoordinator?.HandleSelectionMatrixTransform(15);
        }

        private void BtnClockwiseRotate45_Click(object sender, RoutedEventArgs e)
        {
            inkInteractionCoordinator?.HandleSelectionMatrixTransform(45);
        }

        private void BtnClockwiseRotate90_Click(object sender, RoutedEventArgs e)
        {
            inkInteractionCoordinator?.HandleSelectionMatrixTransform(90);
        }

        #endregion

        private void GridInkCanvasSelectionCover_MouseDown(object sender, MouseButtonEventArgs e)
        {
            lastMousePoint = e.GetPosition(inkCanvas);
            isGridInkCanvasSelectionCoverMouseDown = true;
            if (isStrokeSelectionCloneOn)
            {
                StrokeCollection strokes = inkCanvas.GetSelectedStrokes();
                List<UIElement> elementsList = InkCanvasElementsHelper.GetSelectedElements(inkCanvas);
                isProgramChangeStrokeSelection = true;
                var elementsInitialHistory = ElementsInitialHistory;
                ElementsSelectionClone = InkCanvasElementsHelper.CloneSelectedElements(inkCanvas, ref elementsInitialHistory);
                ElementsInitialHistory = elementsInitialHistory;
                inkCanvas.Select(new StrokeCollection());
                StrokesSelectionClone = strokes.Clone();
                inkCanvas.Strokes.Add(StrokesSelectionClone);
                inkCanvas.Select(strokes, elementsList);
                isProgramChangeStrokeSelection = false;
            }
            else if (lastMousePoint.X < inkCanvas.GetSelectionBounds().Left ||
            lastMousePoint.Y < inkCanvas.GetSelectionBounds().Top ||
            lastMousePoint.X > inkCanvas.GetSelectionBounds().Right ||
            lastMousePoint.Y > inkCanvas.GetSelectionBounds().Bottom)
            {
                isGridInkCanvasSelectionCoverMouseDown = false;
                inkCanvas.Select(new StrokeCollection());
                StrokesSelectionClone = new StrokeCollection();
                ElementsSelectionClone = new List<UIElement>();
            }
        }

        private void GridInkCanvasSelectionCover_MouseMove(object sender, MouseEventArgs e)
        {
            if (!isGridInkCanvasSelectionCoverMouseDown) return;
            Point mousePoint = e.GetPosition(inkCanvas);
            Vector trans = new Vector(mousePoint.X - lastMousePoint.X, mousePoint.Y - lastMousePoint.Y);
            lastMousePoint = mousePoint;
            Matrix m = new Matrix();
            // add Translate
            m.Translate(trans.X, trans.Y);
            // handle UIElement
            List<UIElement> elements = ElementsSelectionClone.Count != 0
                ? ElementsSelectionClone
                : InkCanvasElementsHelper.GetSelectedElements(inkCanvas);
            foreach (UIElement element in elements)
            {
                ApplyElementMatrixTransform(element, m);
            }
            // handle strokes
            StrokeCollection strokes = StrokesSelectionClone.Count != 0
                ? StrokesSelectionClone
                : inkCanvas.GetSelectedStrokes();
            foreach (Stroke stroke in strokes)
            {
                stroke.Transform(m, false);
            }
            updateBorderStrokeSelectionControlLocation();
        }

        private void GridInkCanvasSelectionCover_MouseUp(object sender, MouseButtonEventArgs e)
        {
            ToCommitStrokeManipulationHistoryAfterMouseUp();
            isGridInkCanvasSelectionCoverMouseDown = false;
            if (InkCanvasElementsHelper.IsNotCanvasElementSelected(inkCanvas))
            {
                GridInkCanvasSelectionCover.Visibility = Visibility.Collapsed;
                StrokesSelectionClone = new StrokeCollection();
                ElementsSelectionClone = new List<UIElement>();
            }
            else
            {
                UpdateSelectionClonePrompt();
                GridInkCanvasSelectionCover.Visibility = Visibility.Visible;
                StrokesSelectionClone = new StrokeCollection();
                ElementsSelectionClone = new List<UIElement>();
            }
        }

        private void GridInkCanvasSelectionCover_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            double scale = e.Delta > 0 ? 1.1 : 0.9;
            Point center = InkCanvasElementsHelper.GetAllElementsBoundsCenterPoint(inkCanvas);
            Matrix m = new Matrix();
            m.ScaleAt(scale, scale, center.X, center.Y);

            StrokeCollection strokes = inkCanvas.GetSelectedStrokes();
            List<UIElement> elements = InkCanvasElementsHelper.GetSelectedElements(inkCanvas);
            // handle UIElement
            foreach (UIElement element in elements)
            {
                ApplyElementMatrixTransform(element, m);
            }
            // handle strokes
            foreach (Stroke stroke in strokes)
            {
                stroke.Transform(m, false);
                ScaleStrokeDrawingAttributes(stroke, scale, scale);
            }
            updateBorderStrokeSelectionControlLocation();
        }

        private void BtnSelect_Click(object sender, RoutedEventArgs e)
        {
            ShellViewModel.SetToolMode(ToolMode.Select, true, true);
        }
        private void inkCanvas_SelectionChanged(object sender, EventArgs e)
        {
            if (isProgramChangeStrokeSelection) return;
            if (InkCanvasElementsHelper.IsNotCanvasElementSelected(inkCanvas))
            {
                GridInkCanvasSelectionCover.Visibility = Visibility.Collapsed;
            }
            else
            {
                UpdateSelectionClonePrompt();
                GridInkCanvasSelectionCover.Visibility = Visibility.Visible;
                IconStrokeSelectionClone.SetResourceReference(TextBlock.ForegroundProperty, "FloatBarForeground");
                ToggleButtonStrokeSelectionClone.IsChecked = false;
                isStrokeSelectionCloneOn = false;
                updateBorderStrokeSelectionControlLocation();
            }
        }
        private void updateBorderStrokeSelectionControlLocation()
        {
            Rect selectionBounds = inkCanvas.GetSelectionBounds();
            double borderLeft = (selectionBounds.Left + selectionBounds.Right - BorderStrokeSelectionControlWidth) / 2;
            double borderTop = selectionBounds.Bottom + 15;

            // ensure the border is inside the window
            borderLeft = Math.Max(0, borderLeft);
            borderTop = Math.Max(0, borderTop);
            borderLeft = Math.Min(Width - BorderStrokeSelectionControlWidth, borderLeft);
            borderTop = Math.Min(Height - BorderStrokeSelectionControlHeight, borderTop);

            double borderBottom = borderTop + BorderStrokeSelectionControlHeight;
            double borderRight = borderLeft + BorderStrokeSelectionControlWidth;

            double viewboxTop = ViewboxFloatingBar.Margin.Top;
            double viewboxLeft = ViewboxFloatingBar.Margin.Left;
            double viewboxBottom = viewboxTop + ViewboxFloatingBar.ActualHeight;
            double viewboxRight = viewboxLeft + ViewboxFloatingBar.ActualWidth;

            if (ShellViewModel.IsDesktopAnnotationMode)
            {
                bool isHorizontalOverlap = (borderLeft < viewboxRight && borderRight > viewboxLeft);
                bool isVerticalOverlap = (borderTop < viewboxBottom && borderBottom > viewboxTop);
                if (isHorizontalOverlap && isVerticalOverlap)
                {
                    double belowViewboxMargin = viewboxBottom + 5;
                    double maxBottomPositionMargin = Height - BorderStrokeSelectionControlHeight;
                    borderTop = belowViewboxMargin > maxBottomPositionMargin
                        ? viewboxTop - BorderStrokeSelectionControlHeight - 5
                        : belowViewboxMargin;
                }
            }
            else
            {
                borderTop = Math.Min(Height - BorderStrokeSelectionControlHeight - 60, borderTop);
            }
            BorderStrokeSelectionControl.Margin = !double.IsNaN(borderLeft) && !double.IsNaN(borderTop)
                ? new Thickness(borderLeft, borderTop, 0, 0)
                : new Thickness(0, 0, 0, 0);
        }

        private void GridInkCanvasSelectionCover_ManipulationStarting(object sender, ManipulationStartingEventArgs e)
        {
            e.Mode = ManipulationModes.All;
        }

        private void GridInkCanvasSelectionCover_ManipulationCompleted(object sender, ManipulationCompletedEventArgs e)
        {
            CommitPendingManipulationHistory();
            CommitDrawingAttributesHistoryIfNeeded();
        }

        private void GridInkCanvasSelectionCover_ManipulationDelta(object sender, ManipulationDeltaEventArgs e)
        {
            if (dec.Count < 1 || e.Source is not FrameworkElement sourceElement)
            {
                return;
            }

            ManipulationDelta md = e.DeltaManipulation;
            Vector trans = md.Translation;
            double rotate = md.Rotation;
            Vector scale = md.Scale;
            Point center = GetMatrixTransformCenterPoint(e.ManipulationOrigin, sourceElement);
            Matrix m = new Matrix();
            m.ScaleAt(scale.X, scale.Y, center.X, center.Y);

            StrokeCollection strokes = StrokesSelectionClone.Count != 0
                ? StrokesSelectionClone
                : inkCanvas.GetSelectedStrokes();
            if (StrokesSelectionClone.Count == 0 && Settings.Gesture.IsEnableTwoFingerRotationOnSelection)
            {
                m.RotateAt(rotate, center.X, center.Y);
            }

            m.Translate(trans.X, trans.Y);

            List<UIElement> elements = ElementsSelectionClone.Count != 0
                ? ElementsSelectionClone
                : InkCanvasElementsHelper.GetSelectedElements(inkCanvas);
            foreach (UIElement element in elements)
            {
                ApplyElementMatrixTransform(element, m);
            }

            foreach (Stroke stroke in strokes)
            {
                stroke.Transform(m, false);
                ScaleStrokeDrawingAttributes(stroke, md.Scale.X, md.Scale.Y);
            }

            updateBorderStrokeSelectionControlLocation();
        }

        private void GridInkCanvasSelectionCover_PreviewTouchDown(object sender, TouchEventArgs e)
        {
            dec.Add(e.TouchDevice.Id);
            //设备1个的时候，记录中心点
            if (dec.Count == 1)
            {
                TouchPoint touchPoint = e.GetTouchPoint(null);
                centerPoint = touchPoint.Position;
                lastTouchPointOnGridInkCanvasCover = touchPoint.Position;

                if (isStrokeSelectionCloneOn)
                {
                    StrokeCollection strokes = inkCanvas.GetSelectedStrokes();
                    List<UIElement> elementsList = InkCanvasElementsHelper.GetSelectedElements(inkCanvas);
                    isProgramChangeStrokeSelection = true;
                    var elementsInitialHistory = ElementsInitialHistory;
                    ElementsSelectionClone = InkCanvasElementsHelper.CloneSelectedElements(inkCanvas, ref elementsInitialHistory);
                    ElementsInitialHistory = elementsInitialHistory;
                    inkCanvas.Select(new StrokeCollection());
                    StrokesSelectionClone = strokes.Clone();
                    inkCanvas.Strokes.Add(StrokesSelectionClone);
                    inkCanvas.Select(strokes, elementsList);
                    isProgramChangeStrokeSelection = false;
                }
            }
        }

        private void GridInkCanvasSelectionCover_PreviewTouchUp(object sender, TouchEventArgs e)
        {
            dec.Remove(e.TouchDevice.Id);
            if (dec.Count >= 1) return;
            isProgramChangeStrokeSelection = false;
            if (AreNearlyEqual(lastTouchPointOnGridInkCanvasCover, e.GetTouchPoint(null).Position))
            {
                if (lastTouchPointOnGridInkCanvasCover.X < inkCanvas.GetSelectionBounds().Left ||
                    lastTouchPointOnGridInkCanvasCover.Y < inkCanvas.GetSelectionBounds().Top ||
                    lastTouchPointOnGridInkCanvasCover.X > inkCanvas.GetSelectionBounds().Right ||
                    lastTouchPointOnGridInkCanvasCover.Y > inkCanvas.GetSelectionBounds().Bottom)
                {
                    inkCanvas.Select(new StrokeCollection());
                    StrokesSelectionClone = new StrokeCollection();
                    ElementsSelectionClone = new List<UIElement>();
                }
            }
            else if (InkCanvasElementsHelper.IsNotCanvasElementSelected(inkCanvas))
            {
                GridInkCanvasSelectionCover.Visibility = Visibility.Collapsed;
                StrokesSelectionClone = new StrokeCollection();
                ElementsSelectionClone = new List<UIElement>();
            }
            else
            {
                UpdateSelectionClonePrompt();
                GridInkCanvasSelectionCover.Visibility = Visibility.Visible;
                StrokesSelectionClone = new StrokeCollection();
                ElementsSelectionClone = new List<UIElement>();
            }
        }
    }
}
