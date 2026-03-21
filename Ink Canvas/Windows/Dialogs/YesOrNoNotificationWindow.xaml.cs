using iNKORE.UI.WPF.Modern;
using System;
using System.Windows;

namespace Ink_Canvas
{
    public partial class YesOrNoNotificationWindow : Window
    {
        private readonly Action? _yesAction;
        private readonly Action? _noAction;

        public YesOrNoNotificationWindow(string text, Action? yesAction = null, Action? noAction = null)
        {
            _yesAction = yesAction;
            _noAction = noAction;
            InitializeComponent();
            Label.Text = text;
            if (Application.Current?.MainWindow is MainWindow mainWindow)
            {
                ThemeManager.SetRequestedTheme(
                    this,
                    mainWindow.GetMainWindowTheme() == "Light" ? ElementTheme.Light : ElementTheme.Dark);
            }
        }

        private void ButtonYes_Click(object sender, RoutedEventArgs e)
        {
            _yesAction?.Invoke();
            Close();
        }

        private void ButtonNo_Click(object sender, RoutedEventArgs e)
        {
            _noAction?.Invoke();
            Close();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            MainWindow.IsShowingRestoreHiddenSlidesWindow = false;
        }
    }
}
