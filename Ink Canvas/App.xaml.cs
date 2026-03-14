using Ink_Canvas.Services.Logging;
using iNKORE.UI.WPF.Modern.Controls;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
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

        public AppErrorHandler ErrorHandler { get; }

        public TaskGuard TaskGuard { get; }

        public UiDispatchGuard UiDispatchGuard { get; }

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
            ErrorHandler = new AppErrorHandler(appLogger);
            TaskGuard = new TaskGuard(ErrorHandler);
            UiDispatchGuard = new UiDispatchGuard(ErrorHandler);
            Startup += App_Startup;
            DispatcherUnhandledException += App_DispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
        }

        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            ErrorHandler.Handle(
                e.Exception,
                new AppErrorContext(nameof(App), "DispatcherUnhandledException")
                {
                    Severity = AppErrorSeverity.Fatal,
                    IsFatal = true,
                    ShouldNotifyUser = true,
                    UserMessage = "抱歉，程序发生未处理异常。建议重启应用后再继续使用。"
                });
            e.Handled = true;
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Exception exception = e.ExceptionObject as Exception
                ?? new InvalidOperationException("A non-exception object was raised as an unhandled exception.");
            ErrorHandler.HandleCurrentDomainException(exception, "AppDomain.CurrentDomain.UnhandledException");
        }

        private void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            ErrorHandler.Handle(
                e.Exception.GetBaseException(),
                new AppErrorContext(nameof(App), "TaskScheduler.UnobservedTaskException")
                {
                    AllowRateLimit = true
                });
            e.SetObserved();
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

            double scrollLines = System.Windows.Forms.SystemInformation.MouseWheelScrollLines;
            double offsetDelta = (e.Delta / 120d) * 10d * scrollLines;
            senderScrollViewer.ScrollToVerticalOffset(senderScrollViewer.VerticalOffset - offsetDelta);
            e.Handled = true;
        }
    }
}
