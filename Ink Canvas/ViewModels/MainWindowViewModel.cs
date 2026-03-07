using CommunityToolkit.Mvvm.ComponentModel;

namespace Ink_Canvas.ViewModels
{
    public sealed class MainWindowViewModel : ObservableObject
    {
        public MainWindowViewModel(SettingsViewModel settings)
        {
            Settings = settings;
        }

        public SettingsViewModel Settings { get; }
    }
}
