using System.Runtime.InteropServices;

namespace IronSight.Interop.Core
{
    public static class DllMain
    {
        public static bool IsInitialized { get; private set; }

        public static void Initialize()
        {
            if (IsInitialized) return;

            // --- 第一步：启动日志枢纽 ---
            // 只有先执行这一步，后续 C++ 内部的 LOG_XXX 宏才能生效
            LoggerService.Initialize("IronsightTrace.log");

            // --- 第二步：原生层提权 ---
            // 此时 EnableDebugPrivilege 内部的日志会直接流入 IronsightTrace.log
            bool result = EnableDebugPrivilege(true);

            if (result)
            {
                // 使用我们自己的日志系统记录初始化成功
                // 这里可以模拟一个从 C# 主动发起的 Trace
                // 建议在 LoggerService 里暴露一个 Log 方法供 C# 直接使用
                System.Diagnostics.Debug.WriteLine("[INFO] 权限提升成功。");
            }
            else
            {
                // 如果失败，日志会同时出现在 VS 窗口、控制台和文件
                System.Diagnostics.Debug.WriteLine("[WARN] 警告：权限提升失败。");
            }

            IsInitialized = true;
        }

        [DllImport("IronSight.Core.Native.dll", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool EnableDebugPrivilege([MarshalAs(UnmanagedType.Bool)] bool enableFlag);
    }
}