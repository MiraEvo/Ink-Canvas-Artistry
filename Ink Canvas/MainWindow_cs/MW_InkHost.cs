using Ink_Canvas.Features.Ink;
using Ink_Canvas.Helpers;
using Ink_Canvas.ViewModels;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using Point = System.Windows.Point;

namespace Ink_Canvas
{
    public partial class MainWindow : IInkCanvasHost
    {
        private readonly ShapeDrawingSessionState shapeDrawingFallbackState = new();
        private readonly SelectionSessionState selectionFallbackState = new();
        private InkRecognitionService inkRecognitionService = null!;
        private InkInteractionCoordinator inkInteractionCoordinator = null!;

        private ShapeDrawingSessionState ShapeDrawingState => inkInteractionCoordinator?.ShapeDrawingState ?? shapeDrawingFallbackState;

        private SelectionSessionState SelectionState => inkInteractionCoordinator?.SelectionState ?? selectionFallbackState;

        private void InitializeInkFeature()
        {
            inkRecognitionService = new InkRecognitionService();
            inkInteractionCoordinator = new InkInteractionCoordinator(
                this,
                mainWindowViewModel.Settings,
                mainWindowViewModel.Shell,
                mainWindowViewModel.Input,
                inkRecognitionService);
        }

        private bool isLongPressSelected
        {
            get => ShapeDrawingState.IsLongPressSelected;
            set => ShapeDrawingState.IsLongPressSelected = value;
        }

        private object? lastMouseDownSender
        {
            get => ShapeDrawingState.LastMouseDownSender;
            set => ShapeDrawingState.LastMouseDownSender = value;
        }

        private DateTime lastMouseDownTime
        {
            get => ShapeDrawingState.LastMouseDownTime;
            set => ShapeDrawingState.LastMouseDownTime = value;
        }

        private int drawMultiStepShapeCurrentStep
        {
            get => ShapeDrawingState.DrawMultiStepShapeCurrentStep;
            set => ShapeDrawingState.DrawMultiStepShapeCurrentStep = value;
        }

        private StrokeCollection drawMultiStepShapeSpecialStrokeCollection
        {
            get => ShapeDrawingState.DrawMultiStepShapeSpecialStrokeCollection;
            set => ShapeDrawingState.DrawMultiStepShapeSpecialStrokeCollection = value;
        }

        private double drawMultiStepShapeSpecialParameter3
        {
            get => ShapeDrawingState.DrawMultiStepShapeSpecialParameter3;
            set => ShapeDrawingState.DrawMultiStepShapeSpecialParameter3 = value;
        }

        private bool isFirstTouchCuboid
        {
            get => ShapeDrawingState.IsFirstTouchCuboid;
            set => ShapeDrawingState.IsFirstTouchCuboid = value;
        }

        private Point CuboidFrontRectIniP
        {
            get => ShapeDrawingState.CuboidFrontRectInitialPoint;
            set => ShapeDrawingState.CuboidFrontRectInitialPoint = value;
        }

        private Point CuboidFrontRectEndP
        {
            get => ShapeDrawingState.CuboidFrontRectEndPoint;
            set => ShapeDrawingState.CuboidFrontRectEndPoint = value;
        }

        private Stroke? lastTempStroke
        {
            get => ShapeDrawingState.LastTempStroke;
            set => ShapeDrawingState.LastTempStroke = value;
        }

        private StrokeCollection lastTempStrokeCollection
        {
            get => ShapeDrawingState.LastTempStrokeCollection;
            set => ShapeDrawingState.LastTempStrokeCollection = value;
        }

        private bool isWaitUntilNextTouchDown
        {
            get => ShapeDrawingState.IsWaitUntilNextTouchDown;
            set => ShapeDrawingState.IsWaitUntilNextTouchDown = value;
        }

        private bool isMouseDown
        {
            get => ShapeDrawingState.IsMouseDown;
            set => ShapeDrawingState.IsMouseDown = value;
        }

        private int lastTouchDownTime
        {
            get => ShapeDrawingState.LastTouchDownTime;
            set => ShapeDrawingState.LastTouchDownTime = value;
        }

