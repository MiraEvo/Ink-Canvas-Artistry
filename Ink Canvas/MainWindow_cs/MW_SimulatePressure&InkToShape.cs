using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;

namespace Ink_Canvas
{
    public partial class MainWindow : Window
    {
        private void inkCanvas_StrokeCollected(object sender, InkCanvasStrokeCollectedEventArgs e)
        {
            inkInteractionCoordinator?.HandleStrokeCollected(e);
        }
    }
}
