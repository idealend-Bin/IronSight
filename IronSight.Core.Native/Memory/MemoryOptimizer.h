#pragma once
namespace IronSight::Core::Native::Memory 
{
    struct CleanupResult 
    {
        DWORD ProcessedProcesses;
        long long TotalBytesReleased;
    };

#pragma pack(push, 8) 
    struct ProcessInfo
    {
        uint32_t Pid;
        double WorkingSetMB;
        char Name[260];
    };
#pragma pack(pop)

    class MemoryOptimizer 
    {
        public:
        /// <summary>
        /// 返回按内存使用量排序的前 N 个内存占用最多的进程信息（静态函数）。
        /// </summary>
        /// <param name="topN">要返回的进程数量（前 N 个）。</param>
        /// <returns>包含 ProcessInfo 对象的 std::vector，每个对象表示一个进程的信息。向量按内存使用量降序排列，包含最多 topN 个条目。</returns>
        static std::vector<ProcessInfo> GetTopMemoryConsumers(int topN);
        /// <summary>
        /// 执行全局内存清理的函数。
        /// </summary>
        /// <returns>清理的进程以及释放的内存结果。</returns>
        static CleanupResult ExecuteGlobalCleanup();
    };

    extern "C" 
    {
        // C# 最终调用的平铺接口
        __declspec(dllexport) CleanupResult CleanSystemMemory();
        __declspec(dllexport) int GetTopMemoryConsumers(ProcessInfo buffer[], int maxCount);
    }
}