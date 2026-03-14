using Ink_Canvas.Services.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows;
using MessageBox = System.Windows.MessageBox;

namespace Ink_Canvas.Services.ErrorHandling
{
    public sealed class AppErrorHandler
    {
        private sealed class RateLimitState
        {
            public DateTime WindowStartUtc { get; set; }

            public int DetailedLogCount { get; set; }

            public int SuppressedCount { get; set; }
        }

        private const int MaxDetailedLogsPerWindow = 3;
        private static readonly TimeSpan RateLimitWindow = TimeSpan.FromSeconds(60);

        private readonly IAppLogger logger;
        private readonly object rateLimitSyncRoot = new();
        private readonly Dictionary<string, RateLimitState> rateLimitStates = [];

        private Action<string>? notificationSink;
        private int isHandlingFatalError;

        public AppErrorHandler(IAppLogger logger)
        {
            this.logger = (logger ?? throw new ArgumentNullException(nameof(logger))).ForCategory(nameof(AppErrorHandler));
        }

        public void RegisterNotificationSink(Action<string> sink)
        {
            notificationSink = sink ?? throw new ArgumentNullException(nameof(sink));
        }

        public void NotifyUser(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            try
            {
                notificationSink?.Invoke(message);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error Handling | Failed to deliver non-fatal notification");
            }
        }

        public void Handle(Exception exception, AppErrorContext context)
        {
            ArgumentNullException.ThrowIfNull(exception);
            ArgumentNullException.ThrowIfNull(context);

            if (context.IsFatal || context.Severity == AppErrorSeverity.Fatal)
            {
                HandleFatal(exception, context);
                return;
            }

            if (context.AllowRateLimit && ShouldSuppressDetailedLog(exception, context, out string? summaryMessage))
            {
                if (!string.IsNullOrWhiteSpace(summaryMessage))
                {
                    logger.Error(summaryMessage);
                }

                return;
            }

            LogException(exception, context, force: false);
            if (context.ShouldNotifyUser && !string.IsNullOrWhiteSpace(context.UserMessage))
            {
                NotifyUser(context.UserMessage);
            }
        }

        public void HandleCurrentDomainException(Exception exception, string operation)
        {
            Handle(exception, new AppErrorContext(nameof(App), operation)
            {
                Severity = AppErrorSeverity.Fatal,
                IsFatal = true,
                ShouldNotifyUser = true,
                UserMessage = "程序发生未处理异常，建议重启后再试。"
            });
        }

        private void HandleFatal(Exception exception, AppErrorContext context)
        {
            LogException(exception, context, force: true);

            if (Interlocked.Exchange(ref isHandlingFatalError, 1) == 1)
            {
                return;
            }

            try
            {
                string fatalMessage = string.IsNullOrWhiteSpace(context.UserMessage)
                    ? "程序发生未处理异常，建议重启后再试。"
                    : context.UserMessage;

                Application? application = Application.Current;
                if (application?.Dispatcher is { } dispatcher)
                {
                    if (dispatcher.CheckAccess())
                    {
                        ShowFatalMessageAndShutdown(application, fatalMessage);
                    }
                    else
                    {
                        dispatcher.Invoke(() => ShowFatalMessageAndShutdown(application, fatalMessage));
                    }

                    return;
                }

                MessageBox.Show(fatalMessage, "Ink Canvas Artistry", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error Handling | Failed while presenting fatal error UI", force: true);
            }
            finally
            {
                if (Application.Current != null)
                {
                    Application.Current.Shutdown();
                }
                else
                {
                    Environment.Exit(1);
                }
            }
        }

        private static void ShowFatalMessageAndShutdown(Application application, string fatalMessage)
        {
            MessageBox.Show(fatalMessage, "Ink Canvas Artistry", MessageBoxButton.OK, MessageBoxImage.Error);
            application.Shutdown();
        }

        private void LogException(Exception exception, AppErrorContext context, bool force)
        {
            logger.Error(
                exception,
                $"{context.Source} | {context.Operation} | Severity={context.Severity} | Fatal={context.IsFatal}",
                force);
        }

        private bool ShouldSuppressDetailedLog(Exception exception, AppErrorContext context, out string? summaryMessage)
        {
            summaryMessage = null;

            string rateLimitKey = context.ResolveRateLimitKey(exception);
            DateTime utcNow = DateTime.UtcNow;

            lock (rateLimitSyncRoot)
            {
                if (!rateLimitStates.TryGetValue(rateLimitKey, out RateLimitState? state))
                {
                    rateLimitStates[rateLimitKey] = new RateLimitState
                    {
                        WindowStartUtc = utcNow,
                        DetailedLogCount = 1
                    };
                    return false;
                }

                if (utcNow - state.WindowStartUtc >= RateLimitWindow)
                {
                    if (state.SuppressedCount > 0)
                    {
                        summaryMessage =
                            $"Error RateLimit | Key={rateLimitKey} | In the past {RateLimitWindow.TotalSeconds:0} seconds, suppressed {state.SuppressedCount} additional matching errors.";
                    }

                    state.WindowStartUtc = utcNow;
                    state.DetailedLogCount = 1;
                    state.SuppressedCount = 0;
                    return false;
                }

                if (state.DetailedLogCount < MaxDetailedLogsPerWindow)
                {
                    state.DetailedLogCount++;
                    return false;
                }

                state.SuppressedCount++;
                return true;
            }
        }
    }
}
