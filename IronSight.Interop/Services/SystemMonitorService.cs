using System;
using System.Timers;
using IronSight.Interop.Native.System;
using IronSight.Interop.Events;
using IronSight.Interop.Core;

namespace IronSight.Interop.Services
{
    public class SystemMonitorService : IDisposable
    {
        private System.Timers.Timer _timer;
        private bool _isInitialized;

        public event EventHandler<SystemStatsEventArgs> StatsUpdated;

        public SystemMonitorService()
        {
            _isInitialized = SystemMethods.InitializeSystemMonitor();
            if (!_isInitialized)
            {
                // Handle error or log
                LoggerService.Log(LogLevel.Error, "Failed to initialize SystemMonitor");
            }

            _timer = new System.Timers.Timer(1000); // 1 second update interval
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

        public void Stop()
        {
            _timer.Stop();
        }

        private void OnTimerElapsed(object sender, ElapsedEventArgs e)
        {
            if (!_isInitialized) return;

            try
            {
                SystemMethods.UpdateSystemStats();
                double cpu = SystemMethods.GetCpuUsage();
                double diskRead = SystemMethods.GetDiskReadRate();
                double diskWrite = SystemMethods.GetDiskWriteRate();

                StatsUpdated?.Invoke(this, new SystemStatsEventArgs(cpu, diskRead, diskWrite));
            }
            catch (Exception ex)
            {
                LoggerService.Log(LogLevel.Error, $"Error updating system stats: {ex.Message}");
            }
        }

        public void Dispose()
        {
            Stop();
            _timer.Dispose();
            if (_isInitialized)
            {
                SystemMethods.CleanupSystemMonitor();
                _isInitialized = false;
            }
        }
    }
}