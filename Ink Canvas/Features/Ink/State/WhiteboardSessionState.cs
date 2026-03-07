using Ink_Canvas.Helpers;
using System.Windows.Ink;

namespace Ink_Canvas.Features.Ink.State
{
    internal sealed class WhiteboardSessionState
    {
        public StrokeCollection[] StrokeCollections { get; } = new StrokeCollection[101];

        public StrokeCollection LastTouchDownStrokeCollection { get; set; } = new();

        public int CurrentWhiteboardIndex { get; set; } = 1;

        public int WhiteboardTotalCount { get; set; } = 1;

        public TimeMachineHistory[][] TimeMachineHistories { get; } = new TimeMachineHistory[101][];
    }
}

