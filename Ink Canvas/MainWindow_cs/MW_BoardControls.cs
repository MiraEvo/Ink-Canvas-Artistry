using Ink_Canvas.Helpers;
using System;
using System.Windows;
using System.Windows.Ink;

namespace Ink_Canvas
{
    public partial class MainWindow : Window
    {
        private readonly StrokeCollection[] strokeCollections = new StrokeCollection[101];
        StrokeCollection lastTouchDownStrokeCollection = new StrokeCollection();

        int CurrentWhiteboardIndex = 1, WhiteboardTotalCount = 1;
        private readonly TimeMachineHistory[][] TimeMachineHistories = new TimeMachineHistory[101][]; //最多99页，0用来存储非白板时的墨迹以便还原

        private void SaveStrokes(bool isBackupMain = false)
        {
            if (isBackupMain)
            {
                var timeMachineHistory = timeMachine.ExportTimeMachineHistory();
                TimeMachineHistories[0] = timeMachineHistory;
                timeMachine.ClearStrokeHistory();

            }
            else
            {
                var timeMachineHistory = timeMachine.ExportTimeMachineHistory();
                TimeMachineHistories[CurrentWhiteboardIndex] = timeMachineHistory;
                timeMachine.ClearStrokeHistory();
            }
        }

        private void ClearStrokes(bool isErasedByCode)
        {
            _currentCommitType = CommitReason.ClearingCanvas;
            if (isErasedByCode) _currentCommitType = CommitReason.CodeInput;
            inkCanvas.Strokes.Clear();
            inkCanvas.Children.Clear();
            _currentCommitType = CommitReason.UserInput;
        }

        private void RestoreStrokes(bool isBackupMain = false)
        {
            TimeMachineHistory[] history = isBackupMain
                ? TimeMachineHistories[0]
                : TimeMachineHistories[CurrentWhiteboardIndex];
            if (history == null)
            {
                return;
            }

            try
            {
                timeMachine.ImportTimeMachineHistory(history);
                foreach (var item in history)
                {
                    ApplyHistoryToCanvas(item);
                }
            }
            catch (ArgumentException ex)
            {
                LogHelper.WriteLogToFile(ex, "Board | Failed to restore whiteboard history");
            }
            catch (InvalidOperationException ex)
            {
                LogHelper.WriteLogToFile(ex, "Board | Failed to apply restored whiteboard history");
            }
        }

        private void BtnWhiteBoardSwitchPrevious_Click(object sender, EventArgs e)
        {
            if (CurrentWhiteboardIndex <= 1) return;
            SaveStrokes();
            ClearStrokes(true);
            CurrentWhiteboardIndex--;
            RestoreStrokes();
            UpdateIndexInfoDisplay();
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
            SaveStrokes();
            ClearStrokes(true);
            CurrentWhiteboardIndex++;
            RestoreStrokes();
            UpdateIndexInfoDisplay();
        }

        private void BtnWhiteBoardAdd_Click(object sender, EventArgs e)
        {
            if (WhiteboardTotalCount >= 99) return;
            if (Settings.Automation.IsAutoSaveStrokesAtClear && inkCanvas.Strokes.Count > Settings.Automation.MinimumAutomationStrokeNumber)
            {
                SaveScreenshot(true);
            }
            SaveStrokes();
            ClearStrokes(true);
            WhiteboardTotalCount++;
            CurrentWhiteboardIndex++;
            if (CurrentWhiteboardIndex != WhiteboardTotalCount)
            {
                for (int i = WhiteboardTotalCount; i > CurrentWhiteboardIndex; i--)
                {
                    TimeMachineHistories[i] = TimeMachineHistories[i - 1];
                }
            }
            UpdateIndexInfoDisplay();
        }

        private void BtnWhiteBoardDelete_Click(object sender, RoutedEventArgs e)
        {
            ClearStrokes(true);
            if (CurrentWhiteboardIndex != WhiteboardTotalCount)
            {
                for (int i = CurrentWhiteboardIndex; i <= WhiteboardTotalCount; i++)
                {
                    TimeMachineHistories[i] = TimeMachineHistories[i + 1];
                }
            }
            else
            {
                CurrentWhiteboardIndex--;
            }
            WhiteboardTotalCount--;
            RestoreStrokes();
            UpdateIndexInfoDisplay();
        }

        private void UpdateIndexInfoDisplay()
        {
            TextBlockWhiteBoardIndexInfo.Text = string.Format("{0} / {1}", CurrentWhiteboardIndex, WhiteboardTotalCount);

            if (CurrentWhiteboardIndex == WhiteboardTotalCount)
            {
                BoardLeftPannelNextPage1.Width = 26;
                BoardLeftPannelNextPage2.Width = 0;
                BoardLeftPannelNextPageTextBlock.Text = "加页";
            }
            else
            {
                BoardLeftPannelNextPage1.Width = 0;
                BoardLeftPannelNextPage2.Width = 26;
                BoardLeftPannelNextPageTextBlock.Text = "下一页";
            }

            if (CurrentWhiteboardIndex == 1)
            {
                BtnWhiteBoardSwitchPrevious.IsEnabled = false;
            }
            else
            {
                BtnWhiteBoardSwitchPrevious.IsEnabled = true;
            }

            if (CurrentWhiteboardIndex == 99)
            {
                BoardLeftPannelNextPage1.IsEnabled = false;
            }
            else
            {
                BoardLeftPannelNextPage1.IsEnabled = true;
            }

            if (WhiteboardTotalCount == 99)
            {
                BtnBoardAddPage.IsEnabled = false;
            }
            else
            {
                BtnBoardAddPage.IsEnabled = true;
            }
            /*
            if (WhiteboardTotalCount == 1)
            {
                //BtnWhiteBoardDelete.IsEnabled = false;
            }
            else
            {
                //BtnWhiteBoardDelete.IsEnabled = true;
            }
            */
        }
    }
}
