using System;

namespace Ink_Canvas.Controllers
{
    public interface IAutomationController : IDisposable
    {
        void Initialize();

        void RefreshAutoFoldMonitoring();

        void RefreshProcessKillMonitoring();

        void ScheduleSilentUpdate(string version);

        void CancelSilentUpdate();
    }
}
