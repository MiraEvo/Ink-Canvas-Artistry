using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace Ink_Canvas
{
    public partial class MainWindow : Window
    {
        private async void FoldFloatingBar_Click(object sender, RoutedEventArgs e)
        {
            await toolbarExperienceCoordinator.HandleFoldFloatingBarAsync(sender != null);
        }

        private async void UnFoldFloatingBar_MouseUp(object sender, MouseButtonEventArgs e)
        {
            await toolbarExperienceCoordinator.HandleUnfoldFloatingBarAsync(sender != null);
        }
    }
}
