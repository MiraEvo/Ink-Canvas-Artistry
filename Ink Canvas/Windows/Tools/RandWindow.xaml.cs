using Ink_Canvas.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
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

        public static int RandomSeed { get; set; }
        public bool isAutoClose = false;
        public bool isNotRepeatName = false;

        public int TotalCount = 1;
        public int PeopleCount = 60;
        public List<string> Names = [];

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

            Random random = RandomSeed == 0 ? Random.Shared : new Random(RandomSeed);
            List<string> outputs = [];
            List<int> rands = [];
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
                    if (rands.Count >= PeopleCount) rands.Clear();
                    LabelOutput.Content = GetOutputValue(rand);
                    await Task.Delay(150, cancellationToken);
                }

                rands.Clear();
                for (int i = 0; i < TotalCount; i++) {
                    int rand = random.Next(1, PeopleCount + 1);
                    while (rands.Contains(rand)) {
                        rand = random.Next(1, PeopleCount + 1);
                    }
                    rands.Add(rand);
                    if (rands.Count >= PeopleCount) rands.Clear();
                    outputs.Add(GetOutputValue(rand));
                }

                RenderOutputs(outputs);

                if (isAutoClose) {
                    await Task.Delay(1500, cancellationToken);
                    Close();
                }
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine("RandWindow | Randomization was canceled.");
            }
            finally
            {
                BorderBtnRandCover.Visibility = Visibility.Collapsed;
                isRandomizing = false;
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e) {
            Names = [];
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

                    if (!string.IsNullOrEmpty(s)) Names.Add(s);
                }

                PeopleCount = Names.Count;
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
            if (application?.MainWindow is not MainWindow mainWindow)
            {
                return;
            }

            bool isLightTheme = mainWindow.GetMainWindowTheme() == "Light";
            ThemeManager.SetRequestedTheme(this, isLightTheme ? ElementTheme.Light : ElementTheme.Dark);

            ResourceDictionary resourceDictionary = new ResourceDictionary
            {
                Source = new Uri(
                    isLightTheme
                        ? "Resources/Styles/Window/Light-PopupWindow.xaml"
                        : "Resources/Styles/Window/Dark-PopupWindow.xaml",
                    UriKind.Relative)
            };
            application.Resources.MergedDictionaries.Add(resourceDictionary);
        }

        private async Task TriggerInitialRandomizeAsync(CancellationToken cancellationToken)
        {
            await Task.Delay(100, cancellationToken);
            BorderBtnRand_MouseUp(BorderBtnRand, null);
        }

        private string GetOutputValue(int rand)
        {
            return Names.Count != 0 ? Names[rand - 1] : rand.ToString();
        }

        private void RenderOutputs(IReadOnlyList<string> outputs)
        {
            if (outputs.Count <= 5)
            {
                LabelOutput.Content = BuildOutputChunk(outputs, 0, outputs.Count);
                return;
            }

            if (outputs.Count <= 10)
            {
                int middleIndex = (outputs.Count + 1) / 2;
                LabelOutput2.Visibility = Visibility.Visible;
                LabelOutput.Content = BuildOutputChunk(outputs, 0, middleIndex);
                LabelOutput2.Content = BuildOutputChunk(outputs, middleIndex, outputs.Count);
                return;
            }

            int firstSplit = (outputs.Count + 1) / 3;
            int secondSplit = (outputs.Count + 1) * 2 / 3;
            LabelOutput2.Visibility = Visibility.Visible;
            LabelOutput3.Visibility = Visibility.Visible;
            LabelOutput.Content = BuildOutputChunk(outputs, 0, firstSplit);
            LabelOutput2.Content = BuildOutputChunk(outputs, firstSplit, secondSplit);
            LabelOutput3.Content = BuildOutputChunk(outputs, secondSplit, outputs.Count);
        }

        private static string BuildOutputChunk(IReadOnlyList<string> outputs, int startIndex, int endIndex)
        {
            StringBuilder builder = new();
            for (int i = startIndex; i < endIndex; i++)
            {
                if (builder.Length > 0)
                {
                    builder.AppendLine();
                }

                builder.Append(outputs[i]);
            }

            return builder.ToString();
        }
    }
}

