using Ink_Canvas.Helpers;
using Newtonsoft.Json;
using System;
using System.IO;

namespace Ink_Canvas.Services
{
    public sealed class JsonSettingsService : ISettingsService
    {
        private readonly Func<string> settingsPathProvider;

        public JsonSettingsService(Func<string> settingsPathProvider)
        {
            this.settingsPathProvider = settingsPathProvider;
        }

        public Settings Load()
        {
            string settingsPath = settingsPathProvider();
            try
            {
                if (!File.Exists(settingsPath))
                {
                    return SettingsDefaults.CreateRecommended();
                }

                string text = File.ReadAllText(settingsPath);
                Settings settings = JsonConvert.DeserializeObject<Settings>(text);
                return SettingsDefaults.Normalize(settings);
            }
            catch (IOException ex)
            {
                LogHelper.WriteLogToFile(ex, $"Settings Load | Failed to read '{settingsPath}'");
                return SettingsDefaults.CreateRecommended();
            }
            catch (UnauthorizedAccessException ex)
            {
                LogHelper.WriteLogToFile(ex, $"Settings Load | Access denied for '{settingsPath}'");
                return SettingsDefaults.CreateRecommended();
            }
            catch (JsonException ex)
            {
                LogHelper.WriteLogToFile(ex, $"Settings Load | Invalid JSON in '{settingsPath}'");
                return SettingsDefaults.CreateRecommended();
            }
        }

        public void Save(Settings settings)
        {
            string text = JsonConvert.SerializeObject(SettingsDefaults.Normalize(settings), Formatting.Indented);
            string settingsPath = settingsPathProvider();

            try
            {
                string directory = Path.GetDirectoryName(settingsPath);
                if (!string.IsNullOrWhiteSpace(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllText(settingsPath, text);
            }
            catch (IOException ex)
            {
                LogHelper.WriteLogToFile(ex, $"Settings Save | Failed to write '{settingsPath}'");
            }
            catch (UnauthorizedAccessException ex)
            {
                LogHelper.WriteLogToFile(ex, $"Settings Save | Access denied for '{settingsPath}'");
            }
        }
    }
}
