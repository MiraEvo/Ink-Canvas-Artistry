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
            AutomationStateViewModel automation,
            WorkspaceSessionViewModel workspaceSession)
        {
            Settings = settings;
            Shell = shell;
            Input = input;
            Presentation = presentation;
            Automation = automation;
            WorkspaceSession = workspaceSession;
        }

        public SettingsViewModel Settings { get; }

        public ShellViewModel Shell { get; }

        public InputStateViewModel Input { get; }

        public PresentationSessionViewModel Presentation { get; }

        public AutomationStateViewModel Automation { get; }

        public AutomationStateViewModel AutomationState => Automation;

        public WorkspaceSessionViewModel WorkspaceSession { get; }
    }
}
