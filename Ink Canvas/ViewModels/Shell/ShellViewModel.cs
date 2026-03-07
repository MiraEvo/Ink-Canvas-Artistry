using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;

namespace Ink_Canvas.ViewModels.Shell
{
    public sealed partial class ShellViewModel : ObservableObject
    {
        private WorkspaceMode workspaceMode = WorkspaceMode.DesktopAnnotation;
        private ToolMode toolMode = ToolMode.Cursor;
        private SubPanelKind activeSubPanel = SubPanelKind.None;
        private bool isFloatingBarFolded;
        private bool isFloatingBarTransitioning;
        private bool isBlackboardTransitioning;

        public ShellViewModel()
        {
            SetToolModeCommand = new RelayCommand<string?>(SetToolModeFromString);
        }

        public event Action<WorkspaceMode>? WorkspaceModeChanged;

        public event Action<ToolMode>? ToolModeChanged;

        public event Action<SubPanelKind>? ActiveSubPanelChanged;

        public event Action<bool>? FloatingBarFoldChanged;

        public IRelayCommand<string?> SetToolModeCommand { get; }

        public WorkspaceMode WorkspaceMode => workspaceMode;

        public ToolMode ToolMode => toolMode;

        public SubPanelKind ActiveSubPanel => activeSubPanel;

        public bool IsFloatingBarFolded => isFloatingBarFolded;

        public bool IsFloatingBarTransitioning => isFloatingBarTransitioning;

        public bool IsBlackboardTransitioning => isBlackboardTransitioning;

        public bool IsDesktopAnnotationMode => workspaceMode is WorkspaceMode.DesktopAnnotation;

        public bool IsBlackboardMode => workspaceMode is WorkspaceMode.Blackboard;

        public bool IsCursorMode => toolMode == ToolMode.Cursor;

        public bool IsPenMode => toolMode == ToolMode.Pen;

        public bool IsEraserMode => toolMode == ToolMode.Eraser;

        public bool IsEraserByStrokesMode => toolMode == ToolMode.EraserByStrokes;

        public bool IsSelectionMode => toolMode == ToolMode.Select;

        public bool IsShapeMode => toolMode == ToolMode.Shape;

        public bool IsCanvasControlsVisible => toolMode != ToolMode.Cursor;

        public bool IsToolsPanelOpen => activeSubPanel is SubPanelKind.Tools;

        public bool IsPenPaletteOpen => activeSubPanel is SubPanelKind.PenPalette;

        public bool IsSettingsPanelOpen => activeSubPanel is SubPanelKind.Settings;

        public bool IsTwoFingerPanelOpen => activeSubPanel is SubPanelKind.TwoFingerGesture;

        public bool IsShapePanelOpen => activeSubPanel is SubPanelKind.ShapePanel;

        public bool IsDeletePanelOpen => activeSubPanel is SubPanelKind.DeletePanel;

        public void ToggleWorkspaceMode()
        {
            if (isBlackboardTransitioning)
            {
                return;
            }

            SetBlackboardTransitioning(true);
            SetWorkspaceMode(IsBlackboardMode ? WorkspaceMode.DesktopAnnotation : WorkspaceMode.Blackboard);
        }

        public bool SetWorkspaceMode(WorkspaceMode mode, bool notify = true)
        {
            bool changed = SetProperty(ref workspaceMode, mode);
            if (changed)
            {
                OnPropertyChanged(nameof(IsDesktopAnnotationMode));
                OnPropertyChanged(nameof(IsBlackboardMode));
            }

            if (notify && changed)
            {
                WorkspaceModeChanged?.Invoke(mode);
            }

            return changed;
        }

