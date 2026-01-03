#include <pch.h>
#include "NetworkMonitor.h"
#include "NetworkMethods.h"

namespace IronSight::Core::Native::Network
{
    // 全局监控器实例管理
    static thread_local NetworkMonitor* g_CurrentMonitor = nullptr;

    NetworkMonitor* NetworkMonitor_Create()
    {
        return new NetworkMonitor();
    }

    void NetworkMonitor_Destroy(NetworkMonitor* monitor)
    {
        delete monitor;
    }

    bool NetworkMonitor_Refresh(NetworkMonitor* monitor)
    {
        if (!monitor) return false;
        return monitor->Refresh();
    }

    bool NetworkMonitor_RefreshTcp(NetworkMonitor* monitor)
    {
        if (!monitor) return false;
        return monitor->RefreshTcp();
    }

    bool NetworkMonitor_RefreshUdp(NetworkMonitor* monitor)
    {
        if (!monitor) return false;
        return monitor->RefreshUdp();
    }

    size_t NetworkMonitor_GetConnectionCount(NetworkMonitor* monitor)
    {
        if (!monitor) return 0;
        return monitor->GetConnectionCount();
    }

    size_t NetworkMonitor_CopyConnections(NetworkMonitor* monitor, NetworkConnectionInfo* buffer, size_t bufferSize)
    {
        if (!monitor) return 0;
        return monitor->CopyConnectionsTo(buffer, bufferSize);
    }

    int NetworkConnectionInfo_GetSize()
    {
        return static_cast<int>(sizeof(NetworkConnectionInfo));
    }
}

