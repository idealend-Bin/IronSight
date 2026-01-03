using System.Windows;
using System.Windows.Controls;
using IronSight.App.UI.ViewModels;

namespace IronSight.App.UI.Views
{
    /// <summary>
    /// NetworkMonitorView的交互逻辑
    /// </summary>
    public partial class NetworkMonitorView : UserControl
    {
        public NetworkMonitorView()
        {
            InitializeComponent();
        }

        private async void OnRefreshClick(object sender, RoutedEventArgs e)
        {
            if (DataContext != null)
            {
                await (DataContext as NetworkMonitorViewModel)?.RefreshAsync();
            }
        }
    }
}

