using Ink_Canvas.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using iNKORE.UI.WPF.Modern;
using System.Windows.Media;

namespace Ink_Canvas {
    /// <summary>
    /// Interaction logic for RandWindow.xaml
    /// </summary>
    public partial class RandWindow : Window {
        private readonly CancellationTokenSource windowLifetimeCancellationTokenSource = new CancellationTokenSource();
        private bool isRandomizing;

        public RandWindow() {
            InitializeComponent();
            AnimationsHelper.ShowWithSlideFromBottomAndFade(this, 0.25);
            ApplyThemeFromMainWindow();
        }

        public RandWindow(bool IsAutoClose) {
            InitializeComponent();
            ApplyThemeFromMainWindow();
            isAutoClose = IsAutoClose;
            _ = TriggerInitialRandomizeAsync(windowLifetimeCancellationTokenSource.Token);
        }

        public static int randSeed = 0;
        public bool isAutoClose = false;
        public bool isNotRepeatName = false;

        public int TotalCount = 1;
        public int PeopleCount = 60;
        public List<string> Names = new List<string>();

        private void BorderBtnAdd_MouseUp(object sender, MouseButtonEventArgs e) {
            if (TotalCount >= PeopleCount) return;
            TotalCount++;
            LabelNumberCount.Text = TotalCount.ToString();
        }

        private void BorderBtnMinus_MouseUp(object sender, MouseButtonEventArgs e) {
            if (TotalCount < 2) return;
            TotalCount--;
            LabelNumberCount.Text = TotalCount.ToString();
        }

