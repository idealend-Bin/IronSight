using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IronSight.Interop.Native.Memory
{
    public static class MemoryManager
    {
        public static List<ProcessInfo> GetRankedProcesses(int limit = 10)
        {
            var buffer = new ProcessInfo[limit];
            int count = MemoryMethods.GetTopMemoryConsumers(buffer, limit);

            // 转换为 List 方便 WPF 数据绑定，同时过滤掉那些占用几乎为 0 的僵尸进程
            return buffer.Take(count)
                         .Where(p => p.WorkingSetMB > 0.1)
                         .ToList();
        }
    }
}
