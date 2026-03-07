using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Ink_Canvas.Helpers
{
    internal static class AnimationsHelper
    {
        public static void ShowWithSlideFromBottomAndFade(UIElement element, double duration = 0.15)
        {
            FrameworkElement frameworkElement = EnsureFrameworkElement(element);
            if (frameworkElement.Visibility == Visibility.Visible)
            {
                return;
            }

            double fromY = (frameworkElement.RenderTransform?.Value.OffsetY ?? 0) + 10;
            TranslateTransform translateTransform = new TranslateTransform(0, fromY);
            frameworkElement.RenderTransform = translateTransform;

            Storyboard storyboard = new Storyboard();
            storyboard.Children.Add(CreateOpacityAnimation(0.5, 1, duration));
            storyboard.Children.Add(CreateTranslateAnimation(fromY, 0, duration, isVertical: true));

            frameworkElement.Visibility = Visibility.Visible;
            storyboard.Begin(frameworkElement);
        }

        public static void ShowWithSlideFromLeftAndFade(UIElement element, double duration = 0.25)
        {
            FrameworkElement frameworkElement = EnsureFrameworkElement(element);
            if (frameworkElement.Visibility == Visibility.Visible)
            {
                return;
            }

            double fromX = (frameworkElement.RenderTransform?.Value.OffsetX ?? 0) - 20;
            TranslateTransform translateTransform = new TranslateTransform(fromX, 0);
            frameworkElement.RenderTransform = translateTransform;

            Storyboard storyboard = new Storyboard();
            storyboard.Children.Add(CreateOpacityAnimation(0.5, 1, duration));
            storyboard.Children.Add(CreateTranslateAnimation(fromX, 0, duration, isVertical: false));

            frameworkElement.Visibility = Visibility.Visible;
            storyboard.Begin(frameworkElement);
        }

        public static void ShowWithScaleFromLeft(UIElement element, double duration = 0.5)
        {
            ShowWithScale(element, duration, new Point(0, 0.5), 0, 0);
        }

        public static void ShowWithScaleFromRight(UIElement element, double duration = 0.5)
        {
            ShowWithScale(element, duration, new Point(1, 0.5), 0, 0);
        }

        public static void ShowWithScaleFromBottom(UIElement element, double duration = 0.5)
        {
            ShowWithScale(element, duration, new Point(0.5, 1), 1, 0);
        }

        public static void HideWithSlideAndFade(UIElement element, double duration = 0.15)
        {
            FrameworkElement frameworkElement = EnsureFrameworkElement(element);
            if (frameworkElement.Visibility == Visibility.Collapsed)
            {
                return;
            }

            double toY = (frameworkElement.RenderTransform?.Value.OffsetY ?? 0) + 10;
            frameworkElement.RenderTransform = new TranslateTransform();

            Storyboard storyboard = new Storyboard();
            storyboard.Children.Add(CreateOpacityAnimation(1, 0, duration));
            storyboard.Children.Add(CreateTranslateAnimation(0, toY, duration, isVertical: true));
            storyboard.Completed += (_, __) => frameworkElement.Visibility = Visibility.Collapsed;

            storyboard.Begin(frameworkElement);
        }

        private static void ShowWithScale(UIElement element, double duration, Point origin, double fromScaleX, double fromScaleY)
        {
            FrameworkElement frameworkElement = EnsureFrameworkElement(element);
            if (frameworkElement.Visibility == Visibility.Visible)
            {
                return;
            }

            frameworkElement.Visibility = Visibility.Visible;
            frameworkElement.RenderTransformOrigin = origin;
            frameworkElement.RenderTransform = new ScaleTransform(fromScaleX, fromScaleY);

            Storyboard storyboard = new Storyboard();
            storyboard.Children.Add(CreateScaleAnimation(fromScaleX, 1, duration, axis: "X"));
            storyboard.Children.Add(CreateScaleAnimation(fromScaleY, 1, duration, axis: "Y"));
            storyboard.Begin(frameworkElement);
        }

        private static FrameworkElement EnsureFrameworkElement(UIElement element)
        {
            ArgumentNullException.ThrowIfNull(element);
            if (element is FrameworkElement frameworkElement)
            {
                return frameworkElement;
            }

            throw new ArgumentException("Animation target must be a FrameworkElement.", nameof(element));
        }

        private static DoubleAnimation CreateOpacityAnimation(double from, double to, double duration)
        {
            DoubleAnimation animation = new DoubleAnimation
            {
                From = from,
                To = to,
                Duration = TimeSpan.FromSeconds(duration)
            };
            Storyboard.SetTargetProperty(animation, new PropertyPath(UIElement.OpacityProperty));
            return animation;
        }

        private static DoubleAnimation CreateTranslateAnimation(double from, double to, double duration, bool isVertical)
        {
            DoubleAnimation animation = new DoubleAnimation
            {
                From = from,
                To = to,
                Duration = TimeSpan.FromSeconds(duration)
            };
            Storyboard.SetTargetProperty(
                animation,
                new PropertyPath(isVertical
                    ? "(UIElement.RenderTransform).(TranslateTransform.Y)"
                    : "(UIElement.RenderTransform).(TranslateTransform.X)"));
            return animation;
        }

        private static DoubleAnimation CreateScaleAnimation(double from, double to, double duration, string axis)
        {
            DoubleAnimation animation = new DoubleAnimation
            {
                From = from,
                To = to,
                Duration = TimeSpan.FromSeconds(duration)
            };
            Storyboard.SetTargetProperty(
                animation,
                new PropertyPath($"(UIElement.RenderTransform).(ScaleTransform.Scale{axis})"));
            return animation;
        }
    }
}
