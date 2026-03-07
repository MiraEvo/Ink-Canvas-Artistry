using Ink_Canvas.ViewModels;
using System;
using System.Threading.Tasks;
using System.Windows;

namespace Ink_Canvas.Features.Shell
{
    internal sealed class ToolbarExperienceCoordinator(
        IToolbarUiHost host,
        ShellViewModel shellViewModel)
    {
        public void HandleToggleTwoFingerPanel()
        {
            shellViewModel.ToggleTwoFingerPanelCommand.Execute(null);
        }

        public void HandleToggleToolsPanel()
        {
            shellViewModel.ToggleToolsPanelCommand.Execute(null);
        }

        public void HandleToggleSettingsPanel()
        {
            shellViewModel.ToggleSettingsPanelCommand.Execute(null);
        }

        public void HandleOpenSettingsPanel()
        {
            shellViewModel.OpenSettingsPanelCommand.Execute(null);
        }

        public void HandleFloatingBarMouseMove(Point pointerPosition)
        {
            host.UpdateFloatingBarDrag(pointerPosition);
        }

        public void HandleFloatingBarMouseDown(Point pointerPosition)
        {
            host.BeginFloatingBarDrag(pointerPosition);
        }

        public void HandleFloatingBarMouseUp(Point? pointerPosition)
        {
            host.EndFloatingBarDrag(pointerPosition);
        }

        public async Task HandleFoldFloatingBarAsync(bool userInitiated)
        {
            await host.FoldFloatingBarAsync(userInitiated);
        }

        public async Task HandleUnfoldFloatingBarAsync(bool userInitiated)
        {
            await host.UnfoldFloatingBarAsync(userInitiated);
        }

        public void HandleUndoRequested()
        {
            host.UndoHistory();
            host.HideSubPanels();
        }

        public void HandleRedoRequested()
        {
            host.RedoHistory();
            host.HideSubPanels();
        }

        public async Task HandleCursorRequestedAsync()
        {
            if (shellViewModel.IsBlackboardMode)
            {
                host.ExitBlackboardSession();
                return;
            }

            shellViewModel.SetToolMode(ToolMode.Cursor, true, true);

            if (host.IsPresentationSlideShowRunning)
            {
                await host.AnimateFloatingBarMarginAfterDelayAsync(TimeSpan.FromMilliseconds(100));
            }
        }

        public void HandlePenRequested()
        {
            if (shellViewModel.IsPenMode && host.IsCanvasControlsVisible)
            {
                shellViewModel.TogglePenPaletteCommand.Execute(null);
                return;
            }

            shellViewModel.SetToolMode(ToolMode.Pen, true, true);
        }

        public void HandleEraserRequested()
        {
            shellViewModel.SetToolMode(ToolMode.Eraser, true, true);
        }

        public void HandleStrokeEraserRequested()
        {
            shellViewModel.SetToolMode(ToolMode.EraserByStrokes, true, true);
        }

        public void HandleSelectRequested()
        {
            shellViewModel.SetToolMode(ToolMode.Select, true, true);
        }

        public void HandleDeleteRequested()
        {
            host.DeleteSelectionOrClear();
        }

        public void HandleClearRequested()
        {
            host.ClearCanvas();
        }

        public void HandleToggleBlackboardRequested()
        {
            host.ToggleBlackboardSession();
        }

        public void HandleHideInkCanvasRequested()
        {
            host.ToggleInkCanvasVisibility();
        }

        public void HandleToggleColorThemeRequested()
        {
            host.ToggleColorTheme();
        }

        public void HandleToggleSingleFingerDragRequested()
        {
            host.ToggleSingleFingerDragMode();
        }

        public void HandleCloseSubPanelsRequested()
        {
            host.HideSubPanels();
        }

        public void HandleApplyCurrentWorkspaceVisualStateRequested()
        {
            host.ApplyCurrentWorkspaceVisualState();
        }
    }
}
