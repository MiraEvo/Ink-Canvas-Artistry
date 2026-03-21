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
            return PathSafetyHelper.ResolveRelativePath(
                Environment.GetFolderPath(Environment.SpecialFolder.Startup),
                PathSafetyHelper.NormalizeLeafName(safeExeName + ".lnk", "Ink Canvas Modern.lnk"));
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

        public bool StartAutomaticallyCreate(string exeName)
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
                mainWindowLogger.Error(ex, $"Startup | Invalid startup entry name '{exeName}'");
            }
            catch (IOException ex)
            {
                mainWindowLogger.Error(ex, $"Startup | Failed to create startup entry '{exeName}'");
            }
            catch (UnauthorizedAccessException ex)
            {
                mainWindowLogger.Error(ex, $"Startup | Access denied while creating startup entry '{exeName}'");
            }
            catch (SecurityException ex)
            {
                mainWindowLogger.Error(ex, $"Startup | Security error while creating startup entry '{exeName}'");
            }

            return false;
        }

        public bool StartAutomaticallyDel(string exeName)
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
                mainWindowLogger.Error(ex, $"Startup | Invalid startup entry name '{exeName}'");
            }
            catch (IOException ex)
            {
                mainWindowLogger.Error(ex, $"Startup | Failed to delete startup entry '{exeName}'");
            }
            catch (UnauthorizedAccessException ex)
            {
                mainWindowLogger.Error(ex, $"Startup | Access denied while deleting startup entry '{exeName}'");
            }
            catch (SecurityException ex)
            {
                mainWindowLogger.Error(ex, $"Startup | Security error while deleting startup entry '{exeName}'");
            }

            return false;
        }

        public bool NormalizeStartupRegistration()
        {
            bool hasLegacyRegistration = StartupEntryExists("InkCanvas")
                || StartupEntryExists("Ink Canvas Annotation")
                || StartupEntryExists("Ink Canvas Artistry")
                || File.Exists(GetStartupShortcutPath("InkCanvas"))
                || File.Exists(GetStartupShortcutPath("Ink Canvas Annotation"))
                || File.Exists(GetStartupShortcutPath("Ink Canvas Artistry"));
            bool hasCurrentRegistration = StartupEntryExists("Ink Canvas Modern")
                || File.Exists(GetStartupShortcutPath("Ink Canvas Modern"));

            if (hasLegacyRegistration)
            {
                StartAutomaticallyDel("InkCanvas");
                StartAutomaticallyDel("Ink Canvas Annotation");
                StartAutomaticallyDel("Ink Canvas Artistry");
                StartAutomaticallyCreate("Ink Canvas Modern");
                return true;
            }

            if (hasCurrentRegistration)
            {
                StartAutomaticallyCreate("Ink Canvas Modern");
                return true;
            }

            return false;
        }
    }
}
