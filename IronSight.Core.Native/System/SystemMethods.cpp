#include <pch.h>
#include "SystemMethods.h"
#include "Utilities.h"
#include <shellapi.h>
#include <unordered_set>

namespace IronSight::Core::Native::System
{
	bool SystemMethods::Initialize()
	{
		if (_isPdhInitialized) return true;

		if (PdhOpenQuery(NULL, NULL, &_pdhQuery) != ERROR_SUCCESS) return false;

		// 添加总 CPU 使用率计数器
		PdhAddCounter(_pdhQuery, L"\\Processor(_Total)\\% Processor Time", NULL, &_cpuCounter);

		PdhCollectQueryData(_pdhQuery);
		_isPdhInitialized = true;
		LOG_INFO("SystemMethods: 底层监控引擎初始化成功");
		return true;
	}

	SystemPerformanceSnapshot SystemMethods::GetPerformanceSnapshot()
	{
		SystemPerformanceSnapshot snapshot = { 0 };

		// 1. 获取 CPU 使用率
		PDH_FMT_COUNTERVALUE counterValue;
		PdhCollectQueryData(_pdhQuery);
		PdhGetFormattedCounterValue(_cpuCounter, PDH_FMT_DOUBLE, NULL, &counterValue);
		snapshot.CpuUsage = counterValue.doubleValue;

		// 2. 获取内存信息
		MEMORYSTATUSEX memStatus;
		memStatus.dwLength = sizeof(MEMORYSTATUSEX);
		if (GlobalMemoryStatusEx(&memStatus))
		{
			snapshot.MemoryUsagePercent = (double)memStatus.dwMemoryLoad;
			snapshot.TotalPhysicalMemoryMB = memStatus.ullTotalPhys / (1024.0 * 1024.0);
			snapshot.AvailablePhysicalMemoryMB = memStatus.ullAvailPhys / (1024.0 * 1024.0);
			snapshot.CommittedBytesMB = memStatus.ullTotalPageFile / (1024.0 * 1024.0);
		}

		// 3. 获取进程、线程与句柄总数
		// 使用 GetProcessReferenceCount 风格的逻辑，但为了稳健性使用更通用的 API
		std::vector<DWORD> processIds = std::vector<DWORD>(4096);
		DWORD cbNeeded;
		if (EnumProcesses(processIds.data(), sizeof(processIds), &cbNeeded))
		{
			snapshot.ProcessCount = cbNeeded / sizeof(DWORD);
		}

		// 获取系统总句柄和线程数 (简略逻辑，通常需通过 NtQuerySystemInformation 获取全局精准值)
		// 此处展示基础实现，后续可升级至 NtQuery 提高精度
		snapshot.HandleCount = 0; // 预留
		snapshot.ThreadCount = 0; // 预留

		// 4. 获取 CPU 温度 (通过 WMI 或特定驱动获取更为准确，原生 API 限制较多)
		// 这是一个示意占位，真实环境通常需要从 ThermalZone 对象读取
		snapshot.CpuTemperature = 45.5;

		LOG_DEBUG("SystemMethods: 快照获取完成 - CPU: %.1f%%, RAM: %.1f%%",
			snapshot.CpuUsage, snapshot.MemoryUsagePercent);

		return snapshot;
	}

	void SystemMethods::Cleanup()
	{
		if (_isPdhInitialized)
		{
			PdhCloseQuery(_pdhQuery);
			_isPdhInitialized = false;
		}

	}

