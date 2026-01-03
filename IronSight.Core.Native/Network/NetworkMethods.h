#pragma once

namespace IronSight::Core::Native::Network
{
	extern "C"
	{
		__declspec(dllexport) NetworkMonitor* NetworkMonitor_Create();

		__declspec(dllexport) void NetworkMonitor_Destroy(NetworkMonitor* monitor);

		__declspec(dllexport) bool NetworkMonitor_Refresh(NetworkMonitor* monitor);

		__declspec(dllexport) bool NetworkMonitor_RefreshTcp(NetworkMonitor* monitor);

		__declspec(dllexport) bool NetworkMonitor_RefreshUdp(NetworkMonitor* monitor);

		__declspec(dllexport) size_t NetworkMonitor_GetConnectionCount(NetworkMonitor* monitor);

		__declspec(dllexport) size_t NetworkMonitor_CopyConnections(NetworkMonitor* monitor, NetworkConnectionInfo* buffer, size_t bufferSize);

		__declspec(dllexport) int NetworkConnectionInfo_GetSize();
	}
}