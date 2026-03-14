using Ink_Canvas.Features.Ink;
using Ink_Canvas.ViewModels;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
    public partial class MainWindow : IInkCanvasHost, IInkGestureHost, IInkPaletteHost, IInkArchiveHost
    {
        private readonly ShapeDrawingSessionState shapeDrawingFallbackState = new();
        private readonly SelectionSessionState selectionFallbackState = new();
        private readonly InkGestureSessionState inkGestureFallbackState = new();
        private readonly InkPaletteSessionState inkPaletteFallbackState = new();
        private readonly InkHistorySessionState inkHistoryFallbackState = new();
        private readonly WhiteboardSessionState whiteboardFallbackState = new();
        private InkRecognitionService inkRecognitionService = null!;
        private InkInteractionCoordinator inkInteractionCoordinator = null!;
        private InkGestureCoordinator inkGestureCoordinator = null!;
        private InkPaletteCoordinator inkPaletteCoordinator = null!;
        private InkArchiveCoordinator inkArchiveCoordinator = null!;
        private InkHistoryCoordinator inkHistoryCoordinator = null!;
        private InkArchiveService inkArchiveService = null!;

        private ShapeDrawingSessionState ShapeDrawingState => inkInteractionCoordinator?.ShapeDrawingState ?? shapeDrawingFallbackState;

        private SelectionSessionState SelectionState => inkInteractionCoordinator?.SelectionState ?? selectionFallbackState;

        private InkGestureSessionState GestureState => inkGestureCoordinator?.GestureState ?? inkGestureFallbackState;

        private InkPaletteSessionState PaletteState => inkPaletteCoordinator?.PaletteState ?? inkPaletteFallbackState;

        private InkHistorySessionState InkHistoryState => inkHistoryCoordinator?.HistoryState ?? inkHistoryFallbackState;

        private WhiteboardSessionState WhiteboardState => inkHistoryCoordinator?.WhiteboardState ?? whiteboardFallbackState;

        private void InitializeInkFeature()
        {
            inkArchiveService = new InkArchiveService(appLogger, inkDependencyCacheService);
            inkHistoryCoordinator = new InkHistoryCoordinator(this, appLogger);
            inkRecognitionService = new InkRecognitionService(appLogger);
            inkInteractionCoordinator = new InkInteractionCoordinator(
                this,
                this,
                mainWindowViewModel.Settings,
                mainWindowViewModel.Shell,
                mainWindowViewModel.Input,
                inkRecognitionService);
            inkGestureCoordinator = new InkGestureCoordinator(this);
            inkPaletteCoordinator = new InkPaletteCoordinator(this);
            inkArchiveCoordinator = new InkArchiveCoordinator(this, inkHistoryCoordinator, inkArchiveService, appLogger, errorHandler);
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
            get => GestureState.IsWaitUntilNextTouchDown;
            set => GestureState.IsWaitUntilNextTouchDown = value;
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
            get => GestureState.IsLastTouchEraser;
            set => GestureState.IsLastTouchEraser = value;
        }

        private List<int> dec => GestureState.ActiveTouchDeviceIds;

        private Point centerPoint
        {
            get => GestureState.CenterPoint;
            set => GestureState.CenterPoint = value;
        }

        private InkCanvasEditingMode lastInkCanvasEditingMode
        {
            get => GestureState.LastInkCanvasEditingMode;
            set => GestureState.LastInkCanvasEditingMode = value;
        }

        private bool isSingleFingerDragMode
        {
            get => GestureState.IsSingleFingerDragMode;
            set => GestureState.IsSingleFingerDragMode = value;
        }

        private StrokeCollection newStrokes
        {
            get => ShapeDrawingState.NewStrokes;
            set => ShapeDrawingState.NewStrokes = value;
        }

        private List<Circle> circles => ShapeDrawingState.Circles;

        private int inkColor
        {
            get => PaletteState.InkColor;
            set => PaletteState.InkColor = value;
        }

        private bool isUselightThemeColor
        {
            get => PaletteState.IsUsingLightThemeColor;
            set => PaletteState.IsUsingLightThemeColor = value;
        }

        private bool isDesktopUselightThemeColor
        {
            get => PaletteState.IsDesktopUsingLightThemeColor;
            set => PaletteState.IsDesktopUsingLightThemeColor = value;
        }

        private int lastDesktopInkColor
        {
            get => PaletteState.LastDesktopInkColor;
            set => PaletteState.LastDesktopInkColor = value;
        }

        private int lastBoardInkColor
        {
            get => PaletteState.LastBoardInkColor;
            set => PaletteState.LastBoardInkColor = value;
        }

        private Dictionary<int, Color> inkColorLightThemeMapping => PaletteState.LightThemeMapping;

        private Dictionary<int, Color> inkColorDarkThemeMapping => PaletteState.DarkThemeMapping;

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

        void IInkCanvasHost.HideSelectionCover() => GridInkCanvasSelectionCover.Visibility = Visibility.Collapsed;

        bool IInkGestureHost.IsLoaded => isLoaded;

        bool IInkGestureHost.IsInMultiTouchMode => isInMultiTouchMode;

        void IInkGestureHost.ToggleMultiTouchMode() => inkCanvasInteractionController.ConfigureMultiTouchMode(
            !isInMultiTouchMode,
            MainWindow_StylusDown,
            MainWindow_StylusMove,
            MainWindow_StylusUp,
            MainWindow_TouchDown,
            Main_Grid_TouchDown);

        void IInkGestureHost.HandleMainWindowTouchDown(TouchEventArgs e, InkGestureSessionState gestureState)
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

        void IInkGestureHost.HandleGridTouchDown(TouchEventArgs e, InkGestureSessionState gestureState)
        {
            if (!isHidingSubPanelsWhenInking)
            {
                isHidingSubPanelsWhenInking = true;
                HideSubPanels();
            }

            if (NeedUpdateIniP())
            {
                iniP = e.GetTouchPoint(inkCanvas).Position;
            }

            if (drawingShapeMode == 9 && !isFirstTouchCuboid)
            {
                MouseTouchMove(iniP);
            }

            inkCanvas.Opacity = 1;
            double boundsWidth = GetTouchBoundWidth(e);
            if ((!IsNearlyZero(Settings.Advanced.TouchMultiplier) || !Settings.Advanced.IsSpecialScreen)
                && boundsWidth > BoundsWidth)
            {
                gestureState.IsLastTouchEraser = true;
                if (drawingShapeMode == 0 && forceEraser)
                {
                    return;
                }

                double threshold = Settings.Startup.IsEnableNibMode
                    ? Settings.Advanced.NibModeBoundsWidthThresholdValue
                    : Settings.Advanced.FingerModeBoundsWidthThresholdValue;
                if (boundsWidth > BoundsWidth * threshold)
                {
                    boundsWidth *= Settings.Startup.IsEnableNibMode
                        ? Settings.Advanced.NibModeBoundsWidthEraserSize
                        : Settings.Advanced.FingerModeBoundsWidthEraserSize;
                    if (Settings.Advanced.IsSpecialScreen)
                    {
                        boundsWidth *= Settings.Advanced.TouchMultiplier;
                    }

                    ApplyCanvasInteractionMode(CanvasInteractionMode.EraseByPoint, boundsWidth);
                }
                else
                {
                    if (IsPresentationSlideShowRunning
                        && inkCanvas.Strokes.Count == 0
                        && Settings.PowerPointSettings.IsEnableFingerGestureSlideShowControl)
                    {
                        gestureState.IsLastTouchEraser = false;
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
                gestureState.IsLastTouchEraser = false;
                inkCanvas.EraserShape = forcePointEraser ? new EllipseStylusShape(50, 50) : new EllipseStylusShape(5, 5);
                if (forceEraser)
                {
                    return;
                }

                ApplyCanvasInteractionMode(CanvasInteractionMode.Ink);
            }
        }

        void IInkGestureHost.HandlePreviewTouchDown(TouchEventArgs e, InkGestureSessionState gestureState)
        {
            dec.Add(e.TouchDevice.Id);
            if (dec.Count == 1)
            {
                TouchPoint touchPoint = e.GetTouchPoint(inkCanvas);
                centerPoint = touchPoint.Position;
                lastTouchDownStrokeCollection = inkCanvas.Strokes.Clone();
            }

            if (dec.Count > 1 || isSingleFingerDragMode || !Settings.Gesture.IsEnableTwoFingerGesture)
            {
                if (isInMultiTouchMode || !Settings.Gesture.IsEnableTwoFingerGesture)
                {
                    return;
                }

                if (inkCanvas.EditingMode != InkCanvasEditingMode.None && inkCanvas.EditingMode != InkCanvasEditingMode.Select)
                {
                    lastInkCanvasEditingMode = inkCanvas.EditingMode;
                    ApplyCanvasInteractionMode(CanvasInteractionMode.Suspended);
                }
            }
        }

        void IInkGestureHost.HandlePreviewTouchUp(TouchEventArgs e, InkGestureSessionState gestureState)
        {
            if (dec.Count > 1 && inkCanvas.EditingMode == InkCanvasEditingMode.None)
            {
                inkCanvas.EditingMode = lastInkCanvasEditingMode;
            }

            dec.Remove(e.TouchDevice.Id);
            inkCanvas.Opacity = 1;
            if (dec.Count == 0)
            {
                if (lastTouchDownStrokeCollection.Count != inkCanvas.Strokes.Count
                    && !(drawingShapeMode == 9 && !isFirstTouchCuboid))
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

        void IInkGestureHost.HandleManipulationCompleted(ManipulationCompletedEventArgs e, InkGestureSessionState gestureState)
        {
            if (e.Manipulators.Count() == 0)
            {
                if (forceEraser)
                {
                    return;
                }

                ApplyCanvasInteractionMode(CanvasInteractionMode.Ink);
            }
        }

        void IInkGestureHost.HandleManipulationDelta(ManipulationDeltaEventArgs e, InkGestureSessionState gestureState)
        {
            if (isInMultiTouchMode || !Settings.Gesture.IsEnableTwoFingerGesture)
            {
                return;
            }

            if (!((dec.Count >= 2 && (Settings.PowerPointSettings.IsEnableTwoFingerGestureInPresentationMode || !IsPresentationSlideShowRunning))
                || isSingleFingerDragMode))
            {
                return;
            }

            Matrix matrix = new();
            ManipulationDelta manipulationDelta = e.DeltaManipulation;
            Vector translation = manipulationDelta.Translation;
            if (Settings.Gesture.IsEnableTwoFingerGestureTranslateOrRotation)
            {
                double rotation = manipulationDelta.Rotation;
                Vector scale = manipulationDelta.Scale;
                Point center = GetMatrixTransformCenterPoint(e.ManipulationOrigin, e.Source as FrameworkElement);
                if (Settings.Gesture.IsEnableTwoFingerZoom)
                {
                    matrix.ScaleAt(scale.X, scale.Y, center.X, center.Y);
                }

                if (Settings.Gesture.IsEnableTwoFingerRotation)
                {
                    matrix.RotateAt(rotation, center.X, center.Y);
                }

                if (Settings.Gesture.IsEnableTwoFingerTranslate)
                {
                    matrix.Translate(translation.X, translation.Y);
                }

                List<UIElement> elements = InkCanvasElementsHelper.GetAllElements(inkCanvas);
                foreach (UIElement element in elements)
                {
                    ApplyElementMatrixTransform(element, matrix);
                }
            }

            if (Settings.Gesture.IsEnableTwoFingerZoom)
            {
                foreach (Stroke stroke in inkCanvas.Strokes)
                {
                    stroke.Transform(matrix, false);
                    ScaleStrokeDrawingAttributes(stroke, manipulationDelta.Scale.X, manipulationDelta.Scale.Y);
                }
            }
            else
            {
                foreach (Stroke stroke in inkCanvas.Strokes)
                {
                    stroke.Transform(matrix, false);
                }
            }

            foreach (Circle circle in circles)
            {
                circle.R = GetDistance(circle.Stroke.StylusPoints[0].ToPoint(), circle.Stroke.StylusPoints[circle.Stroke.StylusPoints.Count / 2].ToPoint()) / 2;
                circle.Centroid = new Point(
                    (circle.Stroke.StylusPoints[0].X + circle.Stroke.StylusPoints[circle.Stroke.StylusPoints.Count / 2].X) / 2,
                    (circle.Stroke.StylusPoints[0].Y + circle.Stroke.StylusPoints[circle.Stroke.StylusPoints.Count / 2].Y) / 2);
            }
        }

        bool IInkPaletteHost.IsLoaded => isLoaded;

        void IInkPaletteHost.ApplyInkWidthChange(double value, bool fromBoardSlider)
        {
            if (fromBoardSlider)
            {
                InkWidthSlider.Value = value;
            }
            else
            {
                BoardInkWidthSlider.Value = value;
            }

            Settings.Canvas.InkWidth = value / 2;
            if (inkColor > 100)
            {
                drawingAttributes.Height = 30 + value;
                drawingAttributes.Width = 30 + value;
            }
            else
            {
                drawingAttributes.Height = value / 2;
                drawingAttributes.Width = value / 2;
            }

            SaveSettingsToFile();
        }

        void IInkPaletteHost.ApplyInkAlphaChange(double value, bool fromBoardSlider)
        {
            if (fromBoardSlider)
            {
                InkAlphaSlider.Value = value;
            }
            else
            {
                BoardInkAlphaSlider.Value = value;
            }

            drawingAttributes.Height = 20;
            drawingAttributes.Width = 5;
            Settings.Canvas.InkAlpha = (int)value;
            SaveSettingsToFile();
            CheckColorTheme();
        }

        void IInkPaletteHost.ApplyPaletteColorSelection(int colorIndex) => CheckLastColor(colorIndex);

        void IInkPaletteHost.ApplyPaletteTheme(bool changeColorTheme) => CheckColorTheme(changeColorTheme);

        void IInkPaletteHost.ToggleBoardBackgroundColor()
        {
            if (!isLoaded)
            {
                return;
            }

            Settings.Canvas.UsingWhiteboard = !Settings.Canvas.UsingWhiteboard;
            SaveSettingsToFile();
            if (Settings.Canvas.UsingWhiteboard && inkColor == 5)
            {
                lastBoardInkColor = 0;
            }
            else if (inkColor == 0)
            {
                lastBoardInkColor = 5;
            }

            ComboBoxTheme_SelectionChanged(null, null);
            CheckColorTheme(true);
            if (IsNearlyEqual(BoardPen.Opacity, 1.0))
            {
                BoardPen.Background = (Brush)Application.Current.FindResource("BoardBarBackground");
            }
            if (IsNearlyEqual(BoardEraser.Opacity, 1.0))
            {
                BoardEraser.Background = (Brush)Application.Current.FindResource("BoardBarBackground");
            }
            if (IsNearlyEqual(BoardSelect.Opacity, 1.0))
            {
                BoardSelect.Background = (Brush)Application.Current.FindResource("BoardBarBackground");
            }
            if (IsNearlyEqual(BoardEraserByStrokes.Opacity, 1.0))
            {
                BoardEraserByStrokes.Background = (Brush)Application.Current.FindResource("BoardBarBackground");
            }
        }

        Ink_Canvas.Settings IInkArchiveHost.Settings => Settings;

        InkCanvas IInkArchiveHost.InkCanvas => inkCanvas;

        bool IInkArchiveHost.IsDesktopAnnotationMode => ShellViewModel.IsDesktopAnnotationMode;

        bool IInkArchiveHost.IsBlackboardMode => ShellViewModel.IsBlackboardMode;

        int IInkArchiveHost.CurrentWhiteboardIndex => CurrentWhiteboardIndex;

        string? IInkArchiveHost.ShowOpenArchiveDialog()
        {
            OpenFileDialog openFileDialog = new()
            {
                InitialDirectory = Settings.Automation.AutoSavedStrokesLocation,
                Title = "打开墨迹文件",
                Filter = "Ink Canvas Files (*.icart;*.icstk)|*.icart;*.icstk|Ink Canvas Artistry Files (*.icart)|*.icart|Ink Canvas Stroke Files (*.icstk)|*.icstk"
            };

            return openFileDialog.ShowDialog() == true ? openFileDialog.FileName : null;
        }

        void IInkArchiveHost.ShowArchiveNotification(string message) => ShowNotificationAsync(message);

        void IInkArchiveHost.ReplaceCanvasContent(StrokeCollection strokes, IReadOnlyList<UIElement> elements)
        {
            inkCanvas.Strokes.Add(strokes);
            inkCanvas.Children.Clear();
            foreach (UIElement element in elements)
            {
                inkCanvas.Children.Add(element);
            }
        }

        void IInkArchiveHost.ClearCanvasForArchiveImport()
        {
            ClearStrokes(true);
            inkCanvas.Children.Clear();
        }

        void IInkArchiveHost.EnsureCanvasVisibleAfterArchiveImport()
        {
            if (inkCanvas.Visibility != Visibility.Visible)
            {
            if (toolbarExperienceCoordinator != null)
            {
                taskGuard.Forget(
                    toolbarExperienceCoordinator.HandleCursorRequestedAsync(),
                    new AppErrorContext(nameof(MainWindow), "RestoreCursorToolAfterArchiveImport"));
            }
            }
        }

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
                StrokeCollection strokes = inkCanvas.GetSelectedStrokes().Clone();
                List<UIElement> elements = InkCanvasElementsHelper.GetSelectedElementsCloned(inkCanvas);
                inkCanvas.Select(new StrokeCollection());
                EnterBlackboardSession();
                inkCanvas.Strokes.Add(strokes);
                foreach (UIElement element in elements)
                {
                    inkCanvas.Children.Add(element);
                    inkHistoryCoordinator?.CommitElementInsert(element);
                }
                return;
            }

            StrokeCollection selectedStrokes = inkCanvas.GetSelectedStrokes().Clone();
            List<UIElement> selectedElements = InkCanvasElementsHelper.GetSelectedElementsCloned(inkCanvas);
            inkCanvas.Select(new StrokeCollection());
            BtnWhiteBoardAdd_Click(null, null);
            inkCanvas.Strokes.Add(selectedStrokes);
            foreach (UIElement element in selectedElements)
            {
                inkCanvas.Children.Add(element);
                inkHistoryCoordinator?.CommitElementInsert(element);
            }
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
            inkHistoryCoordinator?.BackupCurrentStrokes();
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
