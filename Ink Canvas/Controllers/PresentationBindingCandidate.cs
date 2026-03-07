using Ink_Canvas.Helpers;

namespace Ink_Canvas.Controllers
{
    internal sealed class PresentationBindingCandidate : IDisposable
    {
        private object? applicationObject;

        public PresentationBindingCandidate(object applicationObject, PresentationRuntimeState state, int priority)
        {
            this.applicationObject = applicationObject;
            State = state;
            Priority = priority;
        }

        public PresentationRuntimeState State { get; }

        public int Priority { get; }

        public object DetachApplicationObject()
        {
            object detachedApplicationObject = applicationObject;
            applicationObject = null;
            return detachedApplicationObject;
        }

        public bool MatchesApplication(object? otherApplicationObject)
        {
            return ComInteropHelper.AreSameComObjects(applicationObject, otherApplicationObject);
        }

        public void Dispose()
        {
            ComInteropHelper.SafeRelease(applicationObject);
            applicationObject = null;
        }
    }
}
