using Ink_Canvas.Helpers;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;

namespace Ink_Canvas
{
    public partial class MainWindow : Window
    {
        private async void FoldFloatingBar_Click(object sender, RoutedEventArgs e)
        {
            ConfigureFoldRequest(sender);
            if (isFloatingBarChangingHideMode)
            {
                return;
            }

            isFloatingBarChangingHideMode = true;

            try
            {
                await ExecuteFloatingBarTransitionAsync(
                    () =>
                    {
                        HideSubPanelsImmediately();
                        isFloatingBarFolded = true;

                        if (ShellViewModel.IsBlackboardMode)
                        {
                            ImageBlackboard_Click(null, null);
                        }

                        if (StackPanelCanvasControls.Visibility == Visibility.Visible
                            && foldFloatingBarByUser
                            && inkCanvas.Strokes.Count > 2)
                        {
                            ShowNotificationAsync("正在清空墨迹并收纳至侧边栏，可进入批注模式后通过【撤销】功能来恢复原先墨迹。");
                        }

                        CursorWithDelIcon_Click(null, null);
                        _ = AnimateSidePanelMarginsAsync(-16);
                    },
                    () =>
                    {
                        HidePresentationNavigation();
                        ViewboxFloatingBarMarginAnimation();
                        HideSubPanels("cursor");
                        _ = AnimateSidePanelMarginsAsync(-16);
                    });
            }
            finally
            {
                isFloatingBarChangingHideMode = false;
            }
        }

        private async void UnFoldFloatingBar_MouseUp(object sender, MouseButtonEventArgs e)
        {
            ConfigureUnfoldRequest(sender);
            if (isFloatingBarChangingHideMode)
            {
                return;
            }

            isFloatingBarChangingHideMode = true;

            try
            {
                await ExecuteFloatingBarTransitionAsync(
                    () => isFloatingBarFolded = false,
                    () =>
                    {
                        ShowPresentationNavigationIfNeeded();
                        ViewboxFloatingBarMarginAnimation();
                        _ = AnimateSidePanelMarginsAsync(-40);
                    });
            }
            finally
            {
                isFloatingBarChangingHideMode = false;
            }
        }

        private void ConfigureFoldRequest(object? sender)
        {
            if (sender == null)
            {
                foldFloatingBarByUser = false;
                AutomationViewModel?.SetFloatingBarFoldRequestedByAutomation(true);
            }
            else
            {
                foldFloatingBarByUser = true;
                AutomationViewModel?.SetFloatingBarFoldRequestedByAutomation(false);
            }

            unfoldFloatingBarByUser = false;
        }

        private void ConfigureUnfoldRequest(object? sender)
        {
            unfoldFloatingBarByUser = sender != null && !IsPresentationSlideShowRunning;
            foldFloatingBarByUser = false;
            AutomationViewModel?.SetFloatingBarFoldRequestedByAutomation(false);
        }

        private async Task ExecuteFloatingBarTransitionAsync(Action startAction, Action finishAction)
        {
            await Dispatcher.InvokeAsync(startAction);
            await Task.Delay(500);
            await Dispatcher.InvokeAsync(finishAction);
        }

        private void HidePresentationNavigation()
        {
            PPTNavigationBottomLeft.Visibility = Visibility.Collapsed;
            PPTNavigationBottomRight.Visibility = Visibility.Collapsed;
            PPTNavigationSidesLeft.Visibility = Visibility.Collapsed;
            PPTNavigationSidesRight.Visibility = Visibility.Collapsed;
        }

        private void ShowPresentationNavigationIfNeeded()
        {
            if (!IsPresentationSlideShowRunning)
            {
                return;
            }

            if (Settings.PowerPointSettings.IsShowBottomPPTNavigationPanel)
            {
                AnimationsHelper.ShowWithScaleFromBottom(PPTNavigationBottomLeft);
                AnimationsHelper.ShowWithScaleFromBottom(PPTNavigationBottomRight);
            }

            if (Settings.PowerPointSettings.IsShowSidePPTNavigationPanel)
            {
                AnimationsHelper.ShowWithScaleFromLeft(PPTNavigationSidesLeft);
                AnimationsHelper.ShowWithScaleFromRight(PPTNavigationSidesRight);
            }
        }

        private async Task AnimateSidePanelMarginsAsync(int marginFromEdge)
        {
            await Dispatcher.InvokeAsync(() =>
            {
                if (marginFromEdge == -16)
                {
                    LeftSidePanel.Visibility = Visibility.Visible;
                }

                ThicknessAnimation leftSidePanelMarginAnimation = new()
                {
                    Duration = TimeSpan.FromSeconds(0.3),
                    From = LeftSidePanel.Margin,
                    To = new Thickness(marginFromEdge, 0, 0, -150)
                };
                ThicknessAnimation rightSidePanelMarginAnimation = new()
                {
                    Duration = TimeSpan.FromSeconds(0.3),
                    From = RightSidePanel.Margin,
                    To = new Thickness(0, 0, marginFromEdge, -150)
                };

                LeftSidePanel.BeginAnimation(FrameworkElement.MarginProperty, leftSidePanelMarginAnimation);
                RightSidePanel.BeginAnimation(FrameworkElement.MarginProperty, rightSidePanelMarginAnimation);
            });

            await Task.Delay(600);

            await Dispatcher.InvokeAsync(() =>
            {
                LeftSidePanel.Margin = new Thickness(marginFromEdge, 0, 0, -150);
                RightSidePanel.Margin = new Thickness(0, 0, marginFromEdge, -150);

                if (marginFromEdge == -40)
                {
                    LeftSidePanel.Visibility = Visibility.Collapsed;
                }
            });
        }
    }
}
