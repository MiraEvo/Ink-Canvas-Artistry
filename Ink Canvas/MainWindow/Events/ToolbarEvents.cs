using System.Windows;
using System.Windows.Input;
using Point = System.Windows.Point;

namespace Ink_Canvas
{
    public partial class MainWindow
    {
        private void TwoFingerGestureBorder_Click(object sender, RoutedEventArgs e)
        {
            toolbarExperienceCoordinator.HandleToggleTwoFingerPanel();
        }

        private void SymbolIconEmoji_MouseMove(object sender, MouseEventArgs e)
        {
            toolbarExperienceCoordinator.HandleFloatingBarMouseMove(e.GetPosition(null));
        }

        private void SymbolIconEmoji_MouseDown(object sender, MouseButtonEventArgs e)
        {
            toolbarExperienceCoordinator.HandleFloatingBarMouseDown(e.GetPosition(null));
        }

        private void SymbolIconEmoji_MouseUp(object sender, MouseButtonEventArgs e)
        {
            Point? pointerPosition = e == null ? null : e.GetPosition(null);
            toolbarExperienceCoordinator.HandleFloatingBarMouseUp(pointerPosition);
        }

        private void SymbolIconUndo_Click(object sender, RoutedEventArgs e)
        {
            if (!Icon_Undo.IsEnabled)
            {
                return;
            }

            toolbarExperienceCoordinator.HandleUndoRequested();
        }

        private void SymbolIconRedo_Click(object sender, RoutedEventArgs e)
        {
            if (!Icon_Redo.IsEnabled)
            {
                return;
            }

            toolbarExperienceCoordinator.HandleRedoRequested();
        }

        private async void SymbolIconCursor_Click(object sender, RoutedEventArgs e)
        {
            await toolbarExperienceCoordinator.HandleCursorRequestedAsync();
        }

        private void SymbolIconDelete_MouseUp(object sender, RoutedEventArgs e)
        {
            toolbarExperienceCoordinator.HandleDeleteRequested();
        }

        private void SymbolIconSettings_Click(object sender, RoutedEventArgs e)
        {
            toolbarExperienceCoordinator.HandleOpenSettingsPanel();
        }

        private void SymbolIconSelect_Click(object sender, RoutedEventArgs e)
        {
            toolbarExperienceCoordinator.HandleSelectRequested();
        }

        private void ImageBlackboard_Click(object sender, RoutedEventArgs e)
        {
            toolbarExperienceCoordinator.HandleToggleBlackboardRequested();
        }

        private void SymbolIconTools_Click(object sender, RoutedEventArgs e)
        {
            toolbarExperienceCoordinator.HandleToggleToolsPanel();
        }

        private async void CursorIcon_Click(object sender, RoutedEventArgs e)
        {
            await toolbarExperienceCoordinator.HandleCursorRequestedAsync();
        }

        private void PenIcon_Click(object sender, RoutedEventArgs e)
        {
            toolbarExperienceCoordinator.HandlePenRequested();
        }

        private void ColorThemeSwitch_MouseUp(object sender, RoutedEventArgs e)
        {
            toolbarExperienceCoordinator.HandleToggleColorThemeRequested();
        }

        private void EraserIcon_Click(object sender, RoutedEventArgs e)
        {
            toolbarExperienceCoordinator.HandleEraserRequested();
        }

        private void EraserIconByStrokes_Click(object sender, RoutedEventArgs e)
        {
            toolbarExperienceCoordinator.HandleStrokeEraserRequested();
        }

        private async void CursorWithDelIcon_Click(object sender, RoutedEventArgs e)
        {
            toolbarExperienceCoordinator.HandleDeleteRequested();
            await toolbarExperienceCoordinator.HandleCursorRequestedAsync();
        }

        private void SelectIcon_MouseUp(object sender, RoutedEventArgs e)
        {
            toolbarExperienceCoordinator.HandleSelectRequested();
        }

        private void CloseBordertools_MouseUp(object sender, MouseButtonEventArgs e)
        {
            toolbarExperienceCoordinator.HandleCloseSubPanelsRequested();
        }

        private void BtnFingerDragMode_Click(object sender, RoutedEventArgs e)
        {
            toolbarExperienceCoordinator.HandleToggleSingleFingerDragRequested();
        }

        private void BtnUndo_Click(object sender, RoutedEventArgs e)
        {
            toolbarExperienceCoordinator.HandleUndoRequested();
        }

        private void BtnRedo_Click(object sender, RoutedEventArgs e)
        {
            toolbarExperienceCoordinator.HandleRedoRequested();
        }

        private void BtnSettings_Click(object sender, RoutedEventArgs e)
        {
            toolbarExperienceCoordinator.HandleToggleSettingsPanel();
        }

        private void BtnClear_Click(object sender, RoutedEventArgs e)
        {
            toolbarExperienceCoordinator.HandleClearRequested();
        }

        private void BtnSwitch_Click(object sender, RoutedEventArgs e)
        {
            toolbarExperienceCoordinator.HandleApplyCurrentWorkspaceVisualStateRequested();
        }

        private void BtnHideInkCanvas_Click(object sender, RoutedEventArgs e)
        {
            toolbarExperienceCoordinator.HandleHideInkCanvasRequested();
        }
    }
}
