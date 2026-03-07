using Ink_Canvas.Helpers;
using Ink_Canvas.ViewModels;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using Application = System.Windows.Application;
using File = System.IO.File;

namespace Ink_Canvas
{
    public partial class MainWindow : Window
    {
        private void PresentationSessionController_PresentationConnected()
        {
            string presentationName = CurrentPresentationName;
            int slideCount = PresentationViewModel.SlideCount;
            if (string.IsNullOrWhiteSpace(presentationName) || slideCount <= 0)
            {
                return;
            }

            string folderPath = GetPresentationStoragePath(presentationName, slideCount);

            if (Settings.PowerPointSettings.IsNotifyPreviousPage)
            {
                QueuePptUiAction(() =>
                {
                    if (TryReadPresentationPosition(folderPath, out int page) && page > 0)
                    {
                        new YesOrNoNotificationWindow(
                            $"上次播放到了第 {page} 页, 是否立即跳转",
                            () => presentationSessionController?.TryGoToSlide(page))
                            .ShowDialog();
                    }
                });
            }

            if (Settings.PowerPointSettings.IsNotifyHiddenPage && presentationSessionController?.HasHiddenSlides() == true)
            {
                QueuePptUiAction(() =>
                {
                    if (!IsShowingRestoreHiddenSlidesWindow)
                    {
                        IsShowingRestoreHiddenSlidesWindow = true;
                        new YesOrNoNotificationWindow(
                            "检测到此演示文档中包含隐藏的幻灯片，是否取消隐藏？",
                            () => presentationSessionController?.TryUnhideHiddenSlides())
                            .ShowDialog();
                    }
                });
            }

            if (Settings.PowerPointSettings.IsNotifyAutoPlayPresentation
                && !IsPresentationSlideShowRunning
                && presentationSessionController?.HasAutomaticAdvance() == true)
            {
                QueuePptUiAction(() =>
                {
                    new YesOrNoNotificationWindow(
                        "检测到此演示文档中自动播放或排练计时已经启用，可能导致幻灯片自动翻页，是否取消？",
                        () => presentationSessionController?.TryDisableAutomaticAdvance())
                        .ShowDialog();
                });

                presentationSessionController?.TryDisableAutomaticAdvance();
            }
        }

        private void PptApplication_PresentationClose()
        {
            RunPptUiAction(ResetPresentationNavigationState);
        }

        //bool isPresentationHaveBlackSpace = false;
        private string pptName = null;
        private int currentShowPosition = -1;

        private string GetPresentationStoragePath(string presentationName, int slideCount)
        {
            return Path.Combine(
                Settings.Automation.AutoSavedStrokesLocation,
                "Auto Saved - Presentations",
                $"{presentationName}_{slideCount}");
        }

        private bool TryReadPresentationPosition(string folderPath, out int page)
        {
            page = 0;
            string positionFilePath = Path.Combine(folderPath, "Position");

            try
            {
                return File.Exists(positionFilePath)
                    && int.TryParse(File.ReadAllText(positionFilePath), out page)
                    && page > 0;
            }
            catch (IOException ex)
            {
                LogHelper.WriteLogToFile(ex, "PowerPoint | Failed to read saved slide position");
            }
            catch (UnauthorizedAccessException ex)
            {
                LogHelper.WriteLogToFile(ex, "PowerPoint | Access denied while reading saved slide position");
            }
            catch (ArgumentException ex)
            {
                LogHelper.WriteLogToFile(ex, "PowerPoint | Failed to resolve saved slide position path");
            }

            return false;
        }

        private void QueuePptUiAction(Action action)
        {
            var dispatcher = Application.Current?.Dispatcher;
            if (dispatcher == null || dispatcher.CheckAccess())
            {
                ExecutePptUiAction(action);
                return;
            }

            try
            {
                dispatcher.BeginInvoke((Action)(() => ExecutePptUiAction(action)));
            }
            catch (InvalidOperationException ex)
            {
                LogHelper.WriteLogToFile(ex, "PowerPoint | Failed to queue UI action");
            }
        }

        private void RunPptUiAction(Action action)
        {
            var dispatcher = Application.Current?.Dispatcher;
            if (dispatcher == null || dispatcher.CheckAccess())
            {
                ExecutePptUiAction(action);
                return;
            }

            try
            {
                dispatcher.Invoke((Action)(() => ExecutePptUiAction(action)));
            }
            catch (InvalidOperationException ex)
            {
                LogHelper.WriteLogToFile(ex, "PowerPoint | Failed to run UI action");
            }
        }

