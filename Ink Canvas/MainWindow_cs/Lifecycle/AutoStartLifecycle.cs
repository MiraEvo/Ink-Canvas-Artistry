using Ink_Canvas.Helpers;
using Microsoft.Win32;
using System;
using System.IO;
using System.Security;
using System.Windows;

namespace Ink_Canvas
{
    public partial class MainWindow : Window
    {
        private const string StartupRegistryPath = @"Software\Microsoft\Windows\CurrentVersion\Run";

        private static string GetStartupShortcutPath(string exeName)
        {
            string safeExeName = Path.GetFileNameWithoutExtension(exeName);
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup), safeExeName + ".lnk");
        }

        private static bool StartupEntryExists(string exeName)
        {
            using RegistryKey registryKey = Registry.CurrentUser.OpenSubKey(StartupRegistryPath, false);
            return registryKey?.GetValue(exeName) is string value && !string.IsNullOrWhiteSpace(value);
        }

        private static void DeleteLegacyStartupShortcut(string exeName)
        {
            string shortcutPath = GetStartupShortcutPath(exeName);
            if (File.Exists(shortcutPath))
            {
                File.Delete(shortcutPath);
            }
        }

        public static bool StartAutomaticallyCreate(string exeName)
        {
            try
            {
                string executablePath = Environment.ProcessPath ?? System.Windows.Forms.Application.ExecutablePath;
                using RegistryKey registryKey = Registry.CurrentUser.CreateSubKey(StartupRegistryPath, true);
                registryKey?.SetValue(exeName, $"\"{executablePath}\"");
                DeleteLegacyStartupShortcut(exeName);
                return true;
            }
            catch (ArgumentException ex)
            {
                LogHelper.WriteLogToFile(ex, $"Startup | Invalid startup entry name '{exeName}'");
            }
            catch (IOException ex)
            {
                LogHelper.WriteLogToFile(ex, $"Startup | Failed to create startup entry '{exeName}'");
            }
            catch (UnauthorizedAccessException ex)
            {
                LogHelper.WriteLogToFile(ex, $"Startup | Access denied while creating startup entry '{exeName}'");
            }
            catch (SecurityException ex)
            {
                LogHelper.WriteLogToFile(ex, $"Startup | Security error while creating startup entry '{exeName}'");
            }

            return false;
        }

        public static bool StartAutomaticallyDel(string exeName)
        {
            try
            {
                using RegistryKey registryKey = Registry.CurrentUser.OpenSubKey(StartupRegistryPath, true);
                registryKey?.DeleteValue(exeName, false);
                DeleteLegacyStartupShortcut(exeName);
                return true;
            }
            catch (ArgumentException ex)
            {
                LogHelper.WriteLogToFile(ex, $"Startup | Invalid startup entry name '{exeName}'");
            }
            catch (IOException ex)
            {
                LogHelper.WriteLogToFile(ex, $"Startup | Failed to delete startup entry '{exeName}'");
            }
            catch (UnauthorizedAccessException ex)
            {
                LogHelper.WriteLogToFile(ex, $"Startup | Access denied while deleting startup entry '{exeName}'");
            }
            catch (SecurityException ex)
            {
                LogHelper.WriteLogToFile(ex, $"Startup | Security error while deleting startup entry '{exeName}'");
            }

            return false;
        }

        public static bool NormalizeStartupRegistration()
        {
            bool hasLegacyRegistration = StartupEntryExists("InkCanvas")
                || StartupEntryExists("Ink Canvas Annotation")
                || File.Exists(GetStartupShortcutPath("InkCanvas"))
                || File.Exists(GetStartupShortcutPath("Ink Canvas Annotation"));
            bool hasCurrentRegistration = StartupEntryExists("Ink Canvas Artistry")
                || File.Exists(GetStartupShortcutPath("Ink Canvas Artistry"));

            if (hasLegacyRegistration)
            {
                StartAutomaticallyDel("InkCanvas");
                StartAutomaticallyDel("Ink Canvas Annotation");
                StartAutomaticallyCreate("Ink Canvas Artistry");
                return true;
            }

            if (hasCurrentRegistration)
            {
                StartAutomaticallyCreate("Ink Canvas Artistry");
                return true;
            }

            return false;
        }
    }
}
