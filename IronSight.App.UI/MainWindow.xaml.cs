using IronSight.App.UI.ViewModels;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;  
using System.Windows.Input;


namespace IronSight.App.UI
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }



        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            (DataContext as IDisposable)?.Dispose();
        }
        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // 1. 判断是否是双击（如果是双击，通常触发最大化/还原）
            if (e.ClickCount == 2)
            {
                Maximize_Click(sender, e);
                return;
            }

            // 2. 如果当前是最大化状态，拖动时需要先还原
            if (this.WindowState == WindowState.Maximized)
            {
                // 这是一个小技巧：记录鼠标相对比例，防止窗口还原后“飞”掉
                var mousePos = e.GetPosition(this);
                var width = this.ActualWidth;
                var ratio = mousePos.X / width;

                this.WindowState = WindowState.Normal;

                // 将窗口移动到鼠标位置，使用户感觉是“拖”下来的
                this.Left = e.GetPosition(null).X - (this.ActualWidth * ratio);
                this.Top = e.GetPosition(null).Y - 10; // 稍微偏离顶部
            }

            // 3. 允许拖动窗口
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }

        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void Maximize_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = this.WindowState == WindowState.Maximized
                ? WindowState.Normal
                : WindowState.Maximized;
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Nav_Checked(object sender, RoutedEventArgs e)
        {
            if (MainTabControl != null &&
                sender is System.Windows.Controls.Primitives.ButtonBase button &&
                int.TryParse(button.CommandParameter?.ToString(), out int index))
            {
                MainTabControl.SelectedIndex = index;
            }
        }
    }
}