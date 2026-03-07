using CommunityToolkit.Mvvm.ComponentModel;

namespace Ink_Canvas.ViewModels
{
    public sealed class MainWindowViewModel : ObservableObject
    {
        public MainWindowViewModel(SettingsViewModel settings, ShellViewModel shell, InputStateViewModel input)
        {
            Settings = settings;
            Shell = shell;
            Input = input;
        }

        public SettingsViewModel Settings { get; }

        public ShellViewModel Shell { get; }

        public InputStateViewModel Input { get; }
    }
}
