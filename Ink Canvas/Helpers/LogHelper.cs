using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Security;

namespace Ink_Canvas.Helpers
{
    internal static class LogHelper
    {
        public static readonly string LogFile = "Log.txt";

        public static void NewLog(string str)
        {
            WriteLogToFile(str, LogType.Info);
        }

        public static void NewLog(Exception ex)
        {
            WriteLogToFile(ex, null, LogType.Error);
        }

        public static void WriteLogToFile(string str, LogType logType = LogType.Info)
        {
            try
            {
                WriteLine($"{DateTime.Now:O} [{GetLogTypeName(logType)}] {str}");
            }
            catch (IOException ex)
            {
                Debug.WriteLine($"LogHelper IO error: {ex}");
            }
            catch (UnauthorizedAccessException ex)
            {
                Debug.WriteLine($"LogHelper access error: {ex}");
            }
            catch (SecurityException ex)
            {
                Debug.WriteLine($"LogHelper security error: {ex}");
            }
        }

        public static void WriteLogToFile(Exception exception, string? context = null, LogType logType = LogType.Error)
        {
            if (exception == null)
            {
                return;
            }

            string message = string.IsNullOrWhiteSpace(context)
                ? exception.ToString()
                : $"{context} | {exception}";
            WriteLogToFile(message, logType);
        }

        public static void WriteObjectLogToFile(object obj, LogType logType = LogType.Info)
        {
            try
            {
                WriteLine($"{DateTime.Now:O} [{GetLogTypeName(logType)}] Object Log:");
                if (obj == null)
                {
                    WriteLine("null");
                    return;
                }

                PropertyInfo[] properties = obj.GetType().GetProperties();
                foreach (PropertyInfo property in properties)
                {
                    object value;
                    try
                    {
                        value = property.GetValue(obj, null);
                    }
                    catch (TargetInvocationException ex)
                    {
                        value = $"<error: {ex.InnerException?.Message ?? ex.Message}>";
                    }

                    WriteLine($"{property.Name}: {value}");
                }
            }
            catch (IOException ex)
            {
                Debug.WriteLine($"LogHelper IO error: {ex}");
            }
            catch (UnauthorizedAccessException ex)
            {
                Debug.WriteLine($"LogHelper access error: {ex}");
            }
            catch (SecurityException ex)
            {
                Debug.WriteLine($"LogHelper security error: {ex}");
            }
        }

        private static string GetLogTypeName(LogType logType)
        {
            return logType switch
            {
                LogType.Event => "Event",
                LogType.Trace => "Trace",
                LogType.Error => "Error",
                _ => "Info"
            };
        }

        private static void WriteLine(string line)
        {
            if (!Directory.Exists(App.RootPath))
            {
                Directory.CreateDirectory(App.RootPath);
            }

            string filePath = Path.Combine(App.RootPath, LogFile);
            using StreamWriter streamWriter = new StreamWriter(filePath, true);
            streamWriter.WriteLine(line);
        }

        public enum LogType
        {
            Info,
            Trace,
            Error,
            Event
        }
    }
}
