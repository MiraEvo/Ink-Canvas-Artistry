using Ink_Canvas.Services.Logging;
using iNKORE.UI.WPF.Modern.Controls;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using MessageBox = System.Windows.MessageBox;

namespace Ink_Canvas
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private System.Threading.Mutex mutex;
        private readonly IAppLogger appLogger;

        public static IReadOnlyList<string> StartArgs { get; private set; } = Array.Empty<string>();
        public static string RootPath { get; } = AppContext.BaseDirectory;

        public FileAppLogger Logger { get; }

        public App()
        {
            Logger = new FileAppLogger(new LogOptions
            {
                Enabled = true,
                DirectoryPath = Path.Join(AppContext.BaseDirectory, "Logs"),
                ActiveFileName = "Log.txt",
                MaxFileSizeBytes = 512 * 1024,
                RetainedArchiveCount = 5
            });
            appLogger = Logger.ForCategory(nameof(App));
            Startup += App_Startup;
            DispatcherUnhandledException += App_DispatcherUnhandledException;
        }

        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            Ink_Canvas.MainWindow.ShowNewMessage("抱歉，出现未预期的异常，可能导致 Ink Canvas 画板运行不稳定。\n建议保存墨迹后重启应用。", true);
            appLogger.Error(e.Exception, force: true);
            e.Handled = true;
        }

        private void App_Startup(object sender, StartupEventArgs e)
        {
            appLogger.Info(string.Format("Ink Canvas Starting (Version: {0})", Assembly.GetExecutingAssembly().GetName().Version));

            bool ret;
            mutex = new System.Threading.Mutex(true, "Ink_Canvas_Artistry", out ret);

            if (!ret && !e.Args.Contains("-m"))
            {
                appLogger.Info("Detected existing instance");
                MessageBox.Show("已有一个程序实例正在运行");
                appLogger.Info("Ink Canvas automatically closed");
                Environment.Exit(0);
            }

            StartArgs = e.Args;
        }

        private void ScrollViewer_PreviewMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            if (System.Windows.Forms.SystemInformation.MouseWheelScrollLines == -1)
            {
                e.Handled = false;
                return;
            }

            if (sender is not ScrollViewerEx senderScrollViewer)
            {
                return;
            }

            senderScrollViewer.ScrollToVerticalOffset(senderScrollViewer.VerticalOffset - e.Delta * 10 * System.Windows.Forms.SystemInformation.MouseWheelScrollLines / 120d);
            e.Handled = true;
        }
    }
}
