using CommunityToolkit.Mvvm.ComponentModel;

namespace Ink_Canvas.ViewModels
{
    public sealed class AutomationStateViewModel : ObservableObject
    {
        private bool isFloatingBarFoldedByUser;
        private bool isFloatingBarUnfoldedByUser;
        private bool isHidingSubPanelsWhenInking;

        public bool IsFloatingBarFoldedByUser => isFloatingBarFoldedByUser;

        public bool IsFloatingBarUnfoldedByUser => isFloatingBarUnfoldedByUser;

        public bool IsHidingSubPanelsWhenInking => isHidingSubPanelsWhenInking;

        public bool SetFloatingBarFoldedByUser(bool value)
        {
            return SetProperty(ref isFloatingBarFoldedByUser, value);
        }

        public bool SetFloatingBarUnfoldedByUser(bool value)
        {
            return SetProperty(ref isFloatingBarUnfoldedByUser, value);
        }

        public bool SetHidingSubPanelsWhenInking(bool value)
        {
            return SetProperty(ref isHidingSubPanelsWhenInking, value);
        }
    }
}
