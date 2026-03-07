using CommunityToolkit.Mvvm.ComponentModel;

namespace Ink_Canvas.ViewModels
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

        public bool SetAutoFoldMonitoring(bool value)
        {
            return SetProperty(ref isAutoFoldMonitoring, value);
        }

        public bool SetProcessKillMonitoring(bool value)
        {
            return SetProperty(ref isProcessKillMonitoring, value);
        }

        public bool SetSilentUpdateWaiting(bool value)
        {
            return SetProperty(ref isSilentUpdateWaiting, value);
        }

        public bool SetFloatingBarFoldedByUser(bool value)
        {
            return SetProperty(ref isFloatingBarFoldedByUser, value);
        }

        public bool SetFloatingBarUnfoldedByUser(bool value)
        {
            return SetProperty(ref isFloatingBarUnfoldedByUser, value);
        }

        public bool SetFloatingBarFoldRequestedByAutomation(bool value)
        {
            return SetProperty(ref isFloatingBarFoldRequestedByAutomation, value);
        }

        public bool SetHidingSubPanelsWhenInking(bool value)
        {
            return SetProperty(ref isHidingSubPanelsWhenInking, value);
        }

        public bool SetPendingUpdateVersion(string value)
        {
            return SetProperty(ref pendingUpdateVersion, value ?? string.Empty);
        }

        public bool SetForegroundProcessName(string value)
        {
            return SetProperty(ref foregroundProcessName, value ?? string.Empty);
        }

        public bool SetForegroundWindowTitle(string value)
        {
            return SetProperty(ref foregroundWindowTitle, value ?? string.Empty);
        }
    }
}
