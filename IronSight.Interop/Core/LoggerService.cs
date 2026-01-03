using System;
using System.Runtime.InteropServices;
using System.IO;

namespace IronSight.Interop.Core
{
    public enum LogLevel : UInt32
    {
        Trace = 0,
        Debug = 1,
        Info = 2,
        Warn = 3,
        Error = 4,
        Fatal = 5
    }

    public static class LoggerService
    {
        // 必须持久化 Delegate，防止被 GC 回收导致底层调用野指针
        private static LogDispatcherCallback? _nativeCallback;
        private static StreamWriter? _fileWriter;
        private static readonly object _lock = new object();

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate void LogDispatcherCallback(LogLevel level, [MarshalAs(UnmanagedType.LPStr)] string message);

        [DllImport("IronSight.Core.Native.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern void RegisterLogCallback(LogDispatcherCallback callback);

        public static void Log(LogLevel level, string message)
        {
            // Same logic as callback
            WriteLog(level, message);
        }

        private static void WriteLog(LogLevel level, string message)
        {
#if !DEBUG
            if (level == LogLevel.Debug || level == LogLevel.Trace) return;
#endif
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            string logLine = $"[{timestamp}] [{level}] {message}";

            lock (_lock)
            {
                System.Diagnostics.Debug.WriteLine(logLine);
                Console.WriteLine(logLine);
                _fileWriter?.WriteLine(logLine);
            }
        }

        public static void Initialize(string logFileName = "ironsight.log")
        {
            lock (_lock)
            {
                // 初始化文件流
                string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "IronSight", logFileName);
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                _fileWriter = new StreamWriter(path, append: true) { AutoFlush = true };

                // 定义分发逻辑
                _nativeCallback = WriteLog;

                // 注册到原生层

                RegisterLogCallback(_nativeCallback);

            }
        }
    }
}