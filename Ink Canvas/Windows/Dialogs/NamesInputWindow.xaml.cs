using Ink_Canvas.Helpers;
using iNKORE.UI.WPF.Modern;
using System;
using System.IO;
using System.Windows;

namespace Ink_Canvas
{
    /// <summary>
    /// Interaction logic for NamesInputWindow.xaml
    /// </summary>
    public partial class NamesInputWindow : Window
    {
        public NamesInputWindow()
        {
            InitializeComponent();
            AnimationsHelper.ShowWithSlideFromBottomAndFade(this, 0.25);
            ApplyThemeFromMainWindow();
        }

        string originText = "";

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (File.Exists(App.RootPath + "Names.txt"))
            {
                TextBoxNames.Text = File.ReadAllText(App.RootPath + "Names.txt");
                originText = TextBoxNames.Text;
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (originText != TextBoxNames.Text)
            {
                var result = MessageBox.Show("是否保存？", "名单导入", MessageBoxButton.YesNo);
                if (result == MessageBoxResult.Yes)
                {
                    File.WriteAllText(App.RootPath + "Names.txt", TextBoxNames.Text);
                }
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ApplyThemeFromMainWindow()
        {
            Application? application = Application.Current;
            if (application is null || application.MainWindow is not MainWindow mainWindow)
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

