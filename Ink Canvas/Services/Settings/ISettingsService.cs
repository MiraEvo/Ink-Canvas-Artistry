namespace Ink_Canvas.Services.Settings
{
    using SettingsModel = global::Ink_Canvas.Settings;

    public interface ISettingsService
    {
        SettingsModel Load();

        void Save(SettingsModel settings);
    }
}

