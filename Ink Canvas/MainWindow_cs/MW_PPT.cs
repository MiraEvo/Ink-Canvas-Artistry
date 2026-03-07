using Ink_Canvas.Helpers;
using Microsoft.Office.Interop.PowerPoint;
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
using System.Runtime.InteropServices;
using System.Reflection;

namespace Ink_Canvas
{
    public partial class MainWindow : Window
    {
        private void PresentationSessionController_PresentationConnected(Presentation connectedPresentation)
        {
            if (presentation == null)
            {
                return;
            }

            try
            {
                if (Settings.PowerPointSettings.IsNotifyPreviousPage)
                {
                    Application.Current.Dispatcher.BeginInvoke((Action)(() =>
                    {
                        string folderPath = Settings.Automation.AutoSavedStrokesLocation + @"\Auto Saved - Presentations\" + presentation.Name + "_" + presentation.Slides.Count;
                        try
                        {
                            if (File.Exists(folderPath + "/Position"))
                            {
                                if (int.TryParse(File.ReadAllText(folderPath + "/Position"), out int page))
                                {
                                    if (page <= 0)
                                    {
                                        return;
                                    }

                                    new YesOrNoNotificationWindow($"上次播放到了第 {page} 页, 是否立即跳转", () =>
                                    {
                                        if (pptApplication?.SlideShowWindows.Count >= 1)
                                        {
                                            presentation.SlideShowWindow.View.GotoSlide(page);
                                        }
                                        else
                                        {
                                            presentation.Windows[1].View.GotoSlide(page);
                                        }
                                    }).ShowDialog();
                                }
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
                        catch (COMException ex)
                        {
                            LogHelper.WriteLogToFile(ex, "PowerPoint | Failed to restore previous slide position");
                        }
                    }));
                }

                if (Settings.PowerPointSettings.IsNotifyHiddenPage && slides != null)
                {
                    bool isHaveHiddenSlide = false;
                    foreach (Slide slide in slides)
                    {
                        if (IsSlideHidden(slide))
                        {
                            isHaveHiddenSlide = true;
                            break;
                        }
                    }

                    Application.Current.Dispatcher.BeginInvoke((Action)(() =>
                    {
                        if (isHaveHiddenSlide && !IsShowingRestoreHiddenSlidesWindow)
                        {
                            IsShowingRestoreHiddenSlidesWindow = true;
                            new YesOrNoNotificationWindow("检测到此演示文档中包含隐藏的幻灯片，是否取消隐藏？",
                                () =>
                                {
                                    foreach (Slide slide in slides)
                                    {
                                        if (IsSlideHidden(slide))
                                        {
                                            SetSlideHidden(slide, false);
                                        }
                                    }
                                }).ShowDialog();
                        }
                    }));
                }

                if (Settings.PowerPointSettings.IsNotifyAutoPlayPresentation && !IsPresentationSlideShowRunning)
                {
                    bool hasSlideTimings = false;
                    foreach (Slide slide in presentation.Slides)
                    {
                        if (HasAutomaticAdvance(slide))
                        {
                            hasSlideTimings = true;
                            break;
                        }
                    }

                    if (hasSlideTimings)
                    {
                        Application.Current.Dispatcher.BeginInvoke((Action)(() =>
                        {
                            new YesOrNoNotificationWindow("检测到此演示文档中自动播放或排练计时已经启用，可能导致幻灯片自动翻页，是否取消？",
                                () =>
                                {
                                    presentation.SlideShowSettings.AdvanceMode = PpSlideShowAdvanceMode.ppSlideShowManualAdvance;
                                }).ShowDialog();
                        }));
                        presentation.SlideShowSettings.AdvanceMode = PpSlideShowAdvanceMode.ppSlideShowManualAdvance;
                    }
                }
            }
            catch (COMException ex)
            {
                LogHelper.WriteLogToFile(ex, "PowerPoint | Failed during presentation connection initialization");
            }
            catch (InvalidOperationException ex)
            {
                LogHelper.WriteLogToFile(ex, "PowerPoint | Invalid state during presentation connection initialization");
            }
        }

        private void PptApplication_PresentationClose(Presentation Pres)
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
        int currentShowPosition = -1;
        private void PptApplication_SlideShowBegin(SlideShowWindow Wn)
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
                /*
                //调整颜色
                double screenRatio = SystemParameters.PrimaryScreenWidth / SystemParameters.PrimaryScreenHeight;
                if (Math.Abs(screenRatio - 16.0 / 9) <= -0.01)
                {
                    if (Wn.Presentation.PageSetup.SlideWidth / Wn.Presentation.PageSetup.SlideHeight < 1.65)
                    {
                        isPresentationHaveBlackSpace = true;
                    }
                }
                else if (screenRatio == -256 / 135)
                {

                }
                */
                lastDesktopInkColor = 1;

                int slideCount = Wn.Presentation.Slides.Count;
                previousSlideID = 0;
                DisposeMemoryStreams();
                memoryStreams = new MemoryStream[slideCount + 2];

                pptName = Wn.Presentation.Name;
                LogHelper.NewLog("Name: " + Wn.Presentation.Name);
                LogHelper.NewLog("Slides Count: " + slideCount);

                //检查是否有已有墨迹，并加载
                if (Settings.PowerPointSettings.IsAutoSaveStrokesInPowerPoint)
                {
                    if (Directory.Exists(Settings.Automation.AutoSavedStrokesLocation + @"\Auto Saved - Presentations\" + Wn.Presentation.Name + "_" + Wn.Presentation.Slides.Count))
                    {
                        LogHelper.WriteLogToFile("Found saved strokes", LogHelper.LogType.Trace);
                        FileInfo[] files = new DirectoryInfo(Settings.Automation.AutoSavedStrokesLocation + @"\Auto Saved - Presentations\" + Wn.Presentation.Name + "_" + Wn.Presentation.Slides.Count).GetFiles();
                        int count = 0;
                        foreach (FileInfo file in files)
                        {
                            if (file.Name != "Position")
                            {
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

                if (Settings.Appearance.IsColorfulViewboxFloatingBar)
                {
                    ViewboxFloatingBar.Opacity = 0.8;
                }
                else
                {
                    ViewboxFloatingBar.Opacity = 0.75;
                }

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
                PptNavigationTextBlockBottom.Text = $"{Wn.View.CurrentShowPosition}/{Wn.Presentation.Slides.Count}";
                LogHelper.NewLog("PowerPoint Slide Show Loading process complete");

                _ = ViewboxFloatingBarMarginAnimationAfterDelayAsync(TimeSpan.FromMilliseconds(100));
            });
        }

        bool isEnteredSlideShowEndEvent = false; //防止重复调用本函数导致墨迹保存失效
        private async void PptApplication_SlideShowEnd(Presentation Pres)
        {
            if (isFloatingBarFolded) UnFoldFloatingBar_MouseUp(null, null);

            LogHelper.WriteLogToFile(string.Format("PowerPoint Slide Show End"), LogHelper.LogType.Event);
            if (isEnteredSlideShowEndEvent)
            {
                LogHelper.WriteLogToFile("Detected previous entrance, returning");
                return;
            }
            isEnteredSlideShowEndEvent = true;
            if (Settings.PowerPointSettings.IsAutoSaveStrokesInPowerPoint)
            {
                string folderPath = Settings.Automation.AutoSavedStrokesLocation + @"\Auto Saved - Presentations\" + Pres.Name + "_" + Pres.Slides.Count;
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }
                TryWritePresentationPosition(folderPath);
                Application.Current.Dispatcher.Invoke(() =>
                {
                    CaptureCurrentInkToMemoryStream(currentShowPosition);
                });
                for (int i = 1; i <= Pres.Slides.Count; i++)
                {
                    if (memoryStreams[i] != null)
                    {
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
            }

            Application.Current.Dispatcher.Invoke(() =>
            {
                //isPresentationHaveBlackSpace = false;

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

                if (Settings.Appearance.IsColorfulViewboxFloatingBar)
                {
                    ViewboxFloatingBar.Opacity = 0.95;
                }
                else
                {
                    ViewboxFloatingBar.Opacity = 1;
                }
            });

            await Task.Delay(150);
            ViewboxFloatingBarMarginAnimation();
            DisposeMemoryStreams();
        }

        int previousSlideID = 0;
        MemoryStream[] memoryStreams = new MemoryStream[50];

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

        private SlideShowWindow TryGetActiveSlideShowWindow()
        {
            try
            {
                return pptApplication?.SlideShowWindows.Count >= 1 ? pptApplication.SlideShowWindows[1] : null;
            }
            catch (COMException ex)
            {
                LogHelper.WriteLogToFile(ex, "PowerPoint | Failed to get active slide show window");
                return null;
            }
            catch (InvalidOperationException ex)
            {
                LogHelper.WriteLogToFile(ex, "PowerPoint | Failed to get active slide show window");
                return null;
            }
        }

        private void RunSlideShowWindowAction(Action<SlideShowWindow> action, string actionName, LogHelper.LogType failureLogType = LogHelper.LogType.Error)
        {
            _ = RunSlideShowWindowActionAsync(action, actionName, failureLogType);
        }

        private async Task RunSlideShowWindowActionAsync(Action<SlideShowWindow> action, string actionName, LogHelper.LogType failureLogType)
        {
            await Task.Run(() =>
            {
                SlideShowWindow slideShowWindow = TryGetActiveSlideShowWindow();
                if (slideShowWindow == null)
                {
                    LogHelper.WriteLogToFile($"PowerPoint | Skipped '{actionName}' because no active slide show window was found.", failureLogType);
                    return;
                }

                try
                {
                    slideShowWindow.Activate();
                }
                catch (COMException ex)
                {
                    LogHelper.WriteLogToFile(ex, $"PowerPoint | Failed to activate slide show window before '{actionName}'", failureLogType);
                }
                catch (InvalidOperationException ex)
                {
                    LogHelper.WriteLogToFile(ex, $"PowerPoint | Failed to activate slide show window before '{actionName}'", failureLogType);
                }

                try
                {
                    action(slideShowWindow);
                }
                catch (COMException ex)
                {
                    LogHelper.WriteLogToFile(ex, $"PowerPoint | Failed to {actionName}", failureLogType);
                }
                catch (InvalidOperationException ex)
                {
                    LogHelper.WriteLogToFile(ex, $"PowerPoint | Failed to {actionName}", failureLogType);
                }
            });
        }

        private static bool IsSlideHidden(Slide slide)
        {
            return IsOfficeTrueState(GetSlideShowTransitionPropertyValue(slide, "Hidden"));
        }

        private static void SetSlideHidden(Slide slide, bool isHidden)
        {
            object transition = slide.SlideShowTransition;
            PropertyInfo hiddenProperty = transition.GetType().GetProperty("Hidden");
            if (hiddenProperty == null || !hiddenProperty.CanWrite)
            {
                return;
            }

            object hiddenValue = Enum.ToObject(hiddenProperty.PropertyType, isHidden ? -1 : 0);
            hiddenProperty.SetValue(transition, hiddenValue);
        }

        private static bool HasAutomaticAdvance(Slide slide)
        {
            object advanceOnTime = GetSlideShowTransitionPropertyValue(slide, "AdvanceOnTime");
            object advanceTime = GetSlideShowTransitionPropertyValue(slide, "AdvanceTime");
            return IsOfficeTrueState(advanceOnTime) && Convert.ToDouble(advanceTime) > 0;
        }

        private static object GetSlideShowTransitionPropertyValue(Slide slide, string propertyName)
        {
            object transition = slide.SlideShowTransition;
            PropertyInfo property = transition.GetType().GetProperty(propertyName);
            return property?.GetValue(transition);
        }

        private static bool IsOfficeTrueState(object value)
        {
            return value != null && Convert.ToInt32(value) == -1;
        }

        private void PptApplication_SlideShowNextSlide(SlideShowWindow Wn)
        {
            LogHelper.WriteLogToFile(string.Format("PowerPoint Next Slide (Slide {0})", Wn.View.CurrentShowPosition), LogHelper.LogType.Event);
            if (Wn.View.CurrentShowPosition != previousSlideID)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    CaptureCurrentInkToMemoryStream(previousSlideID);

                    if (inkCanvas.Strokes.Count > Settings.Automation.MinimumAutomationStrokeNumber && Settings.PowerPointSettings.IsAutoSaveScreenShotInPowerPoint && !_isPptClickingBtnTurned)
                        SavePPTScreenshot(Wn.Presentation.Name + "/" + Wn.View.CurrentShowPosition);
                    _isPptClickingBtnTurned = false;

                    ClearStrokes(true);
                    timeMachine.ClearStrokeHistory();

                    if (IsMemoryStreamIndexValid(Wn.View.CurrentShowPosition)
                        && memoryStreams[Wn.View.CurrentShowPosition] != null
                        && memoryStreams[Wn.View.CurrentShowPosition].Length > 0)
                    {
                        memoryStreams[Wn.View.CurrentShowPosition].Position = 0;
                        inkCanvas.Strokes.Add(new StrokeCollection(memoryStreams[Wn.View.CurrentShowPosition]));
                    }
                    currentShowPosition = Wn.View.CurrentShowPosition;

                    PptNavigationTextBlockBottom.Text = $"{Wn.View.CurrentShowPosition}/{Wn.Presentation.Slides.Count}";
                });
                previousSlideID = Wn.View.CurrentShowPosition;

            }
        }

        private bool _isPptClickingBtnTurned = false;

        private void BtnPPTSlidesUp_Click(object sender, RoutedEventArgs e)
        {
            if (ShellViewModel.IsBlackboardMode)
            {
                ExitBlackboardSession();
            }

            _isPptClickingBtnTurned = true;

            if (inkCanvas.Strokes.Count > Settings.Automation.MinimumAutomationStrokeNumber &&
                Settings.PowerPointSettings.IsAutoSaveScreenShotInPowerPoint)
                SavePPTScreenshot(CurrentPresentationName + "/" + CurrentPresentationSlideIndex);

            RunSlideShowWindowAction(window => window.View.Previous(), "go to previous slide", LogHelper.LogType.Trace);
        }

        private void BtnPPTSlidesDown_Click(object sender, RoutedEventArgs e)
        {
            if (ShellViewModel.IsBlackboardMode)
            {
                ExitBlackboardSession();
            }
            _isPptClickingBtnTurned = true;
            if (inkCanvas.Strokes.Count > Settings.Automation.MinimumAutomationStrokeNumber &&
                Settings.PowerPointSettings.IsAutoSaveScreenShotInPowerPoint)
                SavePPTScreenshot(CurrentPresentationName + "/" + CurrentPresentationSlideIndex);
            RunSlideShowWindowAction(window => window.View.Next(), "go to next slide");
        }


        private async void PPTNavigationBtn_Click(object sender, MouseButtonEventArgs e)
        {
            if (lastBorderMouseDownObject != sender) return;
            Main_Grid.Background = new SolidColorBrush(StringToColor("#01FFFFFF"));
            CursorIcon_Click(null, null);
            SlideShowWindow slideShowWindow = TryGetActiveSlideShowWindow();
            if (slideShowWindow?.SlideNavigation != null)
            {
                slideShowWindow.SlideNavigation.Visible = true;
            }
            // 控制居中
            if (!isFloatingBarFolded)
            {
                await Task.Delay(100);
                ViewboxFloatingBarMarginAnimation();
            }
        }

        private async void BtnPPTSlideShowEnd_Click(object sender, RoutedEventArgs e)
        {
            RunSlideShowWindowAction(window => window.View.Exit(), "exit slide show");

            HideSubPanels("cursor");
            await Task.Delay(150);
            ViewboxFloatingBarMarginAnimation();
        }

        private void GridPPTControlPrevious_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (lastBorderMouseDownObject != sender) return;
            BtnPPTSlidesUp_Click(null, null);
        }

        private void GridPPTControlNext_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (lastBorderMouseDownObject != sender) return;
            BtnPPTSlidesDown_Click(null, null);
        }
    }
}
