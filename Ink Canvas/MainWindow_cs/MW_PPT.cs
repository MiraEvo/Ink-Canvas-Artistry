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

            if (Settings.PowerPointSettings.IsNotifyPreviousPage)
            {
                Application.Current.Dispatcher.BeginInvoke((Action)(() =>
                {
                    string folderPath = Settings.Automation.AutoSavedStrokesLocation
                        + @"\Auto Saved - Presentations\"
                        + presentationName
                        + "_"
                        + slideCount;
                    try
                    {
                        if (File.Exists(folderPath + "/Position")
                            && int.TryParse(File.ReadAllText(folderPath + "/Position"), out int page)
                            && page > 0)
                        {
                            new YesOrNoNotificationWindow(
                                $"上次播放到了第 {page} 页, 是否立即跳转",
                                () => presentationSessionController?.TryGoToSlide(page))
                                .ShowDialog();
                        }
                    }
                    catch (IOException ex)
                    {
                        LogHelper.WriteLogToFile(ex, "PowerPoint | Failed to read saved slide position");
                    }
                    catch (UnauthorizedAccessException ex)
                    {
                        LogHelper.WriteLogToFile(ex, "PowerPoint | Access denied while reading saved slide position");
                    }
                }));
            }

            if (Settings.PowerPointSettings.IsNotifyHiddenPage && presentationSessionController?.HasHiddenSlides() == true)
            {
                Application.Current.Dispatcher.BeginInvoke((Action)(() =>
                {
                    if (!IsShowingRestoreHiddenSlidesWindow)
                    {
                        IsShowingRestoreHiddenSlidesWindow = true;
                        new YesOrNoNotificationWindow(
                            "检测到此演示文档中包含隐藏的幻灯片，是否取消隐藏？",
                            () => presentationSessionController?.TryUnhideHiddenSlides())
                            .ShowDialog();
                    }
                }));
            }

            if (Settings.PowerPointSettings.IsNotifyAutoPlayPresentation
                && !IsPresentationSlideShowRunning
                && presentationSessionController?.HasAutomaticAdvance() == true)
            {
                Application.Current.Dispatcher.BeginInvoke((Action)(() =>
                {
                    new YesOrNoNotificationWindow(
                        "检测到此演示文档中自动播放或排练计时已经启用，可能导致幻灯片自动翻页，是否取消？",
                        () => presentationSessionController?.TryDisableAutomaticAdvance())
                        .ShowDialog();
                }));

                presentationSessionController?.TryDisableAutomaticAdvance();
            }
        }

        private void PptApplication_PresentationClose()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                BtnPPTSlideShowEnd.Visibility = Visibility.Collapsed;
                PPTNavigationBottomLeft.Visibility = Visibility.Collapsed;
                PPTNavigationBottomRight.Visibility = Visibility.Collapsed;
                PPTNavigationSidesLeft.Visibility = Visibility.Collapsed;
                PPTNavigationSidesRight.Visibility = Visibility.Collapsed;
                PresentationViewModel.SetNavigationVisibility(false, false);
            });
        }

        //bool isPresentationHaveBlackSpace = false;
        private string pptName = null;
        private int currentShowPosition = -1;

        private void PptApplication_SlideShowBegin()
        {
            if (Settings.Automation.IsAutoFoldInPPTSlideShow && !isFloatingBarFolded)
            {
                FoldFloatingBar_Click(null, null);
            }
            else if (isFloatingBarFolded)
            {
                UnFoldFloatingBar_MouseUp(null, null);
            }

            LogHelper.WriteLogToFile("PowerPoint Application Slide Show Begin", LogHelper.LogType.Event);
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (ShellViewModel.IsBlackboardMode)
                {
                    ImageBlackboard_Click(null, null);
                }

                lastDesktopInkColor = 1;

                int slideCount = PresentationViewModel.SlideCount;
                previousSlideID = 0;
                DisposeMemoryStreams();
                memoryStreams = new MemoryStream[slideCount + 2];

                pptName = CurrentPresentationName;
                LogHelper.NewLog("Name: " + CurrentPresentationName);
                LogHelper.NewLog("Slides Count: " + slideCount);

                if (Settings.PowerPointSettings.IsAutoSaveStrokesInPowerPoint)
                {
                    string folderPath = Settings.Automation.AutoSavedStrokesLocation
                        + @"\Auto Saved - Presentations\"
                        + CurrentPresentationName
                        + "_"
                        + slideCount;
                    if (Directory.Exists(folderPath))
                    {
                        LogHelper.WriteLogToFile("Found saved strokes", LogHelper.LogType.Trace);
                        FileInfo[] files = new DirectoryInfo(folderPath).GetFiles();
                        int count = 0;
                        foreach (FileInfo file in files)
                        {
                            if (file.Name == "Position")
                            {
                                continue;
                            }

                            int i = -1;
                            try
                            {
                                i = int.Parse(System.IO.Path.GetFileNameWithoutExtension(file.Name));
                                memoryStreams[i] = new MemoryStream(File.ReadAllBytes(file.FullName));
                                memoryStreams[i].Position = 0;
                                count++;
                            }
                            catch (ArgumentException ex)
                            {
                                LogHelper.WriteLogToFile(ex, $"PowerPoint | Failed to load strokes on slide {i}");
                            }
                            catch (IOException ex)
                            {
                                LogHelper.WriteLogToFile(ex, $"PowerPoint | Failed to load strokes on slide {i}");
                            }
                            catch (UnauthorizedAccessException ex)
                            {
                                LogHelper.WriteLogToFile(ex, $"PowerPoint | Failed to load strokes on slide {i}");
                            }
                        }

                        LogHelper.WriteLogToFile(string.Format("Loaded {0} saved strokes", count));
                    }
                }

                BtnPPTSlideShowEnd.Visibility = Visibility.Visible;

                if (Settings.PowerPointSettings.IsShowBottomPPTNavigationPanel)
                {
                    AnimationsHelper.ShowWithScaleFromBottom(PPTNavigationBottomLeft);
                    AnimationsHelper.ShowWithScaleFromBottom(PPTNavigationBottomRight);
                }
                else
                {
                    PPTNavigationBottomLeft.Visibility = Visibility.Collapsed;
                    PPTNavigationBottomRight.Visibility = Visibility.Collapsed;
                }

                if (Settings.PowerPointSettings.IsShowSidePPTNavigationPanel)
                {
                    AnimationsHelper.ShowWithScaleFromLeft(PPTNavigationSidesLeft);
                    AnimationsHelper.ShowWithScaleFromRight(PPTNavigationSidesRight);
                }
                else
                {
                    PPTNavigationSidesLeft.Visibility = Visibility.Collapsed;
                    PPTNavigationSidesRight.Visibility = Visibility.Collapsed;
                }

                PresentationViewModel.SetNavigationVisibility(
                    Settings.PowerPointSettings.IsShowBottomPPTNavigationPanel,
                    Settings.PowerPointSettings.IsShowSidePPTNavigationPanel);

                BtnPPTSlideShowEnd.Visibility = Visibility.Visible;

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
            });
        }

        private bool isEnteredSlideShowEndEvent = false; //防止重复调用本函数导致墨迹保存失效

        private async void PptApplication_SlideShowEnd()
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
                string presentationName = CurrentPresentationName;
                int slideCount = PresentationViewModel.SlideCount;
                string folderPath = Settings.Automation.AutoSavedStrokesLocation
                    + @"\Auto Saved - Presentations\"
                    + presentationName
                    + "_"
                    + slideCount;
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                TryWritePresentationPosition(folderPath);
                Application.Current.Dispatcher.Invoke(() =>
                {
                    CaptureCurrentInkToMemoryStream(currentShowPosition);
                });

                for (int i = 1; i <= slideCount; i++)
                {
                    if (memoryStreams[i] == null)
                    {
                        continue;
                    }

                    try
                    {
                        string baseFilePath = folderPath + @"\" + i.ToString("0000");
                        string icartFilePath = baseFilePath + ".icart";
                        string icstkFilePath = baseFilePath + ".icstk";

                        if (memoryStreams[i].Length > 8)
                        {
                            memoryStreams[i].Position = 0;
                            byte[] srcBuf = new byte[memoryStreams[i].Length];
                            int byteLength = memoryStreams[i].Read(srcBuf, 0, srcBuf.Length);

                            if (File.Exists(icartFilePath))
                            {
                                File.WriteAllBytes(icartFilePath, srcBuf);
                                LogHelper.WriteLogToFile(string.Format("Saved strokes for Slide {0} as .icart, size={1}, byteLength={2}", i, memoryStreams[i].Length, byteLength));
                            }
                            else
                            {
                                File.WriteAllBytes(icstkFilePath, srcBuf);
                                LogHelper.WriteLogToFile(string.Format("Saved strokes for Slide {0} as .icstk, size={1}, byteLength={2}", i, memoryStreams[i].Length, byteLength));
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
                        LogHelper.WriteLogToFile(ex, $"PowerPoint | Failed to save strokes for slide {i}");
                        File.Delete(folderPath + @"\" + i.ToString("0000") + ".icstk");
                    }
                    catch (UnauthorizedAccessException ex)
                    {
                        LogHelper.WriteLogToFile(ex, $"PowerPoint | Failed to save strokes for slide {i}");
                        File.Delete(folderPath + @"\" + i.ToString("0000") + ".icstk");
                    }
                    catch (ArgumentException ex)
                    {
                        LogHelper.WriteLogToFile(ex, $"PowerPoint | Failed to save strokes for slide {i}");
                        File.Delete(folderPath + @"\" + i.ToString("0000") + ".icstk");
                    }
                }
            }

            Application.Current.Dispatcher.Invoke(() =>
            {
                BtnPPTSlideShowEnd.Visibility = Visibility.Collapsed;
                PPTNavigationBottomLeft.Visibility = Visibility.Collapsed;
                PPTNavigationBottomRight.Visibility = Visibility.Collapsed;
                PPTNavigationSidesLeft.Visibility = Visibility.Collapsed;
                PPTNavigationSidesRight.Visibility = Visibility.Collapsed;
                PresentationViewModel.SetNavigationVisibility(false, false);

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
            });

            await Task.Delay(150);
            ViewboxFloatingBarMarginAnimation();
            DisposeMemoryStreams();
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

            Application.Current.Dispatcher.Invoke(() =>
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

                if (IsMemoryStreamIndexValid(currentSlideIndex)
                    && memoryStreams[currentSlideIndex] != null
                    && memoryStreams[currentSlideIndex].Length > 0)
                {
                    memoryStreams[currentSlideIndex].Position = 0;
                    inkCanvas.Strokes.Add(new StrokeCollection(memoryStreams[currentSlideIndex]));
                }

                currentShowPosition = currentSlideIndex;
                PptNavigationTextBlockBottom.Text = $"{currentSlideIndex}/{slideCount}";
            });

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

        private async void BtnPPTSlideShowEnd_Click(object sender, RoutedEventArgs e)
        {
            presentationSessionController?.TryExitSlideShow();

            HideSubPanels("cursor");
            await Task.Delay(150);
            ViewboxFloatingBarMarginAnimation();
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
