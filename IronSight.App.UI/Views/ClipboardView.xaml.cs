using System.Windows;
using System.Windows.Controls;

namespace IronSight.App.UI.Views
{
    /// <summary>
    /// Interaction logic for ClipboardView.xaml
    /// </summary>
    public partial class ClipboardView : UserControl
    {
        public ClipboardView()
        {
            InitializeComponent();
        }

        

        private void Copy_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is ViewModels.ClipboardItem clipboardItem)
            {
                try
                {
                    System.Windows.Clipboard.SetText(clipboardItem.Content);
                }
                catch (Exception ex)
                {
                    if (DataContext is ViewModels.MainViewModel viewModel)
                    {
                        viewModel.StatusMessage = "复制失败: " + ex.Message;
                    }
                }
            }
        }
    }
}
