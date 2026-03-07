using System;
using Ink_Canvas.ViewModels;

namespace Ink_Canvas.Features.Shell.Coordinators
{
    internal sealed class ShellExperienceCoordinator(
        IShellUiHost host,
        ShellViewModel shellViewModel,
        SettingsViewModel settingsViewModel,
        WorkspaceSessionViewModel workspaceSessionViewModel,
        InputStateViewModel inputStateViewModel)
    {
        public void HandleWorkspaceModeChanged(WorkspaceMode workspaceMode)
        {
            if (host.IsSelectionEditingMode)
            {
                shellViewModel.SetToolMode(ToolMode.Pen, true, true);
            }

            if (workspaceMode == WorkspaceMode.Blackboard)
            {
                host.HidePresentationNavigation();
                _ = host.AnimateFloatingBarMarginAfterDelayAsync(TimeSpan.FromMilliseconds(100));

                if (!host.IsPenToolActive)
                {
                    shellViewModel.SetToolMode(ToolMode.Pen, true, true);
                }

                if (settingsViewModel.AutoSwitchTwoFingerGesture)
                {
                    settingsViewModel.SetIsEnableTwoFingerTranslate(true, false);
                    settingsViewModel.SetIsEnableMultiTouchMode(false, false);
                }
            }
            else
            {
                host.HideSubPanelsImmediately();
                host.ShowPresentationNavigationIfEnabled();

                if (settingsViewModel.IsAutoSaveStrokesAtClear
                    && host.CurrentInkStrokeCount > settingsViewModel.MinimumAutomationStrokeNumber)
                {
                    host.SaveScreenshotForCurrentContext();
                }

                host.AnimateFloatingBarMargin();

                if (!host.IsPenToolActive)
                {
                    shellViewModel.SetToolMode(ToolMode.Pen, true, true);
                }

                if (settingsViewModel.AutoSwitchTwoFingerGesture)
                {
                    settingsViewModel.SetIsEnableTwoFingerTranslate(false, false);
                    settingsViewModel.SetIsEnableMultiTouchMode(true, false);
                }
            }

            host.ApplyWorkspaceVisualState(workspaceMode);

            if (workspaceMode == WorkspaceMode.DesktopAnnotation
                && host.CurrentInkStrokeCount == 0
                && !host.IsPresentationSlideShowRunning)
            {
                shellViewModel.SetToolMode(ToolMode.Cursor, true, true);
            }

            host.ApplyShellThemeRefresh();
            host.CompleteBlackboardTransition();
        }

        public void HandleToolModeChanged(ToolMode mode)
        {
            switch (mode)
            {
                case ToolMode.Cursor:
                    inputStateViewModel.SetActiveShapeTool(ShapeToolKind.None, false);
                    inputStateViewModel.SetForceEraser(false, false);
                    host.ApplyCursorToolModeVisuals();
                    break;
                case ToolMode.Pen:
                    host.ApplyPenToolModeVisuals();
                    break;
                case ToolMode.Eraser:
                    host.ApplyPointEraserToolModeVisuals(GetPointEraserDiameter());
                    break;
                case ToolMode.EraserByStrokes:
                    host.ApplyStrokeEraserToolModeVisuals();
                    break;
                case ToolMode.Select:
                    host.ApplySelectionToolModeVisuals();
                    break;
            }
        }

        public void HandleSubPanelChanged(SubPanelKind panel)
        {
            host.ApplySubPanelState(panel);
        }

        public void ApplyPendingWorkspaceDesktopDefaults()
        {
            if (!workspaceSessionViewModel.IsDesktopSession)
            {
                return;
            }

            if (workspaceSessionViewModel.ConsumeRestoreDefaultFloatingBarPosition())
            {
                host.RequestDefaultDesktopFloatingBarPosition();
            }

            if (workspaceSessionViewModel.ConsumeRestoreDefaultToolOnDesktopResume())
            {
                shellViewModel.SetToolMode(ToolMode.Cursor, true, true);
            }
        }

        private double GetPointEraserDiameter()
        {
            double scale = settingsViewModel.EraserSize switch
            {
                0 => 0.5,
                1 => 0.8,
                3 => 1.25,
                4 => 1.8,
                _ => 1
            };

            return scale * 90;
        }
    }
}

