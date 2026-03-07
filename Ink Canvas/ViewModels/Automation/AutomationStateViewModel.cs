using CommunityToolkit.Mvvm.ComponentModel;

namespace Ink_Canvas.ViewModels.Automation
{
    public sealed class AutomationStateViewModel : ObservableObject
    {
        private bool isAutoFoldMonitoring;
        private bool isProcessKillMonitoring;
        private bool isSilentUpdateWaiting;
        private bool isFloatingBarFoldedByUser;
        private bool isFloatingBarUnfoldedByUser;
        private bool isFloatingBarFoldRequestedByAutomation;
        private bool isHidingSubPanelsWhenInking;
        private string pendingUpdateVersion = string.Empty;
        private string foregroundProcessName = string.Empty;
        private string foregroundWindowTitle = string.Empty;

        public bool IsAutoFoldMonitoring => isAutoFoldMonitoring;

        public bool IsProcessKillMonitoring => isProcessKillMonitoring;

        public bool IsSilentUpdateWaiting => isSilentUpdateWaiting;

        public bool IsFloatingBarFoldedByUser => isFloatingBarFoldedByUser;

        public bool IsFloatingBarUnfoldedByUser => isFloatingBarUnfoldedByUser;

        public bool IsFloatingBarFoldRequestedByAutomation => isFloatingBarFoldRequestedByAutomation;

        public bool IsHidingSubPanelsWhenInking => isHidingSubPanelsWhenInking;

        public string PendingUpdateVersion => pendingUpdateVersion;

        public string ForegroundProcessName => foregroundProcessName;

        public string ForegroundWindowTitle => foregroundWindowTitle;

        public bool SetAutoFoldMonitoring(bool value) => SetFlag(ref isAutoFoldMonitoring, value);

        public bool SetProcessKillMonitoring(bool value) => SetFlag(ref isProcessKillMonitoring, value);

        public bool SetSilentUpdateWaiting(bool value) => SetFlag(ref isSilentUpdateWaiting, value);

        public bool SetFloatingBarFoldedByUser(bool value) => SetFlag(ref isFloatingBarFoldedByUser, value);

        public bool SetFloatingBarUnfoldedByUser(bool value) => SetFlag(ref isFloatingBarUnfoldedByUser, value);

        public bool SetFloatingBarFoldRequestedByAutomation(bool value) => SetFlag(ref isFloatingBarFoldRequestedByAutomation, value);

        public bool SetHidingSubPanelsWhenInking(bool value) => SetFlag(ref isHidingSubPanelsWhenInking, value);

        public bool SetPendingUpdateVersion(string? value) => SetText(ref pendingUpdateVersion, value);

        public bool SetForegroundProcessName(string? value) => SetText(ref foregroundProcessName, value);

        public bool SetForegroundWindowTitle(string? value) => SetText(ref foregroundWindowTitle, value);

        private bool SetFlag(ref bool field, bool value)
        {
            return SetProperty(ref field, value);
        }

        private bool SetText(ref string field, string? value)
        {
            return SetProperty(ref field, value ?? string.Empty);
        }
    }
}

