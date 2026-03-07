using Ink_Canvas.Helpers;
using Newtonsoft.Json;
using System;
using System.IO;

namespace Ink_Canvas.Services
{
    public sealed class JsonSettingsService(Func<string> settingsPathProvider) : ISettingsService
    {
        private readonly Func<string> settingsPathProvider = settingsPathProvider ?? throw new ArgumentNullException(nameof(settingsPathProvider));

        public Settings Load()
        {
            try
            {
                string settingsPath = GetSettingsPath();
                if (!File.Exists(settingsPath))
                {
                    return CreateRecommendedSettings();
                }

                return SettingsDefaults.Normalize(ReadSettings(settingsPath));
            }
            catch (IOException ex)
            {
                LogHelper.WriteLogToFile(ex, "Settings Load | Failed to read settings file");
                return CreateRecommendedSettings();
            }
            catch (UnauthorizedAccessException ex)
            {
                LogHelper.WriteLogToFile(ex, "Settings Load | Access denied for settings file");
                return CreateRecommendedSettings();
            }
            catch (JsonException ex)
            {
                LogHelper.WriteLogToFile(ex, "Settings Load | Invalid JSON in settings file");
                return CreateRecommendedSettings();
            }
            catch (ArgumentException ex)
            {
                LogHelper.WriteLogToFile(ex, "Settings Load | Invalid settings path");
                return CreateRecommendedSettings();
            }
        }

        public void Save(Settings settings)
        {
            ArgumentNullException.ThrowIfNull(settings);

            try
            {
                string settingsPath = GetSettingsPath();
                EnsureParentDirectoryExists(settingsPath);
                string text = JsonConvert.SerializeObject(SettingsDefaults.Normalize(settings), Formatting.Indented);
                File.WriteAllText(settingsPath, text);
            }
            catch (IOException ex)
            {
                LogHelper.WriteLogToFile(ex, "Settings Save | Failed to write settings file");
            }
            catch (UnauthorizedAccessException ex)
            {
                LogHelper.WriteLogToFile(ex, "Settings Save | Access denied for settings file");
            }
            catch (ArgumentException ex)
            {
                LogHelper.WriteLogToFile(ex, "Settings Save | Invalid settings path");
            }
        }

        private string GetSettingsPath()
        {
            string settingsPath = settingsPathProvider();
            ArgumentException.ThrowIfNullOrWhiteSpace(settingsPath);
            return settingsPath;
        }

        private static Settings ReadSettings(string settingsPath)
        {
            string text = File.ReadAllText(settingsPath);
            return JsonConvert.DeserializeObject<Settings>(text) ?? CreateRecommendedSettings();
        }

        private static void EnsureParentDirectoryExists(string settingsPath)
        {
            string? directory = Path.GetDirectoryName(settingsPath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }

        private static Settings CreateRecommendedSettings() => SettingsDefaults.CreateRecommended();
    }
}
