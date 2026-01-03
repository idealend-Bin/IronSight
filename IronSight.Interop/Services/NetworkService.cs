using IronSight.Interop.Native.Network;
using System.ComponentModel;
using System.Net;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static IronSight.Interop.Native.Network.NetworkMethods;


namespace IronSight.Interop.Services
{
    /// <summary>
    /// 网络监控器Native互操作类
    /// </summary>
    public sealed class NetworkService : IDisposable
    {

        private IntPtr _nativeHandle;
        private bool _disposed;

        // 预分配的缓冲区，避免频繁GC
        private NetworkConnectionInfo[] _connectionBuffer;
        private GCHandle _bufferHandle;
        private const int DefaultBufferCapacity = 2048;

        /// <summary>
        /// 创建网络监控器实例
        /// </summary>
        public NetworkService()
        {
            _nativeHandle = NetworkMonitor_Create();

            if (_nativeHandle == IntPtr.Zero)
            {
                throw new InvalidOperationException(
                    "Failed to create native NetworkMonitor instance.");
            }

            // 验证结构体大小匹配
            int nativeSize = NetworkConnectionInfo_GetSize();
            int managedSize = Marshal.SizeOf<NetworkConnectionInfo>();

            if (nativeSize != managedSize)
            {
                NetworkMonitor_Destroy(_nativeHandle);
                throw new InvalidOperationException(
                    $"Structure size mismatch: Native={nativeSize}, Managed={managedSize}");
            }

            _connectionBuffer = new NetworkConnectionInfo[DefaultBufferCapacity];
            _bufferHandle = GCHandle.Alloc(_connectionBuffer, GCHandleType.Pinned);
        }

        /// <summary>
        /// 刷新所有网络连接信息
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Refresh()
        {
            ThrowIfDisposed();
            return NetworkMonitor_Refresh(_nativeHandle);
        }

        /// <summary>
        /// 仅刷新TCP连接
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool RefreshTcp()
        {
            ThrowIfDisposed();
            return NetworkMonitor_RefreshTcp(_nativeHandle);
        }

        /// <summary>
        /// 仅刷新UDP连接
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool RefreshUdp()
        {
            ThrowIfDisposed();
            return NetworkMonitor_RefreshUdp(_nativeHandle);
        }

