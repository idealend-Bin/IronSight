#pragma once
#include <map>

namespace IronSight::Core::Native::System
{
	/**
	* 结构体: SystemPerformanceSnapshot
	* 功能: 存储系统级的性能深度快照数据
	* 规范: PascalCase (与 C# 保持同步)
	*/
#pragma pack(push, 8)
	struct SystemPerformanceSnapshot
	{
		double CpuUsage;                // 总 CPU 使用率 (%)
		double CpuTemperature;          // CPU 温度 (Celsius) - 注意：某些硬件可能返回 0
		double MemoryUsagePercent;      // 内存占用率 (%)
		double TotalPhysicalMemoryMB;   // 总物理内存 (MB)
		double AvailablePhysicalMemoryMB; // 可用物理内存 (MB)
		uint32_t ProcessCount;          // 当前运行进程数
		uint32_t ThreadCount;           // 当前总线程数
		uint32_t HandleCount;           // 系统总句柄数
		double CommittedBytesMB;        // 已提交页面内存 (MB)
	};

	/**
	* 结构体: ProcessDetailInfo
	* 功能: 单个进程的详细深度信息数据
	* 规范: 严格遵循 PascalCase 以映射 C# 结构
	*/
	struct ProcessDetailInfo
	{
		uint32_t Pid;
		double MemoryMB;
		double CpuUsage;        // CPU 占用
		double DiskReadRateMS;  // 磁盘读取 MB/s
		double DiskWriteRateMS; // 磁盘写入 MB/s
		uint32_t ThreadCount;
		uint32_t HandleCount;
		int PriorityClass;
		char Name[260];         // 进程名称 (ANSI)
	};
#pragma pack(pop)
	
	struct ProcessHistory
	{
		ULARGE_INTEGER LastKernelTime;
		ULARGE_INTEGER LastUserTime;
		ULARGE_INTEGER LastSystemTime;
		IO_COUNTERS LastIo;
		ULONGLONG LastSampleTick;
	};
	/**
	* 类名: SystemMethods
	* 功能: 负责 System 命名空间下的所有底层监控逻辑
	*/
	class SystemMethods
	{
		private:
		inline static PDH_HQUERY _pdhQuery = nullptr;
		inline static PDH_HCOUNTER _cpuCounter = nullptr;
		inline static bool _isPdhInitialized = false;
		inline static std::map<uint32_t, ProcessHistory> _historyMap;

		public:
		static bool Initialize();
		static SystemPerformanceSnapshot GetPerformanceSnapshot();
		static void Cleanup();
		static int GetDetailedProcessList(ProcessDetailInfo* buffer, int maxCount);
	};

	extern "C"
	{
		__declspec(dllexport) bool InitializeSystemMethods()
		{
			return SystemMethods::Initialize();
		}

		__declspec(dllexport) SystemPerformanceSnapshot GetSystemPerformanceSnapshot()
		{
			return SystemMethods::GetPerformanceSnapshot();
		}

		__declspec(dllexport) void CleanupSystemMethods()
		{
			SystemMethods::Cleanup();
		}

		/**
		* 功能: 获取详细进程列表 (由 SystemMonitorServiceEx 调用)
		* 解决 Unicode 不兼容问题: 显式使用 W 系列 API 并进行多字节转换
		*/
		__declspec(dllexport) int GetDetailedProcessList(ProcessDetailInfo* buffer, int maxCount) { return SystemMethods::GetDetailedProcessList(buffer, maxCount); }

		__declspec(dllexport) bool GetProcessFullPath(uint32_t pid, char* pathBuffer, uint32_t bufferSize);

		__declspec(dllexport) bool SetProcessPriority(uint32_t pid, uint32_t priorityClass);

		__declspec(dllexport) void ShowFileProperties(const char* filePath);

		__declspec(dllexport) bool TerminateSelectedProcess(DWORD pid);

	}
}



