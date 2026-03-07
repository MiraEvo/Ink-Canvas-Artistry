using Ink_Canvas.Helpers;
using Microsoft.Win32;
using System;
using System.IO;
using System.IO.Compression;
using System.Security;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Media.Imaging;
using System.Threading.Tasks;

namespace Ink_Canvas
{
    public partial class MainWindow : Window
    {
        private void SymbolIconSaveStrokes_Click(object sender, RoutedEventArgs e)
        {
            if (inkCanvas.Visibility != Visibility.Visible) return;
            AnimationsHelper.HideWithSlideAndFade(BorderTools);
            AnimationsHelper.HideWithSlideAndFade(BoardBorderTools);
            GridNotifications.Visibility = Visibility.Collapsed;
            SaveInkCanvasFile(true, true);
        }

        private void SaveInkCanvasFile(bool newNotice = true, bool saveByUser = false)
        {
            try
            {
                string savePath = Settings.Automation.AutoSavedStrokesLocation
                    + (saveByUser ? @"\User Saved - " : @"\Auto Saved - ")
                    + (ShellViewModel.IsDesktopAnnotationMode ? "Annotation Strokes" : "BlackBoard Strokes");

                if (!Directory.Exists(savePath))
                {
                    Directory.CreateDirectory(savePath);
                }

                string savePathWithName = savePath + @"\" + DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss-fff")
                    + (ShellViewModel.IsBlackboardMode ? " Page-" + CurrentWhiteboardIndex + " StrokesCount-" + inkCanvas.Strokes.Count + ".icart" : ".icart");

                using (FileStream fs = new FileStream(savePathWithName, FileMode.Create))
                using (var archive = new ZipArchive(fs, ZipArchiveMode.Create))
                {
                    // Save strokes
                    var strokesEntry = archive.CreateEntry("strokes.icstk");
                    using (var strokesStream = strokesEntry.Open())
                    {
                        inkCanvas.Strokes.Save(strokesStream);
                    }

                    // Save UI elements
                    var elementsEntry = archive.CreateEntry("elements.xaml");
                    using (var elementsStream = elementsEntry.Open())
                    {
                        InkCanvasArchiveElementsSerializer.SaveElements(inkCanvas, elementsStream);
                    }

                    // Save related URL files
                    SaveRelatedUrlFiles(archive);

                    if (newNotice)
                    {
                        ShowNotificationAsync("墨迹及元素成功保存至 " + savePathWithName);
                    }
                }
            }
            catch (ArgumentException ex)
            {
                HandleInkArchiveFailure(ex, "Save | Invalid save path or archive state", "墨迹及元素保存失败！");
            }
            catch (IOException ex)
            {
                HandleInkArchiveFailure(ex, "Save | Failed to persist ink archive", "墨迹及元素保存失败！");
            }
            catch (InvalidOperationException ex)
            {
                HandleInkArchiveFailure(ex, "Save | Failed to create ink archive", "墨迹及元素保存失败！");
            }
            catch (UnauthorizedAccessException ex)
            {
                HandleInkArchiveFailure(ex, "Save | Access denied while saving ink archive", "墨迹及元素保存失败！");
            }
            catch (SecurityException ex)
            {
                HandleInkArchiveFailure(ex, "Save | Security error while saving ink archive", "墨迹及元素保存失败！");
            }
            catch (NotSupportedException ex)
            {
                HandleInkArchiveFailure(ex, "Save | Unsupported save path or archive operation", "墨迹及元素保存失败！");
            }
        }

        private void SaveRelatedUrlFiles(ZipArchive archive)
        {
            string dependencyFolder = InkCanvasArchiveElementsSerializer.DependencyFolderName;
            archive.CreateEntry(dependencyFolder + "/");
            foreach (UIElement element in inkCanvas.Children)
            {
                if (InkCanvasArchiveElementsSerializer.TryGetDependencySourcePath(element, out string sourcePath))
                {
                    AddFileToArchive(archive, sourcePath, dependencyFolder);
                }
                else
                {
                    LogHelper.WriteLogToFile("该元素类型暂不支持保存", LogHelper.LogType.Error);
                }
            }
        }

        private void AddFileToArchive(ZipArchive archive, string filePath, string folderName)
        {
            if (File.Exists(filePath))
            {
                string fileName = Path.GetFileName(filePath);
                if (string.IsNullOrWhiteSpace(fileName))
                {
                    LogHelper.WriteLogToFile($"Elements Save | Skipped dependency with invalid file name: {filePath}", LogHelper.LogType.Error);
                    return;
                }

                var fileEntry = archive.CreateEntry(folderName + "/" + fileName);
                using (var entryStream = fileEntry.Open())
                using (var fileStream = File.OpenRead(filePath))
                {
                    fileStream.CopyTo(entryStream);
                }
            }
        }