        public bool SetToolMode(ToolMode mode, bool notify = true, bool force = false)
        {
            bool changed = SetProperty(ref toolMode, mode);
            if (changed)
            {
                OnPropertyChanged(nameof(IsCursorMode));
                OnPropertyChanged(nameof(IsPenMode));
                OnPropertyChanged(nameof(IsEraserMode));
                OnPropertyChanged(nameof(IsEraserByStrokesMode));
                OnPropertyChanged(nameof(IsSelectionMode));
                OnPropertyChanged(nameof(IsShapeMode));
                OnPropertyChanged(nameof(IsCanvasControlsVisible));
            }

            if (notify && (changed || force))
            {
                ToolModeChanged?.Invoke(mode);
            }

            return changed;
        }

        public bool SetActiveSubPanel(SubPanelKind panel, bool notify = true, bool force = false)
        {
            bool changed = SetProperty(ref activeSubPanel, panel);
            if (changed)
            {
                OnPropertyChanged(nameof(IsToolsPanelOpen));
                OnPropertyChanged(nameof(IsPenPaletteOpen));
                OnPropertyChanged(nameof(IsSettingsPanelOpen));
                OnPropertyChanged(nameof(IsTwoFingerPanelOpen));
                OnPropertyChanged(nameof(IsShapePanelOpen));
                OnPropertyChanged(nameof(IsDeletePanelOpen));
            }

            if (notify && (changed || force))
            {
                ActiveSubPanelChanged?.Invoke(panel);
            }

            return changed;
        }

        public void ToggleSubPanel(SubPanelKind panel)
        {
            SetActiveSubPanel(activeSubPanel == panel ? SubPanelKind.None : panel);
        }

        public bool SetFloatingBarFolded(bool value, bool notify = true)
        {
            bool changed = SetProperty(ref isFloatingBarFolded, value);
            if (notify && changed)
            {
                FloatingBarFoldChanged?.Invoke(value);
            }

            return changed;
        }

        public void RequestFloatingBarFold(bool isFolded)
        {
            if (isFloatingBarTransitioning || isFloatingBarFolded == isFolded)
            {
                return;
            }

            SetFloatingBarTransitioning(true);
            SetFloatingBarFolded(isFolded);
        }

        public void SetFloatingBarTransitioning(bool value)
        {
            SetProperty(ref isFloatingBarTransitioning, value);
        }

        public void SetBlackboardTransitioning(bool value)
        {
            SetProperty(ref isBlackboardTransitioning, value);
        }

        private void SetToolModeFromString(string? value)
        {
            if (TryParseToolMode(value, out ToolMode mode))
            {
                SetToolMode(mode, true, true);
            }
        }

        [RelayCommand]
        private void ToggleBlackboardMode()
        {
            ToggleWorkspaceMode();
        }

        [RelayCommand]
        private void ToggleToolsPanel()
        {
            ToggleSubPanel(SubPanelKind.Tools);
        }

        [RelayCommand]
        private void TogglePenPalette()
        {
            ToggleSubPanel(SubPanelKind.PenPalette);
        }

        [RelayCommand]
        private void ToggleSettingsPanel()
        {
            ToggleSubPanel(SubPanelKind.Settings);
        }

        [RelayCommand]
        private void OpenSettingsPanel()
        {
            SetActiveSubPanel(SubPanelKind.Settings);
        }

        [RelayCommand]
        private void ToggleTwoFingerPanel()
        {
            ToggleSubPanel(SubPanelKind.TwoFingerGesture);
        }

        [RelayCommand]
        private void ToggleShapePanel()
        {
            ToggleSubPanel(SubPanelKind.ShapePanel);
        }

        [RelayCommand]
        private void ToggleDeletePanel()
        {
            ToggleSubPanel(SubPanelKind.DeletePanel);
        }

        [RelayCommand]
        private void HideAllSubPanels()
        {
            SetActiveSubPanel(SubPanelKind.None);
        }

        [RelayCommand]
        private void FoldFloatingBar()
        {
            RequestFloatingBarFold(true);
        }

        [RelayCommand]
        private void UnfoldFloatingBar()
        {
            RequestFloatingBarFold(false);
        }

        private static bool TryParseToolMode(string? value, out ToolMode mode)
        {
            return Enum.TryParse(value, true, out mode);
        }
    }
}