        private void ExecutePptUiAction(Action action)
        {
            try
            {
                action();
            }
            catch (TaskCanceledException ex)
            {
                LogHelper.WriteLogToFile(ex, "PowerPoint | UI action canceled");
            }
            catch (InvalidOperationException ex)
            {
                LogHelper.WriteLogToFile(ex, "PowerPoint | UI action failed");
            }
        }

        private void HandleSlideShowStartFloatingBarState()
        {
            if (Settings.Automation.IsAutoFoldInPPTSlideShow && !isFloatingBarFolded)
            {
                FoldFloatingBar_Click(null, null);
            }
            else if (isFloatingBarFolded)
            {
                UnFoldFloatingBar_MouseUp(null, null);
            }
        }

        private void InitializeSlideShowSession()
        {
            if (ShellViewModel.IsBlackboardMode)
            {
                ImageBlackboard_Click(null, null);
            }

            lastDesktopInkColor = 1;

            int slideCount = PresentationViewModel.SlideCount;
            previousSlideID = 0;
            InitializePresentationMemoryStreams(slideCount);

            pptName = CurrentPresentationName;
            LogHelper.NewLog("Name: " + CurrentPresentationName);
            LogHelper.NewLog("Slides Count: " + slideCount);

            if (Settings.PowerPointSettings.IsAutoSaveStrokesInPowerPoint)
            {
                LoadSavedPresentationStrokes(GetPresentationStoragePath(CurrentPresentationName, slideCount));
            }

            ApplyPresentationNavigationVisibility();
            ViewboxFloatingBar.Opacity = Settings.Appearance.IsColorfulViewboxFloatingBar ? 0.8 : 0.75;

            if (Settings.PowerPointSettings.IsShowCanvasAtNewSlideShow && Main_Grid.Background == Brushes.Transparent)
            {
                if (ShellViewModel.IsBlackboardMode)
                {
                    ExitBlackboardSession();
                    ClearStrokes(true);
                }
                else
                {
                    BtnHideInkCanvas_Click(null, null);
                }
            }

            ClearStrokes(true);
            BorderFloatingBarMainControls.Visibility = Visibility.Visible;

            if (Settings.PowerPointSettings.IsShowCanvasAtNewSlideShow)
            {
                BtnColorRed_Click(null, null);
            }

            isEnteredSlideShowEndEvent = false;
            PptNavigationTextBlockBottom.Text = $"{CurrentPresentationSlideIndex}/{slideCount}";
            LogHelper.NewLog("PowerPoint Slide Show Loading process complete");

            _ = ViewboxFloatingBarMarginAnimationAfterDelayAsync(TimeSpan.FromMilliseconds(100));
        }

        private void InitializePresentationMemoryStreams(int slideCount)
        {
            DisposeMemoryStreams();
            memoryStreams = new MemoryStream[slideCount + 2];
        }

