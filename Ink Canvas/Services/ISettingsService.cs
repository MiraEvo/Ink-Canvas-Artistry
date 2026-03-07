namespace Ink_Canvas.Services
{
    public interface ISettingsService
    {
        Settings Load();

        void Save(Settings settings);
    }
}