        private int lastTouchUpTime
        {
            get => ShapeDrawingState.LastTouchUpTime;
            set => ShapeDrawingState.LastTouchUpTime = value;
        }

        private Point iniP
        {
            get => ShapeDrawingState.InitialPoint;
            set => ShapeDrawingState.InitialPoint = value;
        }

        private bool isLastTouchEraser
        {
            get => ShapeDrawingState.IsLastTouchEraser;
            set => ShapeDrawingState.IsLastTouchEraser = value;
        }

        private List<int> dec => ShapeDrawingState.ActiveTouchDeviceIds;

        private Point centerPoint
        {
            get => ShapeDrawingState.CenterPoint;
            set => ShapeDrawingState.CenterPoint = value;
        }

        private InkCanvasEditingMode lastInkCanvasEditingMode
        {
            get => ShapeDrawingState.LastInkCanvasEditingMode;
            set => ShapeDrawingState.LastInkCanvasEditingMode = value;
        }

        private bool isSingleFingerDragMode
        {
            get => ShapeDrawingState.IsSingleFingerDragMode;
            set => ShapeDrawingState.IsSingleFingerDragMode = value;
        }

        private StrokeCollection newStrokes
        {
            get => ShapeDrawingState.NewStrokes;
            set => ShapeDrawingState.NewStrokes = value;
        }

        private List<Circle> circles => ShapeDrawingState.Circles;

        private object? lastBorderMouseDownObject
        {
            get => SelectionState.LastBorderMouseDownObject;
            set => SelectionState.LastBorderMouseDownObject = value;
        }

        private bool isStrokeSelectionCloneOn
        {
            get => SelectionState.IsStrokeSelectionCloneOn;
            set => SelectionState.IsStrokeSelectionCloneOn = value;
        }

        private bool isGridInkCanvasSelectionCoverMouseDown
        {
            get => SelectionState.IsGridInkCanvasSelectionCoverMouseDown;
            set => SelectionState.IsGridInkCanvasSelectionCoverMouseDown = value;
        }

        private Point lastMousePoint
        {
            get => SelectionState.LastMousePoint;
            set => SelectionState.LastMousePoint = value;
        }

        private bool isProgramChangeStrokeSelection
        {
            get => SelectionState.IsProgramChangeStrokeSelection;
            set => SelectionState.IsProgramChangeStrokeSelection = value;
        }

        private StrokeCollection StrokesSelectionClone
        {
            get => SelectionState.StrokesSelectionClone;
            set => SelectionState.StrokesSelectionClone = value;
        }

        private List<UIElement> ElementsSelectionClone
        {
            get => SelectionState.ElementsSelectionClone;
            set => SelectionState.ElementsSelectionClone = value;
        }

        private Point lastTouchPointOnGridInkCanvasCover
        {
            get => SelectionState.LastTouchPointOnGridInkCanvasCover;
            set => SelectionState.LastTouchPointOnGridInkCanvasCover = value;
        }

        private double BorderStrokeSelectionControlWidth
        {
            get => SelectionState.BorderStrokeSelectionControlWidth;
            set => SelectionState.BorderStrokeSelectionControlWidth = value;
        }

        private double BorderStrokeSelectionControlHeight
        {
            get => SelectionState.BorderStrokeSelectionControlHeight;
            set => SelectionState.BorderStrokeSelectionControlHeight = value;
        }

        InkCanvas IInkCanvasHost.InkCanvas => inkCanvas;

        Settings IInkCanvasHost.Settings => Settings;

        bool IInkCanvasHost.IsDesktopAnnotationMode => ShellViewModel.IsDesktopAnnotationMode;

        bool IInkCanvasHost.IsInMultiTouchMode => isInMultiTouchMode;

        int IInkCanvasHost.InkColor => inkColor;

        bool IInkCanvasHost.IsShapePanelAutoHideEnabled => ToggleSwitchDrawShapeBorderAutoHide.IsOn;

        void IInkCanvasHost.SetMultiTouchModeEnabled(bool enabled) => SettingsViewModel.SetIsEnableMultiTouchMode(enabled, false);

        void IInkCanvasHost.BeginShapeDrawing(ShapeToolKind tool) => BeginShapeDrawing(tool);

