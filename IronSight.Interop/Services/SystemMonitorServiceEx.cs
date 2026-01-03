using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using IronSight.Interop.Native.System;
using IronSight.Interop.Core;

namespace IronSight.Interop.Services
{
    /// <summary>
    /// 系统监控服务扩展版 - 专门负责高频、深度的进程与性能分析
    /// </summary>
    public class SystemMonitorServiceEx : IDisposable
    {
        private readonly System.Timers.Timer _timer;
        private bool _isInitialized = false;

        // 事件：当详细快照更新时触发
        public event EventHandler<List<ProcessDetailInfo>>? ProcessListUpdated;
        public event EventHandler<SystemPerformanceSnapshot>? GlobalSnapshotUpdated;

        public SystemMonitorServiceEx(double intervalMs = 2000)
        {
            _isInitialized = SystemMethods.InitializeSystemMethods();

            if (!_isInitialized)
            {
                LoggerService.Log(LogLevel.Error, "Failed to initialize SystemMonitorEx");
            }

            _timer = new System.Timers.Timer(intervalMs);
            _timer.Elapsed += OnTimerElapsed;
            _timer.AutoReset = true;
        }

        public void Start()
        {
            if (_isInitialized)
            {
                _timer.Start();
            }
        }
        public void Stop() => _timer.Stop();

        private void OnTimerElapsed(object? sender, ElapsedEventArgs e)
        {
            if (!_isInitialized) return;

            try
            {
                // 1. 获取全局性能快照
                var globalSnapshot = SystemMethods.GetSystemPerformanceSnapshot();
                GlobalSnapshotUpdated?.Invoke(this, globalSnapshot);

                // 2. 获取深度进程列表 (预留 2000 个缓冲区)
                var buffer = new ProcessDetailInfo[2048];
                int actualCount = SystemMethods.GetDetailedProcessList(buffer, buffer.Length);

                if (actualCount > 0)
                {
                    var resultList = buffer.Take(actualCount).ToList();
                    ProcessListUpdated?.Invoke(this, resultList);
                }
            }
            catch (Exception ex)
            {
                LoggerService.Log(LogLevel.Error, $"SystemMonitorServiceEx 运行异常: {ex.Message}");
            }
        }

        public void Dispose()
        {
            Stop();
            _timer?.Dispose();
            if (_isInitialized)
            {
                SystemMethods.CleanupSystemMethods();
                _isInitialized = false;
            }
        }
    }
}