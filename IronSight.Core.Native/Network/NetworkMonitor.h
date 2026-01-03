#pragma once
#include <mutex>


namespace IronSight::Core::Native::Network
{
	/// <summary>
	/// TCP连接状态枚举
	/// </summary>
	enum class ConnectionState : int
	{
		Unknown = 0,
		Closed = 1,
		Listen = 2,
		SynSent = 3,
		SynReceived = 4,
		Established = 5,
		FinWait1 = 6,
		FinWait2 = 7,
		CloseWait = 8,
		Closing = 9,
		LastAck = 10,
		TimeWait = 11,
		DeleteTcb = 12
	};



	/// <summary>
	/// 网络协议类型枚举
	/// </summary>
	enum class ProtocolType : int
	{
		Unknown = 0,
		Tcp = 1,
		Udp = 2
	};

	/// <summary>
	/// 网络连接信息结构体 - 用于跨边界传输
	/// </summary>
#pragma pack(push, 1)
	struct NetworkConnectionInfo
	{
		uint32_t LocalAddress;      // 本地IP地址 (网络字节序)
		uint32_t RemoteAddress;     // 远程IP地址 (网络字节序)
		uint16_t LocalPort;         // 本地端口
		uint16_t RemotePort;        // 远程端口
		ConnectionState State;       // 连接状态
		ProtocolType Protocol;       // 协议类型
		uint32_t ProcessId;         // 进程ID
		uint64_t Reserved;          // 保留字段，用于扩展
	};
#pragma pack(pop)

	static_assert(sizeof(NetworkConnectionInfo) == 32,
		"NetworkConnectionInfo size mismatch");


	/// <summary>
	/// 高性能网络监控器类
	/// </summary>
	class NetworkMonitor
	{
		public:
		NetworkMonitor();
		~NetworkMonitor();

		// 禁止拷贝
		NetworkMonitor(const NetworkMonitor&) = delete;
		NetworkMonitor& operator=(const NetworkMonitor&) = delete;

		/// <summary>
		/// 刷新所有网络连接信息
		/// </summary>
		/// <returns>成功返回true</returns>
		bool Refresh();

		/// <summary>
		/// 仅刷新TCP连接
		/// </summary>
		bool RefreshTcp();

		/// <summary>
		/// 仅刷新UDP连接
		/// </summary>
		bool RefreshUdp();

		/// <summary>
		/// 获取连接数量
		/// </summary>
		size_t GetConnectionCount() const noexcept;

		/// <summary>
		/// 获取连接数据指针
		/// </summary>
		const NetworkConnectionInfo* GetConnections() const noexcept;

		/// <summary>
		/// 复制连接到外部缓冲区
		/// </summary>
		/// <param name="buffer">目标缓冲区</param>
		/// <param name="bufferSize">缓冲区大小(元素数量)</param>
		/// <returns>实际复制的数量</returns>
		size_t CopyConnectionsTo(NetworkConnectionInfo* buffer,
			size_t bufferSize) const;

		private:
		bool RefreshTcpConnectionsInternal();
		bool RefreshUdpConnectionsInternal();

		// 预分配的缓冲区，避免频繁内存分配
		std::vector<NetworkConnectionInfo> _connections;
		std::vector<uint8_t> _tcpTableBuffer;
		std::vector<uint8_t> _udpTableBuffer;

		mutable std::mutex _mutex;

		static constexpr size_t InitialBufferSize = 65536;
		static constexpr size_t InitialConnectionCapacity = 1024;
	};

}