        void IInkCanvasHost.EndShapeDrawing(bool restoreInkMode) => ExitShapeDrawingMode(restoreInkMode);

        void IInkCanvasHost.SetToolModeToPen() => ShellViewModel.SetToolMode(ToolMode.Pen, false);

        void IInkCanvasHost.SetCanvasManipulationEnabled(bool enabled) => inkCanvas.IsManipulationEnabled = enabled;

        void IInkCanvasHost.CancelSingleFingerDragMode() => CancelSingleFingerDragMode();

        void IInkCanvasHost.ToggleSingleFingerDragMode() => BtnFingerDragMode_Click(null, null);

        bool IInkCanvasHost.TryGetLongPressShapeTool(object sender, out ShapeToolKind tool) => TryGetLongPressShapeTool(sender, out tool);

        void IInkCanvasHost.AnimateLongPressPreview(UIElement shapeButton) =>
            shapeButton.BeginAnimation(OpacityProperty, new DoubleAnimation(1, 0.3, new Duration(TimeSpan.FromMilliseconds(100))));

        void IInkCanvasHost.ResetLongPressPreview(UIElement previewElement) =>
            previewElement.BeginAnimation(OpacityProperty, new DoubleAnimation(1, 1, new Duration(TimeSpan.Zero)));

        void IInkCanvasHost.CollapseShapePanel(bool isLongPressSelected) => CollapseBorderDrawShape(isLongPressSelected);

        void IInkCanvasHost.DrawShapePromptToPen() => DrawShapePromptToPen();

        void IInkCanvasHost.InitializeCuboidDrawing() => InitializeCuboidDrawing();

        void IInkCanvasHost.UpdateSelectionCloneToggleVisual(bool enabled) => UpdateSelectionCloneToggleVisual(enabled);

        void IInkCanvasHost.CommitSelectionCloneToBoardOrNewPage() => CommitSelectionCloneToBoardOrNewPage();

        void IInkCanvasHost.DeleteSelection(object sender, RoutedEventArgs e) => SymbolIconDelete_MouseUp(sender, e);

        void IInkCanvasHost.ChangeSelectedStrokeThickness(double multiplier) => ChangeSelectedStrokeThickness(multiplier);

        void IInkCanvasHost.RestoreSelectedStrokeThickness() => RestoreSelectedStrokeThickness();

        void IInkCanvasHost.SaveSelectionToImage() => SaveSelectionToImage();

        void IInkCanvasHost.ApplySelectionMatrixTransform(int type) => MatrixTransform(type);

        void IInkCanvasHost.BackupCurrentStrokes() => BackupCurrentStrokes();

        void IInkCanvasHost.SetCommitReasonShapeRecognition() => _currentCommitType = CommitReason.ShapeRecognition;

        void IInkCanvasHost.SetCommitReasonUserInput() => _currentCommitType = CommitReason.UserInput;

        void IInkCanvasHost.HideSelectionCover() => GridInkCanvasSelectionCover.Visibility = Visibility.Collapsed;

        private void UpdateSelectionCloneToggleVisual(bool enabled)
        {
            IconStrokeSelectionClone.SetResourceReference(
                TextBlock.ForegroundProperty,
                enabled ? "FloatBarBackground" : "FloatBarForeground");
        }

        private void CommitSelectionCloneToBoardOrNewPage()
        {
            if (ShellViewModel.IsDesktopAnnotationMode)
            {
                StrokeCollection strokes = inkCanvas.GetSelectedStrokes();
                List<UIElement> elements = InkCanvasElementsHelper.GetSelectedElementsCloned(inkCanvas);
                inkCanvas.Select(new StrokeCollection());
                strokes = strokes.Clone();
                EnterBlackboardSession();
                inkCanvas.Strokes.Add(strokes);
                InkCanvasElementsHelper.AddElements(inkCanvas, elements, timeMachine);
                return;
            }

            StrokeCollection selectedStrokes = inkCanvas.GetSelectedStrokes();
            List<UIElement> selectedElements = InkCanvasElementsHelper.GetSelectedElementsCloned(inkCanvas);
            inkCanvas.Select(new StrokeCollection());
            selectedStrokes = selectedStrokes.Clone();
            BtnWhiteBoardAdd_Click(null, null);
            inkCanvas.Strokes.Add(selectedStrokes);
            InkCanvasElementsHelper.AddElements(inkCanvas, selectedElements, timeMachine);
        }

