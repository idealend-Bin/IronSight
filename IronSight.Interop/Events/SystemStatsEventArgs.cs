using System;

namespace IronSight.Interop.Events
{
    public class SystemStatsEventArgs : EventArgs
    {
        public double CpuUsage { get; }
        public double DiskReadRate { get; }
        public double DiskWriteRate { get; }

        public SystemStatsEventArgs(double cpu, double diskRead, double diskWrite)
        {
            CpuUsage = cpu;
            DiskReadRate = diskRead;
            DiskWriteRate = diskWrite;
        }
    }
}