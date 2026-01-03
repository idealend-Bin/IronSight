#include <pch.h>
#include "SystemMonitor.h"
#include "Utilities.h"

#pragma comment(lib, "pdh.lib")

namespace IronSight::Core::Native::System
{
	bool SystemMonitor::InitializeSystemMonitor()
	{
		PDH_STATUS status = PdhOpenQuery(NULL, 0, &_hQuery);
		if (status != ERROR_SUCCESS)
		{
			LOG_ERROR("PdhOpenQuery failed: 0x%08x", status);
			return false;
		}

		// Use PdhAddEnglishCounter to ensure it works on non-English systems (Vista+)

		// CPU
		status = PdhAddEnglishCounter(_hQuery, L"\\Processor(_Total)\\% Processor Time", 0, &_hCpuCounter);
		if (status != ERROR_SUCCESS)
		{
			LOG_ERROR("Failed to add CPU counter: 0x%08x", status);
			CleanupSystemMonitor();
			return false;
		}

		// Disk Read
		status = PdhAddEnglishCounter(_hQuery, L"\\PhysicalDisk(_Total)\\Disk Read Bytes/sec", 0, &_hDiskReadCounter);
		if (status != ERROR_SUCCESS)
		{
			LOG_WARN("Failed to add Disk Read counter: 0x%08x. Disk stats may be unavailable.", status);
		}

		// Disk Write
		status = PdhAddEnglishCounter(_hQuery, L"\\PhysicalDisk(_Total)\\Disk Write Bytes/sec", 0, &_hDiskWriteCounter);
		if (status != ERROR_SUCCESS)
		{
			LOG_WARN("Failed to add Disk Write counter: 0x%08x. Disk stats may be unavailable.", status);
		}

		// Collect initial data
		PdhCollectQueryData(_hQuery);
		return true;
	}

	void SystemMonitor::UpdateSystemStats()
	{
		if (_hQuery)
		{
			PDH_STATUS status = PdhCollectQueryData(_hQuery);
			if (status != ERROR_SUCCESS)
			{
				// Log verbose only if needed, to avoid spam
				LOG_ERROR("PdhCollectQueryData failed: 0x%08x", status);
			}
		}
	}

	double SystemMonitor::GetCpuUsage()
	{
		if (!_hQuery || !_hCpuCounter) return 0.0;

		PDH_FMT_COUNTERVALUE value;
		PDH_STATUS status = PdhGetFormattedCounterValue(_hCpuCounter, PDH_FMT_DOUBLE, NULL, &value);
		if (status == ERROR_SUCCESS)
		{
			return value.doubleValue;
		}
		return 0.0;
	}

	double SystemMonitor::GetDiskReadRate()
	{
		if (!_hQuery || !_hDiskReadCounter) return 0.0;

		PDH_FMT_COUNTERVALUE value;
		PDH_STATUS status = PdhGetFormattedCounterValue(_hDiskReadCounter, PDH_FMT_DOUBLE, NULL, &value);
		if (status == ERROR_SUCCESS)
		{
			return value.doubleValue;
		}
		return 0.0;
	}

	double SystemMonitor::GetDiskWriteRate()
	{
		if (!_hQuery || !_hDiskWriteCounter) return 0.0;

		PDH_FMT_COUNTERVALUE value;
		PDH_STATUS status = PdhGetFormattedCounterValue(_hDiskWriteCounter, PDH_FMT_DOUBLE, NULL, &value);
		if (status == ERROR_SUCCESS)
		{
			return value.doubleValue;
		}
		return 0.0;
	}

	void SystemMonitor::CleanupSystemMonitor()
	{
		if (_hQuery)
		{
			PdhCloseQuery(_hQuery);
			_hQuery = NULL;
		}
	}
}
