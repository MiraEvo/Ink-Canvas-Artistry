using Microsoft.Win32;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Ink_Canvas.Helpers
{
    internal class SoftwareLauncher
    {
        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

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

            foreach (string subkeyName in key.GetSubKeyNames())
            {
                using RegistryKey subkey = key.OpenSubKey(subkeyName);
                if (subkey == null)
                {
                    continue;
                }

                string displayName = subkey.GetValue("DisplayName") as string;
                string installLocation = subkey.GetValue("InstallLocation") as string;
                string uninstallString = subkey.GetValue("UninstallString") as string;

                if (string.IsNullOrEmpty(displayName) || !displayName.Contains(softwareName))
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(installLocation))
                {
                    return Path.Combine(installLocation, "sweclauncher.exe");
                }

                if (!string.IsNullOrEmpty(uninstallString))
                {
                    int lastSlashIndex = uninstallString.LastIndexOf("\\", StringComparison.Ordinal);
                    if (lastSlashIndex >= 0)
                    {
                        string folderPath = uninstallString[..lastSlashIndex];
                        return Path.Combine(folderPath, "sweclauncher", "sweclauncher.exe");
                    }
                }

                break;
            }

            return null;
        }
    }
}
