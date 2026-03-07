using Microsoft.Office.Interop.PowerPoint;
using System;

namespace Ink_Canvas.Controllers
{
    public interface IPresentationSessionController
    {
        event Action<Presentation>? PresentationConnected;

        event Action<Presentation>? PresentationClosed;

        event Action<SlideShowWindow>? SlideShowBegin;

        event Action<SlideShowWindow>? SlideShowNextSlide;

        event Action<Presentation>? SlideShowEnd;

        Microsoft.Office.Interop.PowerPoint.Application? PowerPointApplication { get; }

        Presentation? Presentation { get; }

        Slides? Slides { get; }

        Slide? Slide { get; }

        void StartMonitoring();

        void StopMonitoring();
    }
}
