using CommunityToolkit.Mvvm.ComponentModel;

namespace Ink_Canvas.ViewModels
{
    public sealed class MainWindowViewModel : ObservableObject
    {
        public MainWindowViewModel(
            SettingsViewModel settings,
            ShellViewModel shell,
            InputStateViewModel input,
            PresentationSessionViewModel presentation,
            AutomationStateViewModel automationState,
            WorkspaceSessionViewModel workspaceSession)
        {
            Settings = settings;
            Shell = shell;
            Input = input;
            Presentation = presentation;
            AutomationState = automationState;
            WorkspaceSession = workspaceSession;
        }

        public SettingsViewModel Settings { get; }

        public ShellViewModel Shell { get; }

        public InputStateViewModel Input { get; }

        public PresentationSessionViewModel Presentation { get; }

        public AutomationStateViewModel AutomationState { get; }

        public WorkspaceSessionViewModel WorkspaceSession { get; }
    }
}
