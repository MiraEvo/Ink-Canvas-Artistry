using CommunityToolkit.Mvvm.ComponentModel;

namespace Ink_Canvas.ViewModels.Main
{
    public sealed class MainWindowViewModel(
        SettingsViewModel settings,
        ShellViewModel shell,
        InputStateViewModel input,
        PresentationSessionViewModel presentation,
        AutomationStateViewModel automation,
        WorkspaceSessionViewModel workspaceSession,
        PaletteViewModel palette) : ObservableObject
    {
        public SettingsViewModel Settings { get; } = settings;

        public PaletteViewModel Palette { get; } = palette;

        public ShellViewModel Shell { get; } = shell;

        public InputStateViewModel Input { get; } = input;

        public PresentationSessionViewModel Presentation { get; } = presentation;

        public AutomationStateViewModel Automation { get; } = automation;

        public AutomationStateViewModel AutomationState => Automation;

        public WorkspaceSessionViewModel WorkspaceSession { get; } = workspaceSession;
    }
}