        private void SymbolIconOpenInkCanvasFile_Click(object sender, RoutedEventArgs e)
        {
            AnimationsHelper.HideWithSlideAndFade(BorderTools);
            AnimationsHelper.HideWithSlideAndFade(BoardBorderTools);

            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                InitialDirectory = Settings.Automation.AutoSavedStrokesLocation,
                Title = "打开墨迹文件",
                Filter = "Ink Canvas Files (*.icart;*.icstk)|*.icart;*.icstk|Ink Canvas Artistry Files (*.icart)|*.icart|Ink Canvas Stroke Files (*.icstk)|*.icstk"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                LogHelper.WriteLogToFile($"Strokes Insert: Name: {openFileDialog.FileName}", LogHelper.LogType.Event);

                try
                {
                    string extension = Path.GetExtension(openFileDialog.FileName).ToLower();
                    using (var fs = new FileStream(openFileDialog.FileName, FileMode.Open))
                    {
                        if (extension == ".icart")
                        {
                            using (var archive = new ZipArchive(fs, ZipArchiveMode.Read))
                            {
                                // load strokes
                                var strokesEntry = archive.GetEntry("strokes.icstk");
                                if (strokesEntry != null)
                                {
                                    using (var strokesStream = strokesEntry.Open())
                                    {
                                        var strokes = new StrokeCollection(strokesStream);
                                        ClearStrokes(true);
                                        timeMachine.ClearStrokeHistory();
                                        inkCanvas.Strokes.Add(strokes);
                                        LogHelper.NewLog($"Strokes Insert: Strokes Count: {inkCanvas.Strokes.Count}");
                                    }
                                }

                                // load URL files
                                string saveDirectory = Settings.Automation.AutoSavedStrokesLocation;
                                ExtractUrlFiles(archive, saveDirectory);

                                // load UI Elements
                                var elementsEntry = archive.GetEntry("elements.xaml");
                                if (elementsEntry != null)
                                {
                                    using (var elementsStream = elementsEntry.Open())
                                    {
                                        try
                                        {
                                            var loadedElements = InkCanvasArchiveElementsSerializer.LoadElements(
                                                elementsStream,
                                                Path.Combine(saveDirectory, InkCanvasArchiveElementsSerializer.DependencyFolderName));

                                            inkCanvas.Children.Clear();
                                            foreach (UIElement child in loadedElements)
                                            {
                                                inkCanvas.Children.Add(child);
                                            }

                                            LogHelper.NewLog($"Elements Insert: Elements Count: {inkCanvas.Children.Count}");
                                        }
                                        catch (ArgumentException ex)
                                        {
                                            HandleInkArchiveFailure(ex, "Open | Invalid serialized UI elements", "加载 UI 元素失败");
                                        }
                                        catch (IOException ex)
                                        {
                                            HandleInkArchiveFailure(ex, "Open | Failed to load UI element dependencies", "加载 UI 元素失败");
                                        }
                                        catch (InvalidOperationException ex)
                                        {
                                            HandleInkArchiveFailure(ex, "Open | Failed to deserialize UI elements", "加载 UI 元素失败");
                                        }
                                        catch (UnauthorizedAccessException ex)
                                        {
                                            HandleInkArchiveFailure(ex, "Open | Access denied while loading UI elements", "加载 UI 元素失败");
                                        }
                                        catch (NotSupportedException ex)
                                        {
                                            HandleInkArchiveFailure(ex, "Open | Unsupported UI element dependency", "加载 UI 元素失败");
                                        }
                                    }
                                }
                            }
                        }
                        else if (extension == ".icstk")
                        {
                            // 直接加载 .icstk 文件中的墨迹
                            using (var strokesStream = new MemoryStream())
                            {
                                fs.CopyTo(strokesStream);
                                strokesStream.Seek(0, SeekOrigin.Begin);
                                var strokes = new StrokeCollection(strokesStream);
                                ClearStrokes(true);
                                timeMachine.ClearStrokeHistory();
                                inkCanvas.Strokes.Add(strokes);
                                LogHelper.NewLog($"Strokes Insert: Strokes Count: {inkCanvas.Strokes.Count}");
                            }
                        }
                        else
                        {
                            ShowNotificationAsync("不支持的文件格式。");
                        }

                        if (inkCanvas.Visibility != Visibility.Visible)
                        {
                            SymbolIconCursor_Click(sender, null);
                        }
                    }
                }
                catch (ArgumentException ex)
                {
                    HandleInkArchiveFailure(ex, "Open | Invalid ink archive path or entry", "墨迹或元素打开失败");
                }
                catch (IOException ex)
                {
                    HandleInkArchiveFailure(ex, "Open | Failed to read ink archive", "墨迹或元素打开失败");
                }
                catch (InvalidDataException ex)
                {
                    HandleInkArchiveFailure(ex, "Open | Invalid ink archive data", "墨迹或元素打开失败");
                }
                catch (InvalidOperationException ex)
                {
                    HandleInkArchiveFailure(ex, "Open | Failed to process ink archive", "墨迹或元素打开失败");
                }
                catch (UnauthorizedAccessException ex)
                {
                    HandleInkArchiveFailure(ex, "Open | Access denied while opening ink archive", "墨迹或元素打开失败");
                }
                catch (SecurityException ex)
                {
                    HandleInkArchiveFailure(ex, "Open | Security error while opening ink archive", "墨迹或元素打开失败");
                }
                catch (NotSupportedException ex)
                {
                    HandleInkArchiveFailure(ex, "Open | Unsupported ink archive path or operation", "墨迹或元素打开失败");
                }
            }
        }

        private void ExtractUrlFiles(ZipArchive archive, string outputDirectory)
        {
            string outputRoot = Path.GetFullPath(outputDirectory);
            foreach (var entry in archive.Entries)
            {
                if (entry.FullName.StartsWith(InkCanvasArchiveElementsSerializer.DependencyFolderName + "/", StringComparison.OrdinalIgnoreCase))
                {
                    if (!string.IsNullOrEmpty(entry.Name))
                    {
                        try
                        {
                            string relativePath = entry.FullName.Replace('/', Path.DirectorySeparatorChar);
                            string fileName = Path.GetFullPath(Path.Join(outputRoot, relativePath));
                            string normalizedRoot = AppendDirectorySeparator(outputRoot);

                            if (!fileName.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase))
                            {
                                LogHelper.WriteLogToFile($"Elements Load | Rejected archive entry outside dependency directory: {entry.FullName}", LogHelper.LogType.Error);
                                continue;
                            }

                            string directoryPath = Path.GetDirectoryName(fileName);
                            if (string.IsNullOrWhiteSpace(directoryPath))
                            {
                                LogHelper.WriteLogToFile($"Elements Load | Missing target directory for archive entry: {entry.FullName}", LogHelper.LogType.Error);
                                continue;
                            }

                            if (!Directory.Exists(directoryPath))
                            {
                                Directory.CreateDirectory(directoryPath);
                            }

                            if (!File.Exists(fileName))
                            {
                                entry.ExtractToFile(fileName, overwrite: false);
                            }
                        }
                        catch (ArgumentException ex)
                        {
                            LogHelper.WriteLogToFile(ex, $"Elements Load | Invalid archive entry '{entry.FullName}'");
                        }
                        catch (IOException ex)
                        {
                            LogHelper.WriteLogToFile(ex, $"Elements Load | Failed to extract archive entry '{entry.FullName}'");
                        }
                        catch (InvalidDataException ex)
                        {
                            LogHelper.WriteLogToFile(ex, $"Elements Load | Invalid archive entry data '{entry.FullName}'");
                        }
                        catch (UnauthorizedAccessException ex)
                        {
                            LogHelper.WriteLogToFile(ex, $"Elements Load | Access denied while extracting archive entry '{entry.FullName}'");
                        }
                        catch (SecurityException ex)
                        {
                            LogHelper.WriteLogToFile(ex, $"Elements Load | Security error while extracting archive entry '{entry.FullName}'");
                        }
                        catch (NotSupportedException ex)
                        {
                            LogHelper.WriteLogToFile(ex, $"Elements Load | Unsupported archive entry '{entry.FullName}'");
                        }
                    }
                }
            }
        }

        private void HandleInkArchiveFailure(Exception exception, string context, string notification)
        {
            ShowNotificationAsync(notification);
            LogHelper.WriteLogToFile(exception, context);
        }

        private static string AppendDirectorySeparator(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return path;
            }

            return path.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal)
                ? path
                : path + Path.DirectorySeparatorChar;
        }
    }
}
