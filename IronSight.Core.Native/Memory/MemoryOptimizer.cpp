#include <pch.h>
#include "MemoryOptimizer.h"
#include "Utilities.h"
#include <algorithm>

namespace IronSight::Core::Native::Memory
{
	CleanupResult MemoryOptimizer::ExecuteGlobalCleanup()
	{
		LOG_INFO("--- 开始全局内存优化任务 ---");
		CleanupResult result = { 0, 0 };
		DWORD processes[2048], cbNeeded;

		if (!EnumProcesses(processes, sizeof(processes), &cbNeeded))
		{
			LOG_ERROR("无法获取进程列表，错误代码: %lu", GetLastError());
			return result;
		}

		DWORD count = cbNeeded / sizeof(DWORD);
		for (DWORD i = 0; i < count; i++)
		{
			if (processes[i] == 0) continue;

			HANDLE hProcess = OpenProcess(PROCESS_QUERY_INFORMATION | PROCESS_SET_QUOTA, FALSE, processes[i]);
			if (hProcess)
			{
				PROCESS_MEMORY_COUNTERS pmc;
				if (GetProcessMemoryInfo(hProcess, &pmc, sizeof(pmc)))
				{
					SIZE_T before = pmc.WorkingSetSize;

					if (EmptyWorkingSet(hProcess))
					{
						if (GetProcessMemoryInfo(hProcess, &pmc, sizeof(pmc)))
						{
							SIZE_T released = (before > pmc.WorkingSetSize) ? (before - pmc.WorkingSetSize) : 0;

							// 策略：只有释放超过 1MB 的进程才记录 DEBUG 详情，避免刷屏
							if (released > 1024 * 1024)
							{
								LOG_DEBUG("大幅优化进程 PID: %u, 释放: %.2f MB", processes[i], released / (1024.0 * 1024.0));
							}

							result.TotalBytesReleased += released;
							result.ProcessedProcesses++;
						}
					}
				}
				CloseHandle(hProcess);
			}
		} // 循环在这里结束

		  // --- 修复：移出循环外，只在真正完成后打印一次 ---
		LOG_INFO("优化完成。处理进程: %u, 总释放: %.2f MB",
			result.ProcessedProcesses,
			result.TotalBytesReleased / (1024.0 * 1024.0));

		return result;
	}

	std::vector<ProcessInfo> MemoryOptimizer::GetTopMemoryConsumers(int topN)
	{
		std::vector<ProcessInfo> v;
		DWORD processes[1024], cbNeeded;

		if (!EnumProcesses(processes, sizeof(processes), &cbNeeded)) return v;

		DWORD count = cbNeeded / sizeof(DWORD);
		for (DWORD i = 0; i < count; i++)
		{
			if (processes[i] == 0) continue;

			HANDLE hProcess = OpenProcess(PROCESS_QUERY_INFORMATION | PROCESS_VM_READ, FALSE, processes[i]);
			if (hProcess)
			{
				ProcessInfo info;
				info.Pid = processes[i];

				PROCESS_MEMORY_COUNTERS pmc;
				if (GetProcessMemoryInfo(hProcess, &pmc, sizeof(pmc)))
				{
					info.WorkingSetMB = pmc.WorkingSetSize / (1024.0 * 1024.0);

					// 获取进程名
					if (GetModuleBaseNameA(hProcess, NULL, info.Name, sizeof(info.Name)) == 0)
					{
						strcpy_s(info.Name, "Unknown");
					}
					v.push_back(info);
				}
				CloseHandle(hProcess);
			}
		}

		// 排序：从大到小
		std::sort(v.begin(), v.end(), [](const ProcessInfo& a, const ProcessInfo& b)
			{
				return a.WorkingSetMB > b.WorkingSetMB;
			});

		// 只保留前 topN 个
		if (v.size() > (size_t)topN) v.resize(topN);
		return v;
	}

	extern "C"
	{
		__declspec(dllexport) CleanupResult CleanSystemMemory()
		{
			// 转发调用给 OOP 实现
			return MemoryOptimizer::ExecuteGlobalCleanup();
		}

		int GetTopMemoryConsumers(ProcessInfo buffer[], int maxCount)
		{
			if (!buffer || maxCount <= 0) return 0;

			auto consumers = IronSight::Core::Native::Memory::MemoryOptimizer::GetTopMemoryConsumers(maxCount);

			int actualCount = static_cast<int>(consumers.size());
			for (int i = 0; i < actualCount; ++i)
			{
				buffer[i] = consumers[i]; // 结构体拷贝
			}

			LOG_TRACE("原生层：已填充 %d 个进程数据到缓冲区", actualCount);
			return actualCount;
		}
	}
}