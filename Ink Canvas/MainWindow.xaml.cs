using Ink_Canvas.Features.Automation.Services;
using Ink_Canvas.Features.Ink.Services;
using Ink_Canvas.Services.Logging;
using iNKORE.UI.WPF.Modern;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Media;
using File = System.IO.File;
using MessageBox = System.Windows.MessageBox;

namespace Ink_Canvas
{
    public partial class MainWindow : Window
    {
        private readonly FileAppLogger appLogger;
        private readonly IAppLogger mainWindowLogger;
        private readonly AppErrorHandler errorHandler;
        private readonly TaskGuard taskGuard;
        private readonly UiDispatchGuard uiDispatchGuard;
        private readonly AutoUpdateHelper autoUpdateHelper;
        private readonly DelAutoSavedFiles autoSavedFilesCleaner;
        private readonly InkDependencyCacheService inkDependencyCacheService;

        #region Window Initialization

        public MainWindow()
        {
            App app = Application.Current as App
                ?? throw new InvalidOperationException("App services are not available.");
            appLogger = app.Logger;
            mainWindowLogger = appLogger.ForCategory(nameof(MainWindow));
            errorHandler = app.ErrorHandler;
            taskGuard = app.TaskGuard;
            uiDispatchGuard = app.UiDispatchGuard;
            autoUpdateHelper = new AutoUpdateHelper(appLogger);
            autoSavedFilesCleaner = new DelAutoSavedFiles(appLogger);
            inkDependencyCacheService = new InkDependencyCacheService(appLogger);

            /*
                处于画板模式内：Topmost == false / Shell.IsBlackboardMode
                处于 PPT 放映内：Presentation.IsSlideShowRunning
            */
            InitializeComponent();
            errorHandler.RegisterNotificationSink(message => ShowNotificationAsync(message));
            InitializeMvvm();

            BlackboardLeftSide.Visibility = Visibility.Collapsed;
            BlackboardCenterSide.Visibility = Visibility.Collapsed;
            BlackboardRightSide.Visibility = Visibility.Collapsed;

            BorderTools.Visibility = Visibility.Collapsed;
            BorderSettings.Visibility = Visibility.Collapsed;

            BtnPPTSlideShowEnd.Visibility = Visibility.Collapsed;
            PPTNavigationBottomLeft.Visibility = Visibility.Collapsed;
            PPTNavigationBottomRight.Visibility = Visibility.Collapsed;
            PPTNavigationSidesLeft.Visibility = Visibility.Collapsed;
            PPTNavigationSidesRight.Visibility = Visibility.Collapsed;

            BorderSettings.Margin = new Thickness(0, 150, 0, 150);

            TwoFingerGestureBorder.Visibility = Visibility.Collapsed;
            BoardTwoFingerGestureBorder.Visibility = Visibility.Collapsed;
            BorderDrawShape.Visibility = Visibility.Collapsed;
            BoardBorderDrawShape.Visibility = Visibility.Collapsed;

            GridInkCanvasSelectionCover.Visibility = Visibility.Collapsed;

            ViewboxFloatingBar.Margin = new Thickness((SystemParameters.WorkArea.Width - 284) / 2, SystemParameters.WorkArea.Height - 60, -2000, -200);
            ViewboxFloatingBarMarginAnimation();

            InitTimers();
            timeMachine.OnRedoStateChanged += TimeMachine_OnRedoStateChanged;
            timeMachine.OnUndoStateChanged += TimeMachine_OnUndoStateChanged;
            inkCanvas.Strokes.StrokesChanged += StrokesOnStrokesChanged;

            Microsoft.Win32.SystemEvents.UserPreferenceChanged += SystemEvents_UserPreferenceChanged;
            try
            {
                if (File.Exists("SpecialVersion.ini")) SpecialVersionResetToSuggestion_Click();
            }
            catch (IOException ex)
            {
                mainWindowLogger.Error(ex, "MainWindow Init | Failed to load SpecialVersion.ini");
            }
            catch (UnauthorizedAccessException ex)
            {
                mainWindowLogger.Error(ex, "MainWindow Init | Failed to load SpecialVersion.ini");
            }

            CheckColorTheme(true);
        }

