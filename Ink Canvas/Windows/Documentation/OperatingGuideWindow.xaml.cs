using Ink_Canvas.Helpers;
using iNKORE.UI.WPF.Modern;
using System;
using System.Windows;
using System.Windows.Input;

namespace Ink_Canvas
{
    /// <summary>
    /// Interaction logic for StopwatchWindow.xaml
    /// </summary>
    public partial class OperatingGuideWindow : Window
    {
        public OperatingGuideWindow()
        {
            InitializeComponent();
            AnimationsHelper.ShowWithSlideFromBottomAndFade(this, 0.25);
            ApplyThemeFromMainWindow();
        }

        private void BtnClose_MouseUp(object sender, MouseButtonEventArgs e)
        {
            Close();
        }

        private void WindowDragMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed) DragMove();
        }

        private void SCManipulationBoundaryFeedback(object sender, ManipulationBoundaryFeedbackEventArgs e) {
            e.Handled = true;
        }

        private void ApplyThemeFromMainWindow()
        {
            Application application = Application.Current;
            if (application?.MainWindow is not MainWindow mainWindow)
            {
                return;
            }

            bool isLightTheme = mainWindow.GetMainWindowTheme() == "Light";
            ThemeManager.SetRequestedTheme(this, isLightTheme ? ElementTheme.Light : ElementTheme.Dark);

            ResourceDictionary resourceDictionary = new()
            {
                Source = new Uri(
                    isLightTheme
                        ? "Resources/Styles/Window/Light-PopupWindow.xaml"
                        : "Resources/Styles/Window/Dark-PopupWindow.xaml",
                    UriKind.Relative)
            };
            application.Resources.MergedDictionaries.Add(resourceDictionary);
        }
    }
}

