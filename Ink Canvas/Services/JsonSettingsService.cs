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
            try
            {
                string settingsPath = settingsPathProvider();
                if (!File.Exists(settingsPath))
                {
                    return SettingsDefaults.CreateRecommended();
                }

                string text = File.ReadAllText(settingsPath);
                Settings settings = JsonConvert.DeserializeObject<Settings>(text);
                return SettingsDefaults.Normalize(settings);
            }
            catch
            {
                return SettingsDefaults.CreateRecommended();
            }
        }

        public void Save(Settings settings)
        {
            string text = JsonConvert.SerializeObject(SettingsDefaults.Normalize(settings), Formatting.Indented);

            try
            {
                string settingsPath = settingsPathProvider();
                string directory = Path.GetDirectoryName(settingsPath);
                if (!string.IsNullOrWhiteSpace(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllText(settingsPath, text);
            }
            catch
            {
            }
        }
    }
}