        private void LoadSavedPresentationStrokes(string folderPath)
        {
            if (!Directory.Exists(folderPath))
            {
                return;
            }

            LogHelper.WriteLogToFile("Found saved strokes", LogHelper.LogType.Trace);
            FileInfo[] files = new DirectoryInfo(folderPath).GetFiles();
            int count = 0;
            foreach (FileInfo file in files)
            {
                if (string.Equals(file.Name, "Position", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                int slideIndex = -1;
                try
                {
                    slideIndex = int.Parse(Path.GetFileNameWithoutExtension(file.Name));
                    ReplaceMemoryStream(slideIndex, File.ReadAllBytes(file.FullName));
                    count++;
                }
                catch (ArgumentException ex)
                {
                    LogHelper.WriteLogToFile(ex, $"PowerPoint | Failed to load strokes on slide {slideIndex}");
                }
                catch (IOException ex)
                {
                    LogHelper.WriteLogToFile(ex, $"PowerPoint | Failed to load strokes on slide {slideIndex}");
                }
                catch (UnauthorizedAccessException ex)
                {
                    LogHelper.WriteLogToFile(ex, $"PowerPoint | Failed to load strokes on slide {slideIndex}");
                }
            }

            LogHelper.WriteLogToFile($"Loaded {count} saved strokes");
        }

        private void ApplyPresentationNavigationVisibility()
        {
            bool showBottomNavigation = Settings.PowerPointSettings.IsShowBottomPPTNavigationPanel;
            bool showSideNavigation = Settings.PowerPointSettings.IsShowSidePPTNavigationPanel;

            BtnPPTSlideShowEnd.Visibility = Visibility.Visible;

            if (showBottomNavigation)
            {
                AnimationsHelper.ShowWithScaleFromBottom(PPTNavigationBottomLeft);
                AnimationsHelper.ShowWithScaleFromBottom(PPTNavigationBottomRight);
            }
            else
            {
                PPTNavigationBottomLeft.Visibility = Visibility.Collapsed;
                PPTNavigationBottomRight.Visibility = Visibility.Collapsed;
            }

            if (showSideNavigation)
            {
                AnimationsHelper.ShowWithScaleFromLeft(PPTNavigationSidesLeft);
                AnimationsHelper.ShowWithScaleFromRight(PPTNavigationSidesRight);
            }
            else
            {
                PPTNavigationSidesLeft.Visibility = Visibility.Collapsed;
                PPTNavigationSidesRight.Visibility = Visibility.Collapsed;
            }

            PresentationViewModel.SetNavigationVisibility(showBottomNavigation, showSideNavigation);
        }

        private void ResetPresentationNavigationState()
        {
            BtnPPTSlideShowEnd.Visibility = Visibility.Collapsed;
            HidePresentationNavigation();
            PresentationViewModel.SetNavigationVisibility(false, false);
        }

        private async Task HandleSlideShowEndAsync()
        {
            if (isFloatingBarFolded)
            {
                UnFoldFloatingBar_MouseUp(null, null);
            }

            LogHelper.WriteLogToFile("PowerPoint Slide Show End", LogHelper.LogType.Event);
            if (isEnteredSlideShowEndEvent)
            {
                LogHelper.WriteLogToFile("Detected previous entrance, returning");
                return;
            }

            isEnteredSlideShowEndEvent = true;
            if (Settings.PowerPointSettings.IsAutoSaveStrokesInPowerPoint)
            {
                string folderPath = GetPresentationStoragePath(CurrentPresentationName, PresentationViewModel.SlideCount);
                Directory.CreateDirectory(folderPath);
                TryWritePresentationPosition(folderPath);
                RunPptUiAction(() => CaptureCurrentInkToMemoryStream(currentShowPosition));
                SavePresentationStrokes(folderPath, PresentationViewModel.SlideCount);
            }

            RunPptUiAction(ApplySlideShowEndUi);

            await Task.Delay(150);
            ViewboxFloatingBarMarginAnimation();
            DisposeMemoryStreams();
        }

        private void SavePresentationStrokes(string folderPath, int slideCount)
        {
            for (int i = 1; i <= slideCount; i++)
            {
                if (memoryStreams[i] == null)
                {
                    continue;
                }

                TrySavePresentationStroke(folderPath, i);
            }
        }

        private (string icartFilePath, string icstkFilePath) GetPresentationInkFilePaths(string folderPath, int slideIndex)
        {
            string baseFilePath = Path.Combine(folderPath, slideIndex.ToString("0000"));
            return (baseFilePath + ".icart", baseFilePath + ".icstk");
        }

        private void TrySavePresentationStroke(string folderPath, int slideIndex)
        {
            (string icartFilePath, string icstkFilePath) = GetPresentationInkFilePaths(folderPath, slideIndex);

            try
            {
                if (memoryStreams[slideIndex].Length > 8)
                {
                    memoryStreams[slideIndex].Position = 0;
                    byte[] strokeBuffer = memoryStreams[slideIndex].ToArray();

                    if (File.Exists(icartFilePath))
                    {
                        File.WriteAllBytes(icartFilePath, strokeBuffer);
                        LogHelper.WriteLogToFile($"Saved strokes for Slide {slideIndex} as .icart, size={memoryStreams[slideIndex].Length}, byteLength={strokeBuffer.Length}");
                    }
                    else
                    {
                        File.WriteAllBytes(icstkFilePath, strokeBuffer);
                        LogHelper.WriteLogToFile($"Saved strokes for Slide {slideIndex} as .icstk, size={memoryStreams[slideIndex].Length}, byteLength={strokeBuffer.Length}");
                    }
                }
                else
                {
                    File.Delete(icartFilePath);
                    File.Delete(icstkFilePath);
                }
            }
            catch (IOException ex)
            {
                LogHelper.WriteLogToFile(ex, $"PowerPoint | Failed to save strokes for slide {slideIndex}");
                File.Delete(icstkFilePath);
            }
            catch (UnauthorizedAccessException ex)
            {
                LogHelper.WriteLogToFile(ex, $"PowerPoint | Failed to save strokes for slide {slideIndex}");
                File.Delete(icstkFilePath);
            }
            catch (ArgumentException ex)
            {
                LogHelper.WriteLogToFile(ex, $"PowerPoint | Failed to save strokes for slide {slideIndex}");
                File.Delete(icstkFilePath);
            }
        }

        private void ApplySlideShowEndUi()
        {
            ResetPresentationNavigationState();

            if (ShellViewModel.IsBlackboardMode)
            {
                ExitBlackboardSession();
            }

            ClearStrokes(true);

            if (Main_Grid.Background != Brushes.Transparent)
            {
                BtnHideInkCanvas_Click(null, null);
            }

            RestoreDesktopWorkspaceDefaultsAfterPresentation();
            ViewboxFloatingBar.Opacity = Settings.Appearance.IsColorfulViewboxFloatingBar ? 0.95 : 1;
        }

        private void ApplySlideShowNextSlideState(int currentSlideIndex, int slideCount)
        {
            CaptureCurrentInkToMemoryStream(previousSlideID);

            if (inkCanvas.Strokes.Count > Settings.Automation.MinimumAutomationStrokeNumber
                && Settings.PowerPointSettings.IsAutoSaveScreenShotInPowerPoint
                && !_isPptClickingBtnTurned)
            {
                SavePPTScreenshot(CurrentPresentationName + "/" + currentSlideIndex);
            }

            _isPptClickingBtnTurned = false;

            ClearStrokes(true);
            timeMachine.ClearStrokeHistory();
            RestoreSlideInk(currentSlideIndex);

            currentShowPosition = currentSlideIndex;
            PptNavigationTextBlockBottom.Text = $"{currentSlideIndex}/{slideCount}";
        }

        private void RestoreSlideInk(int slideIndex)
        {
            if (!IsMemoryStreamIndexValid(slideIndex)
                || memoryStreams[slideIndex] == null
                || memoryStreams[slideIndex].Length <= 0)
            {
                return;
            }

            memoryStreams[slideIndex].Position = 0;
            inkCanvas.Strokes.Add(new StrokeCollection(memoryStreams[slideIndex]));
        }

        private async Task HandlePptNavigationButtonClickAsync(object sender)
        {
            if (lastBorderMouseDownObject != sender)
            {
                return;
            }

            Main_Grid.Background = new SolidColorBrush(StringToColor("#01FFFFFF"));
            CursorIcon_Click(null, null);
            presentationSessionController?.TryShowSlideNavigation();

            if (!isFloatingBarFolded)
            {
                await Task.Delay(100);
                ViewboxFloatingBarMarginAnimation();
            }
        }

        private async Task HandlePptSlideShowEndClickAsync()
        {
            presentationSessionController?.TryExitSlideShow();

            HideSubPanels("cursor");
            await Task.Delay(150);
            ViewboxFloatingBarMarginAnimation();
        }

        private void PptApplication_SlideShowBegin()
        {
            HandleSlideShowStartFloatingBarState();
            LogHelper.WriteLogToFile("PowerPoint Application Slide Show Begin", LogHelper.LogType.Event);
            RunPptUiAction(InitializeSlideShowSession);
        }

        private bool isEnteredSlideShowEndEvent = false; //防止重复调用本函数导致墨迹保存失效

        private async void PptApplication_SlideShowEnd()
        {
            await HandleSlideShowEndAsync();
        }

        private int previousSlideID = 0;
        private MemoryStream[] memoryStreams = new MemoryStream[50];

        private void TryWritePresentationPosition(string folderPath)
        {
            string positionFilePath = Path.Combine(folderPath, "Position");
            try
            {
                File.WriteAllText(positionFilePath, previousSlideID.ToString());
            }
            catch (IOException ex)
            {
                LogHelper.WriteLogToFile(ex, $"PowerPoint | Failed to save presentation position to '{positionFilePath}'");
            }
            catch (UnauthorizedAccessException ex)
            {
                LogHelper.WriteLogToFile(ex, $"PowerPoint | Failed to save presentation position to '{positionFilePath}'");
            }
        }

        private void CaptureCurrentInkToMemoryStream(int slideIndex)
        {
            if (!IsMemoryStreamIndexValid(slideIndex))
            {
                return;
            }

            try
            {
                using MemoryStream memoryStream = new MemoryStream();
                inkCanvas.Strokes.Save(memoryStream);
                ReplaceMemoryStream(slideIndex, memoryStream.ToArray());
            }
            catch (ArgumentException ex)
            {
                LogHelper.WriteLogToFile(ex, $"PowerPoint | Failed to capture strokes for slide {slideIndex}");
            }
            catch (InvalidOperationException ex)
            {
                LogHelper.WriteLogToFile(ex, $"PowerPoint | Failed to capture strokes for slide {slideIndex}");
            }
        }

        private void ReplaceMemoryStream(int index, byte[] buffer)
        {
            if (!IsMemoryStreamIndexValid(index) || buffer == null)
            {
                return;
            }

            memoryStreams[index]?.Dispose();
            MemoryStream stream = new MemoryStream(buffer);
            stream.Position = 0;
            memoryStreams[index] = stream;
        }

        private bool IsMemoryStreamIndexValid(int index)
        {
            return memoryStreams != null && index > 0 && index < memoryStreams.Length;
        }

        private void DisposeMemoryStreams()
        {
            if (memoryStreams == null)
            {
                return;
            }

            for (int i = 0; i < memoryStreams.Length; i++)
            {
                memoryStreams[i]?.Dispose();
                memoryStreams[i] = null;
            }
        }

        private void PptApplication_SlideShowNextSlide()
        {
            int currentSlideIndex = CurrentPresentationSlideIndex;
            int slideCount = PresentationViewModel.SlideCount;
            LogHelper.WriteLogToFile($"PowerPoint Next Slide (Slide {currentSlideIndex})", LogHelper.LogType.Event);

            if (currentSlideIndex == previousSlideID)
            {
                return;
            }

            RunPptUiAction(() => ApplySlideShowNextSlideState(currentSlideIndex, slideCount));

            previousSlideID = currentSlideIndex;
        }

        private bool _isPptClickingBtnTurned = false;

        private void BtnPPTSlidesUp_Click(object sender, RoutedEventArgs e)
        {
            if (ShellViewModel.IsBlackboardMode)
            {
                ExitBlackboardSession();
            }

            _isPptClickingBtnTurned = true;

            if (inkCanvas.Strokes.Count > Settings.Automation.MinimumAutomationStrokeNumber
                && Settings.PowerPointSettings.IsAutoSaveScreenShotInPowerPoint)
            {
                SavePPTScreenshot(CurrentPresentationName + "/" + CurrentPresentationSlideIndex);
            }

            presentationSessionController?.TryGoToPreviousSlide();
        }

        private void BtnPPTSlidesDown_Click(object sender, RoutedEventArgs e)
        {
            if (ShellViewModel.IsBlackboardMode)
            {
                ExitBlackboardSession();
            }

            _isPptClickingBtnTurned = true;
            if (inkCanvas.Strokes.Count > Settings.Automation.MinimumAutomationStrokeNumber
                && Settings.PowerPointSettings.IsAutoSaveScreenShotInPowerPoint)
            {
                SavePPTScreenshot(CurrentPresentationName + "/" + CurrentPresentationSlideIndex);
            }

            presentationSessionController?.TryGoToNextSlide();
        }

        private async void PPTNavigationBtn_Click(object sender, MouseButtonEventArgs e)
        {
            await HandlePptNavigationButtonClickAsync(sender);
        }

        private async void BtnPPTSlideShowEnd_Click(object sender, RoutedEventArgs e)
        {
            await HandlePptSlideShowEndClickAsync();
        }

        private void GridPPTControlPrevious_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (lastBorderMouseDownObject != sender)
            {
                return;
            }

            BtnPPTSlidesUp_Click(null, null);
        }

        private void GridPPTControlNext_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (lastBorderMouseDownObject != sender)
            {
                return;
            }

            BtnPPTSlidesDown_Click(null, null);
        }
    }
}