	int SystemMethods::GetDetailedProcessList(ProcessDetailInfo* buffer, int maxCount)
	{
		if (!buffer || maxCount <= 0) return 0;

		HANDLE hSnapshot = CreateToolhelp32Snapshot(TH32CS_SNAPPROCESS, 0);
		if (hSnapshot == INVALID_HANDLE_VALUE) return 0;

		PROCESSENTRY32W pe32;
		pe32.dwSize = sizeof(PROCESSENTRY32W);

		if (!Process32FirstW(hSnapshot, &pe32))
		{
			CloseHandle(hSnapshot);
			return 0;
		}

		ULONGLONG currentTick = GetTickCount64();
		FILETIME sysIdle, sysKernel, sysUser;
		GetSystemTimes(&sysIdle, &sysKernel, &sysUser);

		ULARGE_INTEGER sTime;
		sTime.LowPart = sysKernel.dwLowDateTime; sTime.HighPart = sysKernel.dwHighDateTime;

		int count = 0;
		std::vector<uint32_t> activePids;

		do
		{
			if (count >= maxCount) break;

			ProcessDetailInfo& info = buffer[count];
			info.Pid = pe32.th32ProcessID;
			info.ThreadCount = pe32.cntThreads;
			activePids.push_back(info.Pid);

			// 字符转换：Unicode -> ANSI
			WideCharToMultiByte(CP_ACP, 0, pe32.szExeFile, -1, info.Name, sizeof(info.Name), NULL, NULL);

			// 核心优化：一次 OpenProcess 获取所有权限
			// 需要 PROCESS_QUERY_INFORMATION 用于 Times/IO，PROCESS_VM_READ 用于 MemoryMB
			HANDLE hProcess = OpenProcess(PROCESS_QUERY_INFORMATION | PROCESS_VM_READ, FALSE, info.Pid);

			if (hProcess)
			{
				// 1. 获取内存与句柄数
				PROCESS_MEMORY_COUNTERS_EX pmc;
				if (GetProcessMemoryInfo(hProcess, (PROCESS_MEMORY_COUNTERS*)&pmc, sizeof(pmc)))
				{
					info.MemoryMB = pmc.PrivateUsage / (1024.0 * 1024.0);
				}

				DWORD handleCount = 0;
				if (GetProcessHandleCount(hProcess, &handleCount))
				{
					info.HandleCount = handleCount;
				}

				// 2. 速率采样计算 (CPU & Disk)
				FILETIME createTime, exitTime, kernelTime, userTime;
				IO_COUNTERS ioCounters;

				if (GetProcessTimes(hProcess, &createTime, &exitTime, &kernelTime, &userTime) &&
					GetProcessIoCounters(hProcess, &ioCounters))
				{
					auto& hist = _historyMap[info.Pid];

					ULARGE_INTEGER kTime, uTime;
					kTime.LowPart = kernelTime.dwLowDateTime; kTime.HighPart = kernelTime.dwHighDateTime;
					uTime.LowPart = userTime.dwLowDateTime; uTime.HighPart = userTime.dwHighDateTime;

					if (hist.LastSampleTick > 0)
					{
						// CPU 计算
						ULONGLONG procDiff = (kTime.QuadPart - hist.LastKernelTime.QuadPart) +
							(uTime.QuadPart - hist.LastUserTime.QuadPart);
						ULONGLONG sysDiff = (sTime.QuadPart - hist.LastSystemTime.QuadPart);
						if (sysDiff > 0) info.CpuUsage = (static_cast<double>(procDiff) / sysDiff) * 100.0;

						// 磁盘速率计算
						double timeSec = (currentTick - hist.LastSampleTick) / 1000.0;
						if (timeSec > 0)
						{
							ULONGLONG readDiff = ioCounters.ReadTransferCount - hist.LastIo.ReadTransferCount;
							ULONGLONG writeDiff = ioCounters.WriteTransferCount - hist.LastIo.WriteTransferCount;
							info.DiskReadRateMS = (readDiff / (1024.0 * 1024.0)) / timeSec;
							info.DiskWriteRateMS = (writeDiff / (1024.0 * 1024.0)) / timeSec;
						}
					}

					// 更新历史缓存
					hist.LastKernelTime = kTime;
					hist.LastUserTime = uTime;
					hist.LastSystemTime = sTime;
					hist.LastIo = ioCounters;
					hist.LastSampleTick = currentTick;
				}
				CloseHandle(hProcess);
			}
			else
			{
				// 权限不足的进程填充 0
				info.MemoryMB = 0;
				info.HandleCount = 0;
				info.CpuUsage = 0;
			}

			count++;
		}
		while (Process32NextW(hSnapshot, &pe32));

		CloseHandle(hSnapshot);

		// 内存管理：清理已经退出的 PID 缓存，防止 Map 无限膨胀
		if (_historyMap.size() > (size_t)count + 50)
		{
			std::unordered_set<uint32_t> currentSet(activePids.begin(), activePids.end());
			for (auto it = _historyMap.begin(); it != _historyMap.end(); )
			{
				if (currentSet.find(it->first) == currentSet.end()) it = _historyMap.erase(it);
				else ++it;
			}
		}

		return count;
	}

	// 获取进程完整路径
	bool GetProcessFullPath(uint32_t pid, char* pathBuffer, uint32_t bufferSize)
	{
		HANDLE hProcess = OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION, FALSE, pid);
		if (!hProcess) return false;

		DWORD size = bufferSize;
		bool success = QueryFullProcessImageNameA(hProcess, 0, pathBuffer, &size);
		CloseHandle(hProcess);
		return success;
	}

	// 设置进程优先级
	bool SetProcessPriority(uint32_t pid, uint32_t priorityClass)
	{
		HANDLE hProcess = OpenProcess(PROCESS_SET_INFORMATION, FALSE, pid);
		if (!hProcess) return false;

		bool success = SetPriorityClass(hProcess, priorityClass);
		CloseHandle(hProcess);
		return success;
	}

	// 弹出文件属性窗口
	void ShowFileProperties(const char* filePath)
	{
		SHELLEXECUTEINFOA sei = { sizeof(sei) };
		sei.fMask = SEE_MASK_INVOKEIDLIST;
		sei.lpVerb = "properties";
		sei.lpFile = filePath;
		sei.nShow = SW_SHOW;
		ShellExecuteExA(&sei);
	}

	// 假设这是在 SystemMethods 类中
	bool TerminateSelectedProcess(DWORD pid)
	{
		// 1. 尝试获取进程句柄
		// PROCESS_TERMINATE: 仅需要结束进程的权限
		HANDLE hProcess = OpenProcess(PROCESS_TERMINATE, FALSE, pid);

		// 如果打开失败（例如进程不存在、权限不足），直接返回 false
		if (hProcess == NULL)
		{
			return false;
		}

		// 2. 执行终止
		// 这里的退出码通常使用 1 表示异常终止，或者你可以使用其他自定义码
		BOOL isSuccess = TerminateProcess(hProcess, 0xFFFFFFFF);

		// 3. 【关键】无论终止成功与否，必须关闭句柄以释放系统资源
		CloseHandle(hProcess);

		return (isSuccess == TRUE);
	}


}