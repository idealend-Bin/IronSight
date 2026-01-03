#pragma once
namespace IronSight::Core::Native::System
{
	class SystemMonitor
	{
		private:
		inline static PDH_HQUERY _hQuery = nullptr;
		inline static PDH_HCOUNTER _hCpuCounter = nullptr;
		inline static PDH_HCOUNTER _hDiskReadCounter = nullptr;
		inline static PDH_HCOUNTER _hDiskWriteCounter = nullptr;

		public:
		/// <summary>
		/// 初始化系统监视器并准备其运行环境。
		/// </summary>
		/// <returns>初始化成功时返回 true，失败时返回 false。</returns>
		static bool InitializeSystemMonitor();
		/// <summary>
		/// 更新系统统计信息。
		/// </summary>
		static void UpdateSystemStats();
		/// <summary>
		/// 获取当前 CPU 使用率。
		/// </summary>
		/// <returns>返回表示当前 CPU 使用率的 double 值，通常以百分比表示（例如 0.0 到 100.0 之间）。</returns>
		static double GetCpuUsage();
		/// <summary>
		/// 获取当前磁盘读取速率。
		/// </summary>
		/// <returns>表示磁盘读取速率的 double 值，具体单位由实现决定。</returns>
		static double GetDiskReadRate();
		/// <summary>
		/// 获取当前磁盘写入速率。
		/// </summary>
		/// <returns>表示磁盘写入速率的 double 值。</returns>
		static double GetDiskWriteRate();
		/// <summary>
		/// 清理系统监视器并释放其占用的资源。
		/// </summary>
		static void CleanupSystemMonitor();
	};
}


extern "C"
{
	__declspec(dllexport) bool InitializeSystemMonitor() { return IronSight::Core::Native::System::SystemMonitor::InitializeSystemMonitor(); }
	__declspec(dllexport) void UpdateSystemStats() { return IronSight::Core::Native::System::SystemMonitor::UpdateSystemStats(); }
	__declspec(dllexport) double GetCpuUsage() { return IronSight::Core::Native::System::SystemMonitor::GetCpuUsage(); };
	__declspec(dllexport) double GetDiskReadRate() { return IronSight::Core::Native::System::SystemMonitor::GetDiskReadRate(); } // Bytes/sec
	__declspec(dllexport) double GetDiskWriteRate() { return IronSight::Core::Native::System::SystemMonitor::GetDiskWriteRate(); }; // Bytes/sec
	__declspec(dllexport) void CleanupSystemMonitor() { return IronSight::Core::Native::System::SystemMonitor::CleanupSystemMonitor(); };
}

