using Ink_Canvas.Features.Ink;
using System;
using System.Windows;

namespace Ink_Canvas
{
    public partial class MainWindow : Window
    {
        private void SaveStrokes(bool isBackupMain = false)
        {
            inkHistoryCoordinator?.SaveCurrentPageHistory(isBackupMain);
        }

        private void ClearStrokes(bool isErasedByCode)
        {
            inkHistoryCoordinator?.ClearCanvas(isErasedByCode);
        }

        private void RestoreStrokes(bool isBackupMain = false)
        {
            inkHistoryCoordinator?.RestoreCurrentPageHistory(isBackupMain);
        }

        private void BtnWhiteBoardSwitchPrevious_Click(object sender, EventArgs e)
        {
            if (!inkHistoryCoordinator.MoveToPreviousWhiteboardPage())
            {
                return;
            }
        }

        private void BtnWhiteBoardSwitchNext_Click(object sender, EventArgs e)
        {
            if (Settings.Automation.IsAutoSaveStrokesAtClear && inkCanvas.Strokes.Count > Settings.Automation.MinimumAutomationStrokeNumber)
            {
                SaveScreenshot(true);
            }

            if (CurrentWhiteboardIndex >= WhiteboardTotalCount)
            {
                BtnWhiteBoardAdd_Click(sender, e);
                return;
            }

            inkHistoryCoordinator?.MoveToNextWhiteboardPage();
        }

        private void BtnWhiteBoardAdd_Click(object sender, EventArgs e)
        {
            if (WhiteboardTotalCount >= 99)
            {
                return;
            }

            if (Settings.Automation.IsAutoSaveStrokesAtClear && inkCanvas.Strokes.Count > Settings.Automation.MinimumAutomationStrokeNumber)
            {
                SaveScreenshot(true);
            }

            inkHistoryCoordinator?.AddWhiteboardPage();
        }

        private void BtnWhiteBoardDelete_Click(object sender, RoutedEventArgs e)
        {
            inkHistoryCoordinator?.DeleteWhiteboardPage();
        }

        private void UpdateIndexInfoDisplay()
        {
            ((IInkHistoryHost)this).UpdateWhiteboardIndexDisplay(CurrentWhiteboardIndex, WhiteboardTotalCount);
        }
    }
}
