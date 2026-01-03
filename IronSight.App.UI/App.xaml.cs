using System.Configuration;
using System.Data;
using System.Windows;

namespace IronSight.App.UI
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            // 1. 调用基类逻辑
            base.OnStartup(e);

            // 2. 初始化你的双层握手逻辑
            // 这会触发 Logger 初始化和 C++ 提权
            IronSight.Interop.Core.DllMain.Initialize();

            // 此时，日志已经开始写入，权限已经拿到
        }
    }

}