        #endregion

        #region Ink Canvas Functions

        DrawingAttributes drawingAttributes;
        private void loadPenCanvas()
        {
            drawingAttributes = inkCanvas.DefaultDrawingAttributes;
            drawingAttributes.Color = Colors.Red;
            drawingAttributes.Height = 2.5;
            drawingAttributes.Width = 2.5;

            inkCanvas.EditingMode = InkCanvasEditingMode.Ink;
            inkCanvas.Gesture += InkCanvas_Gesture;
        }

        private void InkCanvas_Gesture(object sender, InkCanvasGestureEventArgs e)
        {
            ReadOnlyCollection<GestureRecognitionResult> gestures = e.GetGestureRecognitionResults();
            foreach (GestureRecognitionResult gest in gestures)
            {
                if (!IsPresentationSlideShowRunning)
                {
                    continue;
                }

                if (gest.ApplicationGesture == ApplicationGesture.Left)
                {
                    BtnPPTSlidesDown_Click(null, null);
                }

                if (gest.ApplicationGesture == ApplicationGesture.Right)
                {
                    BtnPPTSlidesUp_Click(null, null);
                }
            }
        }

        private void inkCanvas_EditingModeChanged(object sender, RoutedEventArgs e)
        {
            var inkCanvas1 = sender as InkCanvas;
            if (inkCanvas1 == null) return;
            SyncInputInteractionMode();
            if (Settings.Canvas.IsShowCursor)
            {
                if (inkCanvas1.EditingMode == InkCanvasEditingMode.Ink || drawingShapeMode != 0)
                {
                    inkCanvas1.ForceCursor = true;
                }
                else
                {
                    inkCanvas1.ForceCursor = false;
                }
            }
            else
            {
                inkCanvas1.ForceCursor = false;
            }
            if (inkCanvas1.EditingMode == InkCanvasEditingMode.Ink) forcePointEraser = !forcePointEraser;
        }

        #endregion Ink Canvas Functions

        #region Definations and Loading

        public static Settings Settings { get; private set; } = new();
        public static readonly string settingsFileName = "Settings.json";
        bool isLoaded = false;

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            loadPenCanvas();
            LoadSettings(true);

            ThemeManager.Current.ApplicationTheme = ApplicationTheme.Light;
            SystemEvents_UserPreferenceChanged(null, null);

            AppVersionTextBlock.Text = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "Unknown";
            mainWindowLogger.Event("Ink Canvas Loaded");
            isLoaded = true;
            RegisterGlobalHotkeys();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            mainWindowLogger.Event("Ink Canvas closing");
            if (!closeIsFromButton && Settings.Advanced.IsSecondConfimeWhenShutdownApp)
            {
                e.Cancel = true;
                if (MessageBox.Show("是否继续关闭 Ink Canvas 画板，这将丢失当前未保存的工作。", "Ink Canvas 画板", MessageBoxButton.OKCancel, MessageBoxImage.Warning) == MessageBoxResult.OK)
                {
                    if (MessageBox.Show("真的狠心关闭 Ink Canvas 画板吗？", "Ink Canvas 画板", MessageBoxButton.OKCancel, MessageBoxImage.Error) == MessageBoxResult.OK)
                    {
                        if (MessageBox.Show("是否取消关闭 Ink Canvas 画板？", "Ink Canvas 画板", MessageBoxButton.OKCancel, MessageBoxImage.Error) != MessageBoxResult.OK)
                        {
                            e.Cancel = false;
                        }
                    }
                }
            }
            if (e.Cancel)
            {
                mainWindowLogger.Event("Ink Canvas closing cancelled");
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            StopPresentationMonitoring();
            DisposeAutomationControllers();
            inkEngineCoordinator?.Dispose();
            inkDependencyCacheService.CleanupCurrentSessions();
            mainWindowLogger.Event("Ink Canvas closed");
        }

        #endregion Definations and Loading
    }
}
