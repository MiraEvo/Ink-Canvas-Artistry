using System.Windows;
using System.Windows.Input;

namespace Ink_Canvas
{
    public partial class MainWindow : Window
    {
        private void RegisterGlobalHotkeys()
        {
            Hotkey.Regist(this, HotkeyModifiers.MOD_SHIFT, Key.Escape, HotKey_ExitPPTSlideShow);
            Hotkey.Regist(this, HotkeyModifiers.MOD_CONTROL, Key.E, HotKey_Clear);
            Hotkey.Regist(this, HotkeyModifiers.MOD_ALT, Key.C, HotKey_Capture);
            Hotkey.Regist(this, HotkeyModifiers.MOD_ALT, Key.V, HotKey_Hide);
            Hotkey.Regist(this, HotkeyModifiers.MOD_ALT, Key.D, HotKey_DrawTool);
            Hotkey.Regist(this, HotkeyModifiers.MOD_ALT, Key.Q, HotKey_QuitDrawMode);
            Hotkey.Regist(this, HotkeyModifiers.MOD_ALT, Key.B, HotKey_Board);
        }

        private void HotKey_ExitPPTSlideShow()
        {
            if (PresentationViewModel.CanNavigateSlides)
            {
                hotkeyController?.ExitPresentation();
            }
        }

        private void HotKey_Clear()
        {
            hotkeyController?.ClearCanvas();
        }

        private void HotKey_Capture()
        {
            hotkeyController?.CaptureScreen();
        }
        
        private void HotKey_Hide()
        {
            hotkeyController?.ToggleCanvasVisibility();
        }

        private void HotKey_DrawTool()
        {
            hotkeyController?.ActivatePen();
        }

        private void HotKey_QuitDrawMode()
        {
            hotkeyController?.ExitDrawMode();
        }

        private void HotKey_Board()
        {
            hotkeyController?.ToggleBlackboard();
        }

        private void Window_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (!PresentationViewModel.CanNavigateSlides || !WorkspaceSessionViewModel.IsDesktopSession) return;
            if (e.Delta >= 120)
            {
                BtnPPTSlidesUp_Click(null, null);
            }
            else if (e.Delta <= -120)
            {
                BtnPPTSlidesDown_Click(null, null);
            }
        }

        private void Main_Grid_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (!PresentationViewModel.CanNavigateSlides || !WorkspaceSessionViewModel.IsDesktopSession) return;

            if (e.Key == Key.Down || e.Key == Key.PageDown || e.Key == Key.Right || e.Key == Key.N || e.Key == Key.Space)
            {
                BtnPPTSlidesDown_Click(null, null);
            }
            if (e.Key == Key.Up || e.Key == Key.PageUp || e.Key == Key.Left || e.Key == Key.P)
            {
                BtnPPTSlidesUp_Click(null, null);
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                KeyExit(null, null);
            }
        }

        private void CommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void HotKey_Undo(object sender, ExecutedRoutedEventArgs e)
        {
            SymbolIconUndo_Click(null, null);
        }

        private void HotKey_Redo(object sender, ExecutedRoutedEventArgs e)
        {
            SymbolIconRedo_Click(null, null);
        }

        private void KeyExit(object sender, ExecutedRoutedEventArgs e)
        {
            BtnPPTSlideShowEnd_Click(null, null);
        }

        private void KeyChangeToSelect(object sender, ExecutedRoutedEventArgs e)
        {
            if (StackPanelCanvasControls.Visibility == Visibility.Visible)
            {
                SymbolIconSelect_Click(null, null);
            }
        }

        private void KeyChangeToEraser(object sender, ExecutedRoutedEventArgs e)
        {
            if (StackPanelCanvasControls.Visibility == Visibility.Visible)
            {
                if (Eraser_Icon.Background != null)
                {
                    EraserIconByStrokes_Click(null, null);
                }
                else
                {
                    EraserIcon_Click(null, null);
                }
            }
        }

        private void KeyDrawLine(object sender, ExecutedRoutedEventArgs e)
        {
            if (StackPanelCanvasControls.Visibility == Visibility.Visible)
            {
                BtnDrawLine_Click(lastMouseDownSender, null);
            }
        }
    }
}