        /// <summary>
        /// 获取当前连接数量
        /// </summary>
        public int ConnectionCount
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                ThrowIfDisposed();
                return (int)NetworkMonitor_GetConnectionCount(_nativeHandle);
            }
        }

        /// <summary>
        /// 获取连接信息数组
        /// </summary>
        /// <returns>连接信息只读跨度</returns>
        public ReadOnlySpan<NetworkConnectionInfo> GetConnections()
        {
            ThrowIfDisposed();

            int count = ConnectionCount;

            if (count == 0)
            {
                return ReadOnlySpan<NetworkConnectionInfo>.Empty;
            }
           
            // 如果缓冲区太小，重新分配
            EnsureBufferCapacity(count);

            nuint copied = NetworkMonitor_CopyConnections(
                _nativeHandle,
                _bufferHandle.AddrOfPinnedObject(),
                (nuint)count);

            return new ReadOnlySpan<NetworkConnectionInfo>(_connectionBuffer, 0, (int)copied);
        }

        /// <summary>
        /// 获取连接信息列表（分配新内存）
        /// </summary>
        public List<NetworkConnectionInfo> GetConnectionsList()
        {
            var span = GetConnections();
            var list = new List<NetworkConnectionInfo>(span.Length);

            foreach (ref readonly var connection in span)
            {
                list.Add(connection);
            }

            return list;
        }

        /// <summary>
        /// 更新网络监控的采样频率
        /// </summary>
        /// <param name="tickRate">采样间隔时间</param>
        public void UpdateTickRate(TimeSpan tickRate)
        {
            ThrowIfDisposed();
            
            // 将时间间隔转换为毫秒
            uint intervalMs = (uint)tickRate.TotalMilliseconds;
            
            // 调用原生方法更新采样频率
            bool success = NetworkMonitor_SetUpdateInterval(_nativeHandle, intervalMs);
            
            if (!success)
            {
                throw new InvalidOperationException("Failed to update network monitor tick rate.");
            }
        }

        private void EnsureBufferCapacity(int requiredCapacity)
        {
            if (_connectionBuffer.Length >= requiredCapacity)
            {
                return;
            }

            // 释放旧的句柄
            if (_bufferHandle.IsAllocated)
            {
                _bufferHandle.Free();
            }

            // 分配更大的缓冲区（增长因子1.5）
            int newCapacity = Math.Max(requiredCapacity,
                (int)(_connectionBuffer.Length * 1.5));

            _connectionBuffer = new NetworkConnectionInfo[newCapacity];
            _bufferHandle = GCHandle.Alloc(_connectionBuffer, GCHandleType.Pinned);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(NetworkService));
            }
        }

        public void Dispose()
        {
            if (_disposed) return;

            _disposed = true;

            if (_bufferHandle.IsAllocated)
            {
                _bufferHandle.Free();
            }

            if (_nativeHandle != IntPtr.Zero)
            {
                NetworkMonitor_Destroy(_nativeHandle);
                _nativeHandle = IntPtr.Zero;
            }

            GC.SuppressFinalize(this);
        }

        ~NetworkService()
        {
            Dispose();
        }
    }

    /// <summary>
    /// 网络连接信息结构体
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public readonly struct NetworkConnectionInfo
    {
        private readonly uint _localAddress;
        private readonly uint _remoteAddress;
        private readonly ushort _localPort;
        private readonly ushort _remotePort;
        private readonly ConnectionState _state;
        private readonly ProtocolType _protocol;
        private readonly uint _processId;
        private readonly ulong _reserved;

        /// <summary>
        /// 本地IP地址
        /// </summary>
        public IPAddress LocalAddress => new(_localAddress);

        /// <summary>
        /// 远程IP地址
        /// </summary>
        public IPAddress RemoteAddress => new(_remoteAddress);

        /// <summary>
        /// 本地端口
        /// </summary>
        public ushort LocalPort => _localPort;

        /// <summary>
        /// 远程端口
        /// </summary>
        public ushort RemotePort => _remotePort;

        /// <summary>
        /// 连接状态
        /// </summary>
        public ConnectionState State => _state;

        /// <summary>
        /// 协议类型
        /// </summary>
        public ProtocolType Protocol => _protocol;

        /// <summary>
        /// 进程ID
        /// </summary>
        public uint ProcessId => _processId;

        /// <summary>
        /// 本地地址字符串
        /// </summary>
        public string LocalAddressString => LocalAddress.ToString();

        /// <summary>
        /// 远程地址字符串
        /// </summary>
        public string RemoteAddressString => RemoteAddress.ToString();

        /// <summary>
        /// 本地端点字符串
        /// </summary>
        public string LocalEndPoint => $"{LocalAddressString}:{LocalPort}";

        /// <summary>
        /// 远程端点字符串
        /// </summary>
        public string RemoteEndPoint => $"{RemoteAddressString}:{RemotePort}";
    }

    /// <summary>
    /// 网络连接显示模型
    /// </summary>
    public sealed class NetworkConnectionDisplayModel : INotifyPropertyChanged
    {
        private string _processName = string.Empty;

        public string LocalAddress { get; init; } = string.Empty;
        public ushort LocalPort { get; init; }
        public string RemoteAddress { get; init; } = string.Empty;
        public ushort RemotePort { get; init; }
        public string State { get; init; } = string.Empty;
        public string Protocol { get; init; } = string.Empty;
        public uint ProcessId { get; init; }

        public string ProcessName
        {
            get => _processName;
            set
            {
                if (_processName != value)
                {
                    _processName = value;
                    OnPropertyChanged();
                }
            }
        }

        public string LocalEndPoint => $"{LocalAddress}:{LocalPort}";
        public string RemoteEndPoint => $"{RemoteAddress}:{RemotePort}";

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

