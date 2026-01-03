using System;
using System.Runtime.InteropServices;
using System.Text;

namespace IronSight.Interop.Native.System
{
    public struct SystemPerformanceSnapshot
    {
        public double CpuUsage;                // 总 CPU 使用率 (%)
        public double CpuTemperature;          // CPU 温度 (Celsius) - 注意：某些硬件可能返回 0
        public double MemoryUsagePercent;      // 内存占用率 (%)
        public double TotalPhysicalMemoryMB;   // 总物理内存 (MB)
        public double AvailablePhysicalMemoryMB; // 可用物理内存 (MB)
        public uint ProcessCount;          // 当前运行进程数
        public uint ThreadCount;           // 当前总线程数
        public uint HandleCount;           // 系统总句柄数
        public double CommittedBytesMB;        // 已提交页面内存 (MB)
    }

    /**
     * 结构体: ProcessDetailInfo
     * 修正说明: 
     * 1. 必须使用显式字段 (Fields) 以确保 [StructLayout] 能够精准控制内存对齐。
     * 2. WPF 无法直接绑定字段，因此我们手动编写属性 (Properties) 包装这些字段。
     * 3. 这样既满足了 C++ 的 8 字节对齐要求，也解决了 WPF Error 40 绑定错误。
     */
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 8)]
    public struct ProcessDetailInfo
    {
        private uint _pid;
        private double _memoryMB;
        private double _cpuUsage;        // CPU 占用
        private double _diskReadRateMS;  // 磁盘读取 MB/s
        private double _diskWriteRateMS; // 磁盘写入 MB/s
        private uint _threadCount;
        private uint _handleCount;
        private int _priorityClass;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        private string _name;

        public uint Pid { get => _pid; set => _pid = value; }
        public double MemoryMB { get => _memoryMB; set => _memoryMB = value; }
        public double CpuUsage { get => _cpuUsage; set => _cpuUsage = value; }
        public double DiskReadRateMS { get => _diskReadRateMS; set => _diskReadRateMS = value; }
        public double DiskWriteRateMS { get => _diskWriteRateMS; set => _diskWriteRateMS = value; }
        public uint ThreadCount { get => _threadCount; set => _threadCount = value; }
        public uint HandleCount { get => _handleCount; set => _handleCount = value; }
        public int PriorityClass { get => _priorityClass; set => _priorityClass = value; }
        public string Name { get => _name; set => _name = value; }

        // 辅助判断数据是否发生显著变化，用于差量更新性能优化
        public bool IsVisuallyDifferent(ProcessDetailInfo other) =>
            Math.Abs(CpuUsage - other.CpuUsage) > 0.1 ||
            Math.Abs(MemoryMB - other.MemoryMB) > 0.5 ||
            Math.Abs(DiskReadRateMS - other.DiskReadRateMS) > 0.01;
    }

    public static class SystemMethods
    {
        [DllImport("IronSight.Core.Native.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool InitializeSystemMonitor();

        [DllImport("IronSight.Core.Native.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void UpdateSystemStats();

        [DllImport("IronSight.Core.Native.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern double GetCpuUsage();

        [DllImport("IronSight.Core.Native.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern double GetDiskReadRate();

        [DllImport("IronSight.Core.Native.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern double GetDiskWriteRate();

        [DllImport("IronSight.Core.Native.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void CleanupSystemMonitor();

        [DllImport("IronSight.Core.Native.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool InitializeSystemMethods();

        [DllImport("IronSight.Core.Native.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern SystemPerformanceSnapshot GetSystemPerformanceSnapshot();

        [DllImport("IronSight.Core.Native.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void CleanupSystemMethods();

        [DllImport("IronSight.Core.Native.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int GetDetailedProcessList([In, Out, MarshalAs(UnmanagedType.LPArray)] ProcessDetailInfo[] buffer, int maxCount);

        [DllImport("IronSight.Core.Native.dll", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool TerminateSelectedProcess(uint pid);

        [DllImport("IronSight.Core.Native.dll", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetProcessFullPath(uint pid, [MarshalAs(UnmanagedType.LPStr)] StringBuilder pathBuffer, uint bufferSize);

        [DllImport("IronSight.Core.Native.dll", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetProcessPriority(uint pid, uint priorityClass);

        [DllImport("IronSight.Core.Native.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void ShowFileProperties([MarshalAs(UnmanagedType.LPStr)] string filePath);
    }
}