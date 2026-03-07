using Ink_Canvas.Services.Logging;
using Newtonsoft.Json;
using System;
using System.IO;
using SettingsModel = global::Ink_Canvas.Settings;

namespace Ink_Canvas.Services.Settings
{
    public sealed class JsonSettingsService : ISettingsService
    {
        private readonly Func<string> settingsPathProvider;
        private readonly IAppLogger logger;

        public JsonSettingsService(Func<string> settingsPathProvider, IAppLogger logger)
        {
            this.settingsPathProvider = settingsPathProvider ?? throw new ArgumentNullException(nameof(settingsPathProvider));
            this.logger = (logger ?? throw new ArgumentNullException(nameof(logger))).ForCategory(nameof(JsonSettingsService));
        }

        public SettingsModel Load()
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
                logger.Error(ex, "Settings Load | Failed to read settings file");
                return CreateRecommendedSettings();
            }
            catch (UnauthorizedAccessException ex)
            {
                logger.Error(ex, "Settings Load | Access denied for settings file");
                return CreateRecommendedSettings();
            }
            catch (JsonException ex)
            {
                logger.Error(ex, "Settings Load | Invalid JSON in settings file");
                return CreateRecommendedSettings();
            }
            catch (ArgumentException ex)
            {
                logger.Error(ex, "Settings Load | Invalid settings path");
                return CreateRecommendedSettings();
            }
        }

        public void Save(SettingsModel settings)
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
                logger.Error(ex, "Settings Save | Failed to write settings file");
            }
            catch (UnauthorizedAccessException ex)
            {
                logger.Error(ex, "Settings Save | Access denied for settings file");
            }
            catch (ArgumentException ex)
            {
                logger.Error(ex, "Settings Save | Invalid settings path");
            }
        }

        private string GetSettingsPath()
        {
            string settingsPath = settingsPathProvider();
            ArgumentException.ThrowIfNullOrWhiteSpace(settingsPath);
            return settingsPath;
        }

        private static SettingsModel ReadSettings(string settingsPath)
        {
            string text = File.ReadAllText(settingsPath);
            return JsonConvert.DeserializeObject<SettingsModel>(text) ?? CreateRecommendedSettings();
        }

        private static void EnsureParentDirectoryExists(string settingsPath)
        {
            string? directory = Path.GetDirectoryName(settingsPath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }

        private static SettingsModel CreateRecommendedSettings() => SettingsDefaults.CreateRecommended();
    }
}
