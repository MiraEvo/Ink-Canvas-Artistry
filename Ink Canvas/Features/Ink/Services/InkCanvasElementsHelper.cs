using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Ink_Canvas.Features.Ink.Services
{
    public static class InkCanvasElementsHelper
    {
        public static Point GetAllElementsBoundsCenterPoint(InkCanvas inkCanvas)
        {
            Rect bounds = inkCanvas.GetSelectionBounds();
            return new Point(bounds.Left + bounds.Width / 2, bounds.Top + bounds.Height / 2);
        }

        public static bool IsNotCanvasElementSelected(InkCanvas inkCanvas)
        {
            return inkCanvas.GetSelectedStrokes().Count == 0 && inkCanvas.GetSelectedElements().Count == 0;
        }

        public static List<UIElement> GetAllElements(InkCanvas inkCanvas)
        {
            return inkCanvas.Children.Cast<UIElement>().ToList();
        }

        public static List<UIElement> GetSelectedElements(InkCanvas inkCanvas)
        {
            return inkCanvas.GetSelectedElements().Cast<UIElement>().ToList();
        }

        public class ElementData
        {
            public double SetLeftData { get; set; }
            public double SetTopData { get; set; }
            public FrameworkElement FrameworkElement { get; set; }
        }

        public static List<UIElement> CloneSelectedElements(InkCanvas inkCanvas, ref Dictionary<string, object> ElementsInitialHistory)
        {
            List<UIElement> clonedElements = new List<UIElement>();
            int key = 0;
            foreach (var cloneCandidate in inkCanvas.GetSelectedElements()
                         .Cast<UIElement>()
                         .Select(element => new
                         {
                             Element = element,
                             FrameworkElement = CloneUIElement(element) as FrameworkElement
                         })
                         .Where(candidate => candidate.FrameworkElement != null))
            {
                FrameworkElement frameworkElement = cloneCandidate.FrameworkElement!;
                string timestamp = $"ele_{DateTime.Now:ddHHmmssfff}{key}";
                frameworkElement.Name = timestamp;
                ++key;
                InkCanvas.SetLeft(frameworkElement, InkCanvas.GetLeft(cloneCandidate.Element));
                InkCanvas.SetTop(frameworkElement, InkCanvas.GetTop(cloneCandidate.Element));
                inkCanvas.Children.Add(frameworkElement);
                clonedElements.Add(frameworkElement);
                ElementsInitialHistory[frameworkElement.Name] = new ElementData
                {
                    SetLeftData = InkCanvas.GetLeft(cloneCandidate.Element),
                    SetTopData = InkCanvas.GetTop(cloneCandidate.Element),
                    FrameworkElement = frameworkElement
                };
            }
            return clonedElements;
        }

        public static List<UIElement> GetSelectedElementsCloned(InkCanvas inkCanvas)
        {
            List<UIElement> clonedElements = new List<UIElement>();
            int key = 0;
            foreach (var cloneCandidate in inkCanvas.GetSelectedElements()
                         .Cast<UIElement>()
                         .Select(element => new
                         {
                             Element = element,
                             FrameworkElement = CloneUIElement(element) as FrameworkElement
                         })
                         .Where(candidate => candidate.FrameworkElement != null))
            {
                FrameworkElement frameworkElement = cloneCandidate.FrameworkElement!;
                string timestamp = $"ele_{DateTime.Now:ddHHmmssfff}{key}";
                frameworkElement.Name = timestamp;
                ++key;
                InkCanvas.SetLeft(frameworkElement, InkCanvas.GetLeft(cloneCandidate.Element));
                InkCanvas.SetTop(frameworkElement, InkCanvas.GetTop(cloneCandidate.Element));
                clonedElements.Add(frameworkElement);
            }
            return clonedElements;
        }

        public static void AddElements(InkCanvas inkCanvas, List<UIElement> elements, TimeMachine timeMachine)
        {
            foreach (UIElement element in elements)
            {
                inkCanvas.Children.Add(element);
                timeMachine.CommitElementInsertHistory(element);
            }
        }

        private static UIElement CloneUIElement(UIElement element)
        {
            if (element == null) return null;

            if (element is Image originalImage)
            {
                return CloneImage(originalImage);
            }
            
            if (element is MediaElement originalMediaElement)
            {
                return CloneMediaElement(originalMediaElement);
            }

            if (element is FrameworkElement frameworkElement)
            {
                var clonedElement = (UIElement)Activator.CreateInstance(element.GetType());
                if (clonedElement is FrameworkElement clonedFrameworkElement)
                {
                    clonedFrameworkElement.Width = frameworkElement.Width;
                    clonedFrameworkElement.Height = frameworkElement.Height;
                    clonedFrameworkElement.Margin = frameworkElement.Margin;
                    clonedFrameworkElement.HorizontalAlignment = frameworkElement.HorizontalAlignment;
                    clonedFrameworkElement.VerticalAlignment = frameworkElement.VerticalAlignment;
                    clonedFrameworkElement.DataContext = frameworkElement.DataContext;
                }
                return clonedElement;
            }

            return null;
        }

        private static Image CloneImage(Image originalImage)
        {
            Image clonedImage = new Image
            {
                Source = originalImage.Source,
                Width = originalImage.Width,
                Height = originalImage.Height,
                Stretch = originalImage.Stretch,
                Opacity = originalImage.Opacity,
                RenderTransform = originalImage.RenderTransform.Clone()
            };
            return clonedImage;
        }

        private static MediaElement CloneMediaElement(MediaElement originalMediaElement)
        {
            MediaElement clonedMediaElement = new MediaElement
            {
                Source = originalMediaElement.Source,
                Width = originalMediaElement.Width,
                Height = originalMediaElement.Height,
                Stretch = originalMediaElement.Stretch,
                Opacity = originalMediaElement.Opacity,
                RenderTransform = originalMediaElement.RenderTransform.Clone(),
                LoadedBehavior = originalMediaElement.LoadedBehavior,
                UnloadedBehavior = originalMediaElement.UnloadedBehavior,
                Volume = originalMediaElement.Volume,
                Balance = originalMediaElement.Balance,
                IsMuted = originalMediaElement.IsMuted,
                ScrubbingEnabled = originalMediaElement.ScrubbingEnabled
            };
            clonedMediaElement.Loaded += async (sender, args) =>
            {
                clonedMediaElement.Play();
                await Task.Delay(100);
                clonedMediaElement.Pause();
            };
            return clonedMediaElement;
        }
    }
}

