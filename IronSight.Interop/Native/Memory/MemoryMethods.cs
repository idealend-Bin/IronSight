using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace IronSight.Interop.Native.Memory
{
    [StructLayout(LayoutKind.Sequential)]
    public struct CleanupResult
    {
        public uint ProcessedProcesses;
        public long TotalBytesReleased;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct ProcessInfo
    {
        public uint Pid;
        public double WorkingSetMB;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string Name;
    }

    public static class MemoryMethods
    {
        private const string DllName = "IronSight.Core.Native.dll";

        // 现有的内存压缩函数
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool EmptyWorkingSet(IntPtr hProcess);

        // 新增的排行获取函数
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int GetTopMemoryConsumers([In, Out] ProcessInfo[] buffer, int maxCount);

        // 还可以加上我们之前写的全量清理
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern CleanupResult CleanSystemMemory();
    }
}
