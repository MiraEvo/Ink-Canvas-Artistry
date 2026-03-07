using System;
using System.Collections.Generic;

namespace Ink_Canvas.Features.Presentation
{
    internal sealed class PresentationInkSessionState
    {
        private byte[]?[] slideInkBuffers = [];

        public string PresentationName { get; private set; } = string.Empty;

        public int CurrentSlideIndex { get; private set; }

        public int PreviousSlideIndex { get; private set; }

        public bool IsSlideShowEndHandled { get; private set; }

        public bool IsNavigationButtonTurnPending { get; private set; }

        public int SlideCount => Math.Max(0, slideInkBuffers.Length - 2);

        public void Start(string presentationName, int slideCount, int currentSlideIndex)
        {
            PresentationName = presentationName ?? string.Empty;
            slideInkBuffers = new byte[Math.Max(0, slideCount) + 2][];
            CurrentSlideIndex = Math.Max(0, currentSlideIndex);
            PreviousSlideIndex = 0;
            IsSlideShowEndHandled = false;
            IsNavigationButtonTurnPending = false;
        }

        public void End()
        {
            PresentationName = string.Empty;
            slideInkBuffers = [];
            CurrentSlideIndex = 0;
            PreviousSlideIndex = 0;
            IsSlideShowEndHandled = false;
            IsNavigationButtonTurnPending = false;
        }

        public void SetCurrentSlideIndex(int slideIndex)
        {
            CurrentSlideIndex = Math.Max(0, slideIndex);
        }

        public void SetPreviousSlideIndex(int slideIndex)
        {
            PreviousSlideIndex = Math.Max(0, slideIndex);
        }

        public void MarkNavigationButtonTurnRequested()
        {
            IsNavigationButtonTurnPending = true;
        }

        public void ClearNavigationButtonTurnRequested()
        {
            IsNavigationButtonTurnPending = false;
        }

        public bool TryBeginSlideShowEnd()
        {
            if (IsSlideShowEndHandled)
            {
                return false;
            }

            IsSlideShowEndHandled = true;
            return true;
        }

        public void SetSlideInk(int slideIndex, byte[]? inkData)
        {
            if (!IsBufferIndexValid(slideIndex))
            {
                return;
            }

            slideInkBuffers[slideIndex] = inkData;
        }

        public bool TryGetSlideInk(int slideIndex, out byte[]? inkData)
        {
            inkData = null;
            if (!IsBufferIndexValid(slideIndex))
            {
                return false;
            }

            inkData = slideInkBuffers[slideIndex];
            return inkData is { Length: > 0 };
        }

        public IEnumerable<(int SlideIndex, byte[] InkData)> EnumerateSavedInk()
        {
            for (int i = 1; i < slideInkBuffers.Length; i++)
            {
                if (slideInkBuffers[i] is { Length: > 0 } inkData)
                {
                    yield return (i, inkData);
                }
            }
        }

        private bool IsBufferIndexValid(int slideIndex)
        {
            return slideIndex > 0 && slideIndex < slideInkBuffers.Length;
        }
    }
}
