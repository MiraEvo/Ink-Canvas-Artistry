using Ink_Canvas.Helpers;
using Microsoft.Office.Interop.PowerPoint;
using Ink_Canvas.ViewModels;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using Application = System.Windows.Application;
using File = System.IO.File;
using Microsoft.Office.Core;

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
                        catch (Exception ex)
                        {
                            LogHelper.WriteLogToFile(ex.ToString(), LogHelper.LogType.Error);
                        }
                    }));
                }

                if (Settings.PowerPointSettings.IsNotifyHiddenPage && slides != null)
                {
                    bool isHaveHiddenSlide = false;
                    foreach (Slide slide in slides)
                    {
                        if (slide.SlideShowTransition.Hidden == MsoTriState.msoTrue)
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
                                        if (slide.SlideShowTransition.Hidden == MsoTriState.msoTrue)
                                        {
                                            slide.SlideShowTransition.Hidden = MsoTriState.msoFalse;
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
                        if (slide.SlideShowTransition.AdvanceOnTime == MsoTriState.msoTrue && slide.SlideShowTransition.AdvanceTime > 0)
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
            catch
            {
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
                memoryStreams = new MemoryStream[slideCount + 2];

                pptName = Wn.Presentation.Name;
                LogHelper.NewLog("Name: " + Wn.Presentation.Name);
                LogHelper.NewLog("Slides Count: " + slideCount.ToString());

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
                                catch (Exception ex)
                                {
                                    LogHelper.WriteLogToFile(string.Format("Failed to load strokes on Slide {0}\n{1}", i, ex.ToString()), LogHelper.LogType.Error);
                                }
                            }
                        }
                        LogHelper.WriteLogToFile(string.Format("Loaded {0} saved strokes", count.ToString()));
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

                new Thread(new ThreadStart(() =>
                {
                    Thread.Sleep(100);
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        ViewboxFloatingBarMarginAnimation();
                    });
                })).Start();
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
                try
                {
                    File.WriteAllText(folderPath + "/Position", previousSlideID.ToString());
                }
                catch { }
                Application.Current.Dispatcher.Invoke(() =>
                {
                    try
                    {
                        MemoryStream ms = new MemoryStream();
                        inkCanvas.Strokes.Save(ms);
                        ms.Position = 0;
                        memoryStreams[currentShowPosition] = ms;
                    }
                    catch { }
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
                                byte[] srcBuf = new byte[memoryStreams[i].Length];
                                int byteLength = memoryStreams[i].Read(srcBuf, 0, srcBuf.Length);

                                if (File.Exists(icartFilePath))
                                {
                                    File.WriteAllBytes(icartFilePath, srcBuf);
                                    LogHelper.WriteLogToFile(string.Format("Saved strokes for Slide {0} as .icart, size={1}, byteLength={2}", i.ToString(), memoryStreams[i].Length, byteLength));
                                }
                                else
                                {
                                    File.WriteAllBytes(icstkFilePath, srcBuf);
                                    LogHelper.WriteLogToFile(string.Format("Saved strokes for Slide {0} as .icstk, size={1}, byteLength={2}", i.ToString(), memoryStreams[i].Length, byteLength));
                                }
                            }
                            else
                            {
                                File.Delete(icartFilePath);
                                File.Delete(icstkFilePath);
                            }
                        }
                        catch (Exception ex)
                        {
                            LogHelper.WriteLogToFile(string.Format("Failed to save strokes for Slide {0}\n{1}", i, ex.ToString()), LogHelper.LogType.Error);
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
        }

        int previousSlideID = 0;
        MemoryStream[] memoryStreams = new MemoryStream[50];

        private void PptApplication_SlideShowNextSlide(SlideShowWindow Wn)
        {
            LogHelper.WriteLogToFile(string.Format("PowerPoint Next Slide (Slide {0})", Wn.View.CurrentShowPosition), LogHelper.LogType.Event);
            if (Wn.View.CurrentShowPosition != previousSlideID)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MemoryStream ms = new MemoryStream();
                    inkCanvas.Strokes.Save(ms);
                    ms.Position = 0;
                    memoryStreams[previousSlideID] = ms;

                    if (inkCanvas.Strokes.Count > Settings.Automation.MinimumAutomationStrokeNumber && Settings.PowerPointSettings.IsAutoSaveScreenShotInPowerPoint && !_isPptClickingBtnTurned)
                        SavePPTScreenshot(Wn.Presentation.Name + "/" + Wn.View.CurrentShowPosition);
                    _isPptClickingBtnTurned = false;

                    ClearStrokes(true);
                    timeMachine.ClearStrokeHistory();

                    try
                    {
                        if (memoryStreams[Wn.View.CurrentShowPosition] != null && memoryStreams[Wn.View.CurrentShowPosition].Length > 0)
                        {
                            inkCanvas.Strokes.Add(new StrokeCollection(memoryStreams[Wn.View.CurrentShowPosition]));
                        }
                        currentShowPosition = Wn.View.CurrentShowPosition;
                    }
                    catch { }

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

            try
            {
                new Thread(new ThreadStart(() =>
                {
                    try
                    {
                        pptApplication.SlideShowWindows[1].Activate();
                    }
                    catch { }
                    try
                    {
                        pptApplication.SlideShowWindows[1].View.Previous();
                    }
                    catch { } // Without this catch{}, app will crash when click the pre-page button in the fir page in some special env.
                })).Start();
            }
            catch { }
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
            try
            {
                new Thread(new ThreadStart(() =>
                {
                    try
                    {
                        pptApplication.SlideShowWindows[1].Activate();
                    }
                    catch { }
                    try
                    {
                        pptApplication.SlideShowWindows[1].View.Next();
                    }
                    catch { }
                })).Start();
            }
            catch { }
        }


        private async void PPTNavigationBtn_Click(object sender, MouseButtonEventArgs e)
        {
            if (lastBorderMouseDownObject != sender) return;
            Main_Grid.Background = new SolidColorBrush(StringToColor("#01FFFFFF"));
            CursorIcon_Click(null, null);
            try
            {
                pptApplication.SlideShowWindows[1].SlideNavigation.Visible = true;
            }
            catch { }
            // 控制居中
            if (!isFloatingBarFolded)
            {
                await Task.Delay(100);
                ViewboxFloatingBarMarginAnimation();
            }
        }

        /*
        private void BtnPPTSlideShow_Click(object sender, RoutedEventArgs e)
        {
            new Thread(new ThreadStart(() =>
            {
                try
                {
                    presentation.SlideShowSettings.Run();
                }
                catch { }
            })).Start();
        }
        */

        private async void BtnPPTSlideShowEnd_Click(object sender, RoutedEventArgs e)
        {
            new Thread(new ThreadStart(() =>
            {
                try
                {
                    pptApplication.SlideShowWindows[1].View.Exit();
                }
                catch { }
            })).Start();

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
