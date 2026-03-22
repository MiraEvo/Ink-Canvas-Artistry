using Microsoft.Win32;
using System;
using System.IO;
using System.Linq;

namespace Ink_Canvas.Features.Automation.Services
{
    internal static class SoftwareLauncher
    {
        public static void LaunchEasiCamera(string softwareName)
        {
            string executablePath = FindEasiCameraExecutablePath(softwareName);

            if (string.IsNullOrEmpty(executablePath))
            {
                return;
            }

            try
            {
                ProcessHelper.StartWithShell(executablePath);
            }
            catch (FileNotFoundException ex)
            {
                Console.WriteLine("启动失败: " + ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine("启动失败: " + ex.Message);
            }
        }

        private static string FindEasiCameraExecutablePath(string softwareName)
        {
            using RegistryKey key = Registry.LocalMachine.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall");
            if (key == null)
            {
                return null;
            }

            foreach (RegistryKey subkey in key.GetSubKeyNames()
                         .Select(subkeyName => key.OpenSubKey(subkeyName))
                         .Where(static openedSubKey => openedSubKey != null)
                         .Select(static openedSubKey => openedSubKey!))
            {
                using (subkey)
                {
                    string displayName = subkey.GetValue("DisplayName") as string;
                    string installLocation = subkey.GetValue("InstallLocation") as string;
                    string uninstallString = subkey.GetValue("UninstallString") as string;

                    if (string.IsNullOrEmpty(displayName) || !displayName.Contains(softwareName))
                    {
                        continue;
                    }

                    if (!string.IsNullOrEmpty(installLocation))
                    {
                        return Path.Join(installLocation, "sweclauncher.exe");
                    }

                    if (!string.IsNullOrEmpty(uninstallString))
                    {
                        string folderPath = TryGetDirectoryFromUninstallString(uninstallString);
                        if (!string.IsNullOrWhiteSpace(folderPath))
                        {
                            return Path.Join(folderPath, "sweclauncher", "sweclauncher.exe");
                        }
                    }

                    break;
                }
            }

            return null;
        }

        private static string TryGetDirectoryFromUninstallString(string uninstallString)
        {
            string trimmedValue = uninstallString.Trim();
            if (trimmedValue.Length == 0)
            {
                return null;
            }

            string executablePath = trimmedValue;
            if (trimmedValue[0] == '"')
            {
                int closingQuoteIndex = trimmedValue.IndexOf('"', 1);
                if (closingQuoteIndex > 1)
                {
                    executablePath = trimmedValue[1..closingQuoteIndex];
                }
            }
            else
            {
                int exeIndex = trimmedValue.IndexOf(".exe", StringComparison.OrdinalIgnoreCase);
                if (exeIndex > 0)
                {
                    executablePath = trimmedValue[..(exeIndex + 4)];
                }
            }

            return Path.GetDirectoryName(executablePath);
        }
    }
}

