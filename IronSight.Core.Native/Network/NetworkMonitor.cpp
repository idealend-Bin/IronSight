#include <pch.h>
#include "NetworkMonitor.h"


#pragma comment(lib, "iphlpapi.lib")
#pragma comment(lib, "ws2_32.lib")

namespace IronSight::Core::Native::Network
{
    NetworkMonitor::NetworkMonitor()
    {
        _connections.reserve(InitialConnectionCapacity);
        _tcpTableBuffer.resize(InitialBufferSize);
        _udpTableBuffer.resize(InitialBufferSize);
    }

    NetworkMonitor::~NetworkMonitor() = default;

    bool NetworkMonitor::Refresh()
    {
        std::lock_guard<std::mutex> lock(_mutex);

        _connections.clear();

        bool tcpSuccess = RefreshTcpConnectionsInternal();
        bool udpSuccess = RefreshUdpConnectionsInternal();

        return tcpSuccess && udpSuccess;
    }

    bool NetworkMonitor::RefreshTcp()
    {
        std::lock_guard<std::mutex> lock(_mutex);

        _connections.clear();
        return RefreshTcpConnectionsInternal();
    }

    bool NetworkMonitor::RefreshUdp()
    {
        std::lock_guard<std::mutex> lock(_mutex);

        _connections.clear();
        return RefreshUdpConnectionsInternal();
    }

    bool NetworkMonitor::RefreshTcpConnectionsInternal()
    {
        DWORD bufferSize = static_cast<DWORD>(_tcpTableBuffer.size());
        DWORD result = ERROR_SUCCESS;

        // 循环直到缓冲区足够大
        while (true)
        {
            result = GetExtendedTcpTable(
                _tcpTableBuffer.data(),
                &bufferSize,
                FALSE,
                AF_INET,
                TCP_TABLE_OWNER_PID_ALL,
                0
            );

            if (result == ERROR_SUCCESS)
            {
                break;
            }
            else if (result == ERROR_INSUFFICIENT_BUFFER)
            {
                // 扩展缓冲区并重试
                _tcpTableBuffer.resize(bufferSize + 4096);
            }
            else
            {
                return false;
            }
        }

        auto* tcpTable = reinterpret_cast<MIB_TCPTABLE_OWNER_PID*>(
            _tcpTableBuffer.data());

        // 预留空间以避免多次重新分配
        _connections.reserve(_connections.size() + tcpTable->dwNumEntries);

        for (DWORD i = 0; i < tcpTable->dwNumEntries; ++i)
        {
            const auto& row = tcpTable->table[i];

            NetworkConnectionInfo info{};
            info.LocalAddress = row.dwLocalAddr;
            info.RemoteAddress = row.dwRemoteAddr;
            info.LocalPort = ntohs(static_cast<uint16_t>(row.dwLocalPort));
            info.RemotePort = ntohs(static_cast<uint16_t>(row.dwRemotePort));
            info.ProcessId = row.dwOwningPid;
            info.Protocol = ProtocolType::Tcp;

            // 转换状态
            switch (row.dwState)
            {
            case MIB_TCP_STATE_CLOSED:
                info.State = ConnectionState::Closed;
                break;
            case MIB_TCP_STATE_LISTEN:
                info.State = ConnectionState::Listen;
                break;
            case MIB_TCP_STATE_SYN_SENT:
                info.State = ConnectionState::SynSent;
                break;
            case MIB_TCP_STATE_SYN_RCVD:
                info.State = ConnectionState::SynReceived;
                break;
            case MIB_TCP_STATE_ESTAB:
                info.State = ConnectionState::Established;
                break;
            case MIB_TCP_STATE_FIN_WAIT1:
                info.State = ConnectionState::FinWait1;
                break;
            case MIB_TCP_STATE_FIN_WAIT2:
                info.State = ConnectionState::FinWait2;
                break;
            case MIB_TCP_STATE_CLOSE_WAIT:
                info.State = ConnectionState::CloseWait;
                break;
            case MIB_TCP_STATE_CLOSING:
                info.State = ConnectionState::Closing;
                break;
            case MIB_TCP_STATE_LAST_ACK:
                info.State = ConnectionState::LastAck;
                break;
            case MIB_TCP_STATE_TIME_WAIT:
                info.State = ConnectionState::TimeWait;
                break;
            case MIB_TCP_STATE_DELETE_TCB:
                info.State = ConnectionState::DeleteTcb;
                break;
            default:
                info.State = ConnectionState::Unknown;
                break;
            }

            _connections.push_back(info);
        }

        return true;
    }

    bool NetworkMonitor::RefreshUdpConnectionsInternal()
    {
        DWORD bufferSize = static_cast<DWORD>(_udpTableBuffer.size());
        DWORD result = ERROR_SUCCESS;

        while (true)
        {
            result = GetExtendedUdpTable(
                _udpTableBuffer.data(),
                &bufferSize,
                FALSE,
                AF_INET,
                UDP_TABLE_OWNER_PID,
                0
            );

            if (result == ERROR_SUCCESS)
            {
                break;
            }
            else if (result == ERROR_INSUFFICIENT_BUFFER)
            {
                _udpTableBuffer.resize(bufferSize + 4096);
            }
            else
            {
                return false;
            }
        }

        auto* udpTable = reinterpret_cast<MIB_UDPTABLE_OWNER_PID*>(
            _udpTableBuffer.data());

        _connections.reserve(_connections.size() + udpTable->dwNumEntries);

        for (DWORD i = 0; i < udpTable->dwNumEntries; ++i)
        {
            const auto& row = udpTable->table[i];

            NetworkConnectionInfo info{};
            info.LocalAddress = row.dwLocalAddr;
            info.RemoteAddress = 0;  // UDP无连接
            info.LocalPort = ntohs(static_cast<uint16_t>(row.dwLocalPort));
            info.RemotePort = 0;
            info.ProcessId = row.dwOwningPid;
            info.Protocol = ProtocolType::Udp;
            info.State = ConnectionState::Unknown;  // UDP无状态

            _connections.push_back(info);
        }

        return true;
    }

    size_t NetworkMonitor::GetConnectionCount() const noexcept
    {
        std::lock_guard<std::mutex> lock(_mutex);
        return _connections.size();
    }

    const NetworkConnectionInfo* NetworkMonitor::GetConnections() const noexcept
    {
        return _connections.data();
    }

    size_t NetworkMonitor::CopyConnectionsTo(NetworkConnectionInfo* buffer, size_t bufferSize) const
    {
        std::lock_guard<std::mutex> lock(_mutex);

        size_t copyCount = (std::min)(_connections.size(), bufferSize);

        if (copyCount > 0 && buffer != nullptr)
        {
            std::memcpy(buffer, _connections.data(), 
                copyCount * sizeof(NetworkConnectionInfo));
        }

        return copyCount;
    }
}