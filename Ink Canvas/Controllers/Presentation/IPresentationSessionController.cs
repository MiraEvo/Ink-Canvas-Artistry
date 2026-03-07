using Microsoft.Office.Interop.PowerPoint;
using System;
using PowerPointPresentation = Microsoft.Office.Interop.PowerPoint.Presentation;

namespace Ink_Canvas.Controllers.Presentation
{
    public interface IPresentationSessionController
    {
        event Action? PresentationConnected;

        event Action? PresentationClosed;

        event Action? SlideShowBegin;

        event Action? SlideShowNextSlide;

        event Action? SlideShowEnd;

        Microsoft.Office.Interop.PowerPoint.Application? PowerPointApplication { get; }

        PowerPointPresentation? Presentation { get; }

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

