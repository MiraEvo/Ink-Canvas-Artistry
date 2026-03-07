using Microsoft.Win32;
using System;
using System.IO;
using System.Windows;

namespace Ink_Canvas
{
    public partial class MainWindow : Window
    {
        private const string StartupRegistryPath = @"Software\Microsoft\Windows\CurrentVersion\Run";

        private static string GetStartupShortcutPath(string exeName)
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup), exeName + ".lnk");
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
            catch (Exception) { }

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
            catch (Exception) { }

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
