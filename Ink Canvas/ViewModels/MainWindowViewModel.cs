using CommunityToolkit.Mvvm.ComponentModel;

namespace Ink_Canvas.ViewModels
{
    public sealed class MainWindowViewModel : ObservableObject
    {
        public MainWindowViewModel(SettingsViewModel settings, ShellViewModel shell)
        {
            Settings = settings;
            Shell = shell;
        }

        public SettingsViewModel Settings { get; }

        public ShellViewModel Shell { get; }
    }
}
