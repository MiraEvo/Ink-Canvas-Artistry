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
            Application application = Application.Current;
            MainWindow mainWindow = application?.MainWindow as MainWindow;
            if (mainWindow != null && application != null)
            {
                if (mainWindow.GetMainWindowTheme() == "Light")
                {
                    ThemeManager.SetRequestedTheme(this, ElementTheme.Light);
                    ResourceDictionary rd = new ResourceDictionary() { Source = new Uri("Resources/Styles/Light-PopupWindow.xaml", UriKind.Relative) };
                    application.Resources.MergedDictionaries.Add(rd);
                }
                else
                {
                    ThemeManager.SetRequestedTheme(this, ElementTheme.Dark);
                    ResourceDictionary rd = new ResourceDictionary() { Source = new Uri("Resources/Styles/Dark-PopupWindow.xaml", UriKind.Relative) };
                    application.Resources.MergedDictionaries.Add(rd);
                }
            }
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
    }
}
