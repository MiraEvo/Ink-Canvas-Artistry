using Microsoft.Win32;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Ink_Canvas
{
    public partial class MainWindow : Window
    {
        #region Image
        private async void BtnImageInsert_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new()
            {
                Filter = "Image files (*.jpg; *.jpeg; *.png; *.bmp)|*.jpg;*.jpeg;*.png;*.bmp"
            };
            try
            {
                if (openFileDialog.ShowDialog() != true)
                {
                    return;
                }

                string selectedFilePath = openFileDialog.FileName;
                Image image = await CreateAndCompressImageAsync(selectedFilePath);
                CenterAndScaleElement(image);

                InkCanvas.SetLeft(image, 0);
                InkCanvas.SetTop(image, 0);
                inkCanvas.Children.Add(image);

                inkHistoryCoordinator?.CommitElementInsert(image);
            }
            finally
            {
                DisposeDialogIfNeeded(openFileDialog);
            }
        }

        private async Task<Image> CreateAndCompressImageAsync(string filePath)
        {
            (string elementName, string copiedFilePath) = await CopyDependencyFileAsync(filePath, "img");
            return await Dispatcher.InvokeAsync(() => CreateImageElement(copiedFilePath, elementName));
        }

        private Image CreateImageElement(string filePath, string elementName)
        {
            BitmapImage bitmapImage = new();
            bitmapImage.BeginInit();
            bitmapImage.UriSource = new Uri(filePath);
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
            bitmapImage.EndInit();

            int width = bitmapImage.PixelWidth;
            int height = bitmapImage.PixelHeight;

            Image image = new()
            {
                Name = elementName
            };

            if (isLoaded && Settings.Canvas.IsCompressPicturesUploaded && (width > 1920 || height > 1080))
            {
                double scaleX = 1920.0 / width;
                double scaleY = 1080.0 / height;
                double scale = Math.Min(scaleX, scaleY);

                TransformedBitmap transformedBitmap = new(bitmapImage, new ScaleTransform(scale, scale));
                image.Source = transformedBitmap;
                image.Width = transformedBitmap.PixelWidth;
                image.Height = transformedBitmap.PixelHeight;
                return image;
            }

            image.Source = bitmapImage;
            image.Width = width;
            image.Height = height;
            return image;
        }
        #endregion

        #region Media
        private async void BtnMediaInsert_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new()
            {
                Filter = "Media files (*.mp4; *.avi; *.wmv)|*.mp4;*.avi;*.wmv"
            };
            try
            {
                if (openFileDialog.ShowDialog() != true)
                {
                    return;
                }

                string selectedFilePath = openFileDialog.FileName;
                MediaElement mediaElement = await CreateMediaElementAsync(selectedFilePath);
                CenterAndScaleElement(mediaElement);

                InkCanvas.SetLeft(mediaElement, 0);
                InkCanvas.SetTop(mediaElement, 0);
                inkCanvas.Children.Add(mediaElement);

                mediaElement.Loaded += async (_, _) =>
                {
                    mediaElement.Play();
                    await Task.Delay(100);
                    mediaElement.Pause();
                };

                inkHistoryCoordinator?.CommitElementInsert(mediaElement);
            }
            finally
            {
                DisposeDialogIfNeeded(openFileDialog);
            }
        }

        private async Task<MediaElement> CreateMediaElementAsync(string filePath)
        {
            (string elementName, string copiedFilePath) = await CopyDependencyFileAsync(filePath, "media");
            return await Dispatcher.InvokeAsync(() => CreateMediaElement(copiedFilePath, elementName));
        }

        private static MediaElement CreateMediaElement(string filePath, string elementName)
        {
            return new MediaElement
            {
                Source = new Uri(filePath),
                Name = elementName,
                LoadedBehavior = MediaState.Manual,
                UnloadedBehavior = MediaState.Manual,
                Width = 256,
                Height = 256
            };
        }
        #endregion

        private async Task<(string ElementName, string CopiedFilePath)> CopyDependencyFileAsync(string sourceFilePath, string namePrefix)
        {
            string elementName = $"{namePrefix}_{DateTime.Now:yyyyMMdd_HH_mm_ss_fff}";
            string copiedFilePath = await inkDependencyCacheService.GetOrCreateDependencyFileAsync(
                sourceFilePath,
                $"{namePrefix}{Path.GetExtension(sourceFilePath)}");

            return (elementName, copiedFilePath);
        }

        private static void DisposeDialogIfNeeded(object dialog)
        {
            if (dialog is IDisposable disposableDialog)
            {
                disposableDialog.Dispose();
            }
        }

        private void CenterAndScaleElement(FrameworkElement element)
        {
            double maxWidth = SystemParameters.PrimaryScreenWidth / 2;
            double maxHeight = SystemParameters.PrimaryScreenHeight / 2;

            double scaleX = maxWidth / element.Width;
            double scaleY = maxHeight / element.Height;
            double scale = Math.Min(scaleX, scaleY);

            TransformGroup transformGroup = new();
            transformGroup.Children.Add(new ScaleTransform(scale, scale));

            double canvasWidth = inkCanvas.ActualWidth;
            double canvasHeight = inkCanvas.ActualHeight;
            double centerX = (canvasWidth - element.Width * scale) / 2;
            double centerY = (canvasHeight - element.Height * scale) / 2;

            transformGroup.Children.Add(new TranslateTransform(centerX, centerY));
            element.RenderTransform = transformGroup;
        }
    }
}
