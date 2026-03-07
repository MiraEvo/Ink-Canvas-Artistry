using System.Windows.Forms;

namespace Ink_Canvas.Services
{
    public sealed class PathPickerService : IPathPickerService
    {
        public string PickFolder(string initialPath)
        {
            using FolderBrowserDialog folderBrowser = new FolderBrowserDialog
            {
                SelectedPath = initialPath ?? string.Empty,
                ShowNewFolderButton = true
            };

            return folderBrowser.ShowDialog() == DialogResult.OK
                ? folderBrowser.SelectedPath
                : null;
        }
    }
}
