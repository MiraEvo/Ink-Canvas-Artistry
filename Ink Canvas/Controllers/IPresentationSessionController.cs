using Microsoft.Office.Interop.PowerPoint;
using System;

namespace Ink_Canvas.Controllers
{
    public interface IPresentationSessionController
    {
        event Action? PresentationConnected;

        event Action? PresentationClosed;

        event Action? SlideShowBegin;

        event Action? SlideShowNextSlide;

        event Action? SlideShowEnd;

        Microsoft.Office.Interop.PowerPoint.Application? PowerPointApplication { get; }

        Presentation? Presentation { get; }

        Slides? Slides { get; }

        Slide? Slide { get; }

        bool TryGoToSlide(int slideNumber);

        bool TryGoToPreviousSlide();

        bool TryGoToNextSlide();

        bool TryExitSlideShow();

        bool TryShowSlideNavigation();

        bool HasHiddenSlides();

        bool TryUnhideHiddenSlides();

        bool HasAutomaticAdvance();

        bool TryDisableAutomaticAdvance();

        void StartMonitoring();

        void StopMonitoring();
    }
}