        private void RestoreSelectedStrokeThickness()
        {
            foreach (Stroke stroke in inkCanvas.GetSelectedStrokes())
            {
                stroke.DrawingAttributes.Width = inkCanvas.DefaultDrawingAttributes.Width;
                stroke.DrawingAttributes.Height = inkCanvas.DefaultDrawingAttributes.Height;
            }
        }

        private void SaveSelectionToImage()
        {
            StrokeCollection selectedStrokes = inkCanvas.GetSelectedStrokes();
            var selectedElements = inkCanvas.GetSelectedElements();

            if (selectedStrokes.Count == 0 && selectedElements.Count == 0)
            {
                return;
            }

            Rect bounds = inkCanvas.GetSelectionBounds();
            double width = bounds.Width + 10;
            double height = bounds.Height + 10;
            RenderTargetBitmap renderTarget = new(
                (int)Math.Ceiling(width),
                (int)Math.Ceiling(height),
                96,
                96,
                PixelFormats.Pbgra32);

            DrawingVisual drawingVisual = new();
            using (DrawingContext drawingContext = drawingVisual.RenderOpen())
            {
                drawingContext.PushTransform(new TranslateTransform(-bounds.X, -bounds.Y));

                foreach (Stroke stroke in selectedStrokes)
                {
                    stroke.Draw(drawingContext);
                }

                foreach (UIElement element in selectedElements)
                {
                    VisualBrush visualBrush = new(element);
                    Rect elementBounds = new(element.RenderSize);
                    Transform renderTransform = element.RenderTransform;
                    if (renderTransform != null)
                    {
                        drawingContext.PushTransform(renderTransform);
                        drawingContext.DrawRectangle(visualBrush, null, elementBounds);
                        drawingContext.Pop();
                    }
                    else
                    {
                        drawingContext.DrawRectangle(visualBrush, null, elementBounds);
                    }
                }
            }

            renderTarget.Render(drawingVisual);

            SaveFileDialog saveFileDialog = new()
            {
                Filter = "PNG Images|*.png",
                Title = "Save Selected Ink as PNG",
                FileName = DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss-fff")
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                PngBitmapEncoder encoder = new();
                encoder.Frames.Add(BitmapFrame.Create(renderTarget));
                using FileStream fileStream = new(saveFileDialog.FileName, FileMode.Create);
                encoder.Save(fileStream);
            }
        }

        private void BackupCurrentStrokes()
        {
            lastTouchDownStrokeCollection = inkCanvas.Strokes.Clone();
            int whiteboardIndex = CurrentWhiteboardIndex;
            if (ShellViewModel.IsDesktopAnnotationMode)
            {
                whiteboardIndex = 0;
            }

            strokeCollections[whiteboardIndex] = lastTouchDownStrokeCollection;
        }

        public double GetDistance(Point point1, Point point2)
        {
            return Math.Sqrt((point1.X - point2.X) * (point1.X - point2.X) + (point1.Y - point2.Y) * (point1.Y - point2.Y));
        }

        public double GetPointSpeed(Point point1, Point point2, Point point3)
        {
            return (Math.Sqrt((point1.X - point2.X) * (point1.X - point2.X) + (point1.Y - point2.Y) * (point1.Y - point2.Y))
                + Math.Sqrt((point3.X - point2.X) * (point3.X - point2.X) + (point3.Y - point2.Y) * (point3.Y - point2.Y)))
                / 20;
        }

        public Point GetCenterPoint(Point point1, Point point2)
        {
            return new Point((point1.X + point2.X) / 2, (point1.Y + point2.Y) / 2);
        }

        public StylusPoint GetCenterPoint(StylusPoint point1, StylusPoint point2)
        {
            return new StylusPoint((point1.X + point2.X) / 2, (point1.Y + point2.Y) / 2);
        }
    }
}
