using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;

namespace Ink_Canvas.ViewModels
{
    public sealed class ShellViewModel : ObservableObject
    {
        private WorkspaceMode workspaceMode = WorkspaceMode.DesktopAnnotation;
        private ToolMode toolMode = ToolMode.Cursor;
        private SubPanelKind activeSubPanel = SubPanelKind.None;
        private bool isFloatingBarFolded;
        private bool isFloatingBarTransitioning;
        private bool isBlackboardTransitioning;

        public ShellViewModel()
        {
            ToggleBlackboardModeCommand = new RelayCommand(ToggleWorkspaceMode);
            SetToolModeCommand = new RelayCommand<string>(value =>
            {
                if (TryParseToolMode(value, out ToolMode mode))
                {
                    SetToolMode(mode, true, true);
                }
            });
            ToggleToolsPanelCommand = new RelayCommand(() => ToggleSubPanel(SubPanelKind.Tools));
            TogglePenPaletteCommand = new RelayCommand(() => ToggleSubPanel(SubPanelKind.PenPalette));
            ToggleSettingsPanelCommand = new RelayCommand(() => ToggleSubPanel(SubPanelKind.Settings));
            OpenSettingsPanelCommand = new RelayCommand(() => SetActiveSubPanel(SubPanelKind.Settings));
            ToggleTwoFingerPanelCommand = new RelayCommand(() => ToggleSubPanel(SubPanelKind.TwoFingerGesture));
            ToggleShapePanelCommand = new RelayCommand(() => ToggleSubPanel(SubPanelKind.ShapePanel));
            ToggleDeletePanelCommand = new RelayCommand(() => ToggleSubPanel(SubPanelKind.DeletePanel));
            HideAllSubPanelsCommand = new RelayCommand(() => SetActiveSubPanel(SubPanelKind.None));
            FoldFloatingBarCommand = new RelayCommand(() => RequestFloatingBarFold(true));
            UnfoldFloatingBarCommand = new RelayCommand(() => RequestFloatingBarFold(false));
        }

        public event Action<WorkspaceMode> WorkspaceModeChanged;

        public event Action<ToolMode> ToolModeChanged;

        public event Action<SubPanelKind> ActiveSubPanelChanged;

        public event Action<bool> FloatingBarFoldChanged;

        public IRelayCommand ToggleBlackboardModeCommand { get; }

        public IRelayCommand<string> SetToolModeCommand { get; }

        public IRelayCommand ToggleToolsPanelCommand { get; }

        public IRelayCommand TogglePenPaletteCommand { get; }

        public IRelayCommand ToggleSettingsPanelCommand { get; }

        public IRelayCommand OpenSettingsPanelCommand { get; }

        public IRelayCommand ToggleTwoFingerPanelCommand { get; }

        public IRelayCommand ToggleShapePanelCommand { get; }

        public IRelayCommand ToggleDeletePanelCommand { get; }

        public IRelayCommand HideAllSubPanelsCommand { get; }

        public IRelayCommand FoldFloatingBarCommand { get; }

        public IRelayCommand UnfoldFloatingBarCommand { get; }

        public WorkspaceMode WorkspaceMode => workspaceMode;

        public ToolMode ToolMode => toolMode;

        public SubPanelKind ActiveSubPanel => activeSubPanel;

        public bool IsFloatingBarFolded => isFloatingBarFolded;

        public bool IsFloatingBarTransitioning => isFloatingBarTransitioning;

        public bool IsBlackboardTransitioning => isBlackboardTransitioning;

        public bool IsDesktopAnnotationMode => workspaceMode == WorkspaceMode.DesktopAnnotation;

        public bool IsBlackboardMode => workspaceMode == WorkspaceMode.Blackboard;

        public bool IsCursorMode => toolMode == ToolMode.Cursor;

        public bool IsPenMode => toolMode == ToolMode.Pen;

        public bool IsEraserMode => toolMode == ToolMode.Eraser;

        public bool IsEraserByStrokesMode => toolMode == ToolMode.EraserByStrokes;

        public bool IsSelectionMode => toolMode == ToolMode.Select;

        public bool IsShapeMode => toolMode == ToolMode.Shape;

        public bool IsCanvasControlsVisible => toolMode != ToolMode.Cursor;

        public bool IsToolsPanelOpen => activeSubPanel == SubPanelKind.Tools;

        public bool IsPenPaletteOpen => activeSubPanel == SubPanelKind.PenPalette;

        public bool IsSettingsPanelOpen => activeSubPanel == SubPanelKind.Settings;

        public bool IsTwoFingerPanelOpen => activeSubPanel == SubPanelKind.TwoFingerGesture;

        public bool IsShapePanelOpen => activeSubPanel == SubPanelKind.ShapePanel;

        public bool IsDeletePanelOpen => activeSubPanel == SubPanelKind.DeletePanel;

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

        private static bool TryParseToolMode(string value, out ToolMode mode)
        {
            return Enum.TryParse(value, true, out mode);
        }
    }
}