        private async void BorderBtnRand_MouseUp(object sender, MouseButtonEventArgs e) {
            if (isRandomizing)
            {
                return;
            }

            Random random = new Random();// randSeed + DateTime.Now.Millisecond / 10 % 10);
            string outputString = "";
            List<string> outputs = new List<string>();
            List<int> rands = new List<int>();
            CancellationToken cancellationToken = windowLifetimeCancellationTokenSource.Token;

            LabelOutput2.Visibility = Visibility.Collapsed;
            LabelOutput3.Visibility = Visibility.Collapsed;
            BorderBtnRandCover.Visibility = Visibility.Visible;
            isRandomizing = true;

            try
            {
                for (int i = 0; i < 5; i++) {
                    int rand = random.Next(1, PeopleCount + 1);
                    while (rands.Contains(rand)) {
                        rand = random.Next(1, PeopleCount + 1);
                    }
                    rands.Add(rand);
                    if (rands.Count >= PeopleCount) rands = new List<int>();
                    LabelOutput.Content = Names.Count != 0 ? Names[rand - 1] : rand.ToString();
                    await Task.Delay(150, cancellationToken);
                }

                rands = new List<int>();
                for (int i = 0; i < TotalCount; i++) {
                    int rand = random.Next(1, PeopleCount + 1);
                    while (rands.Contains(rand)) {
                        rand = random.Next(1, PeopleCount + 1);
                    }
                    rands.Add(rand);
                    if (rands.Count >= PeopleCount) rands = new List<int>();

                    if (Names.Count != 0) {
                        outputs.Add(Names[rand - 1]);
                        outputString += Names[rand - 1] + Environment.NewLine;
                    } else {
                        outputs.Add(rand.ToString());
                        outputString += rand.ToString() + Environment.NewLine;
                    }
                }

                if (TotalCount <= 5) {
                    LabelOutput.Content = outputString.Trim();
                } else if (TotalCount <= 10) {
                    LabelOutput2.Visibility = Visibility.Visible;
                    outputString = "";
                    for (int i = 0; i < (outputs.Count + 1) / 2; i++) {
                        outputString += outputs[i] + Environment.NewLine;
                    }
                    LabelOutput.Content = outputString.Trim();
                    outputString = "";
                    for (int i = (outputs.Count + 1) / 2; i < outputs.Count; i++) {
                        outputString += outputs[i] + Environment.NewLine;
                    }
                    LabelOutput2.Content = outputString.Trim();
                } else {
                    LabelOutput2.Visibility = Visibility.Visible;
                    LabelOutput3.Visibility = Visibility.Visible;
                    outputString = "";
                    for (int i = 0; i < (outputs.Count + 1) / 3; i++) {
                        outputString += outputs[i] + Environment.NewLine;
                    }
                    LabelOutput.Content = outputString.Trim();
                    outputString = "";
                    for (int i = (outputs.Count + 1) / 3; i < (outputs.Count + 1) * 2 / 3; i++) {
                        outputString += outputs[i] + Environment.NewLine;
                    }
                    LabelOutput2.Content = outputString.Trim();
                    outputString = "";
                    for (int i = (outputs.Count + 1) * 2 / 3; i < outputs.Count; i++) {
                        outputString += outputs[i] + Environment.NewLine;
                    }
                    LabelOutput3.Content = outputString.Trim();
                }

                if (isAutoClose) {
                    await Task.Delay(1500, cancellationToken);
                    Close();
                }
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                BorderBtnRandCover.Visibility = Visibility.Collapsed;
                isRandomizing = false;
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e) {
            Names = new List<string>();
            if (File.Exists(App.RootPath + "Names.txt")) {
                string[] fileNames = File.ReadAllLines(App.RootPath + "Names.txt");
                string[] replaces = new string[0];

                if (File.Exists(App.RootPath + "Replace.txt")) {
                    replaces = File.ReadAllLines(App.RootPath + "Replace.txt");
                }

                //Fix emtpy lines
                foreach (string str in fileNames) {
                    string s = str;
                    //Make replacement
                    foreach (string replace in replaces) {
                        int separatorIndex = replace.IndexOf("-->", StringComparison.Ordinal);
                        if (separatorIndex > 0 && s == replace.Substring(0, separatorIndex)) {
                            s = replace.Substring(separatorIndex + 3);
                        }
                    }

                    if (s != "") Names.Add(s);
                }

                PeopleCount = Names.Count();
                TextBlockPeopleCount.Text = PeopleCount.ToString();
                if (PeopleCount == 0) {
                    PeopleCount = 60;
                    TextBlockPeopleCount.Text = "点击此处以导入名单";
                }
            }
        }

        private void BorderBtnHelp_MouseUp(object sender, MouseButtonEventArgs e) {
            new NamesInputWindow().ShowDialog();
            Window_Loaded(this, null);
        }

        private void BtnClose_MouseUp(object sender, MouseButtonEventArgs e) {
            Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            windowLifetimeCancellationTokenSource.Cancel();
            windowLifetimeCancellationTokenSource.Dispose();
            base.OnClosed(e);
        }

        private void ApplyThemeFromMainWindow()
        {
            Application application = Application.Current;
            MainWindow mainWindow = application?.MainWindow as MainWindow;
            if (mainWindow == null || application == null)
            {
                return;
            }

            string resourcePath;
            if (mainWindow.GetMainWindowTheme() == "Light")
            {
                ThemeManager.SetRequestedTheme(this, ElementTheme.Light);
                resourcePath = "Resources/Styles/Window/Light-PopupWindow.xaml";
            }
            else
            {
                ThemeManager.SetRequestedTheme(this, ElementTheme.Dark);
                resourcePath = "Resources/Styles/Window/Dark-PopupWindow.xaml";
            }

            ResourceDictionary resourceDictionary = new ResourceDictionary
            {
                Source = new Uri(resourcePath, UriKind.Relative)
            };
            application.Resources.MergedDictionaries.Add(resourceDictionary);
        }

        private async Task TriggerInitialRandomizeAsync(CancellationToken cancellationToken)
        {
            await Task.Delay(100, cancellationToken);
            BorderBtnRand_MouseUp(BorderBtnRand, null);
        }
    }
}

