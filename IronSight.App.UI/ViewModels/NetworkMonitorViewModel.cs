using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows.Threading;
using IronSight.Interop.Native.Network;
using IronSight.Interop.Services;

namespace IronSight.App.UI.ViewModels
{
    /// <summary>
    /// 网络连接显示模型
    /// </summary>
    public sealed class NetworkConnectionDisplayModel : BaseViewModel
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
            set => SetProperty(ref _processName, value);
        }

        public string LocalEndPoint => $"{LocalAddress}:{LocalPort}";
        public string RemoteEndPoint => $"{RemoteAddress}:{RemotePort}";
    }

    /// <summary>
    /// 网络监控器ViewModel
    /// </summary>
    public sealed class NetworkMonitorViewModel : BaseViewModel, IDisposable
    {
        private readonly NetworkService _monitor;
        private readonly DispatcherTimer _refreshTimer;
        private readonly Dictionary<uint, string> _processNameCache;
        private readonly object _lockObject = new();

        private ObservableCollection<NetworkConnectionDisplayModel> _connections;
        private bool _isRefreshing;
        private int _tcpConnectionCount;
        private int _udpConnectionCount;
        private int _totalConnectionCount;
        private double _refreshInterval = 1000;
        private bool _isAutoRefreshEnabled = true;
        private bool _showTcp = true;
        private bool _showUdp = true;
        private string _filterText = string.Empty;
        private bool _disposed;

        public NetworkMonitorViewModel()
        {
            _monitor = new NetworkService();
            _connections = new ObservableCollection<NetworkConnectionDisplayModel>();
            _processNameCache = new Dictionary<uint, string>();

            _refreshTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(_refreshInterval)
            };
            _refreshTimer.Tick += OnRefreshTimerTick;
        }

        #region Properties

        /// <summary>
        /// 网络连接集合
        /// </summary>
        public ObservableCollection<NetworkConnectionDisplayModel> Connections
        {
            get => _connections;
            private set => SetProperty(ref _connections, value);
        }

        /// <summary>
        /// 是否正在刷新
        /// </summary>
        public bool IsRefreshing
        {
            get => _isRefreshing;
            private set
            {
                if (SetProperty(ref _isRefreshing, value))
                {
                    OnPropertyChanged(nameof(IsNotRefreshing));
                }
            }
        }

        /// <summary>
        /// 是否未在刷新
        /// </summary>
        public bool IsNotRefreshing => !IsRefreshing;

        /// <summary>
        /// TCP连接数量
        /// </summary>
        public int TcpConnectionCount
        {
            get => _tcpConnectionCount;
            private set => SetProperty(ref _tcpConnectionCount, value);
        }

        /// <summary>
        /// UDP连接数量
        /// </summary>
        public int UdpConnectionCount
        {
            get => _udpConnectionCount;
            private set => SetProperty(ref _udpConnectionCount, value);
        }

        /// <summary>
        /// 总连接数量
        /// </summary>
        public int TotalConnectionCount
        {
            get => _totalConnectionCount;
            private set => SetProperty(ref _totalConnectionCount, value);
        }

        /// <summary>
        /// 刷新间隔（毫秒）
        /// </summary>
        public double RefreshInterval
        {
            get => _refreshInterval;
            set
            {
                if (Math.Abs(_refreshInterval - value) > 0.001)
                {
                    _refreshInterval = Math.Max(100, value);
                    _refreshTimer.Interval = TimeSpan.FromMilliseconds(_refreshInterval);
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 是否启用自动刷新
        /// </summary>
        public bool IsAutoRefreshEnabled
        {
            get => _isAutoRefreshEnabled;
            set
            {
                if (SetProperty(ref _isAutoRefreshEnabled, value))
                {
                    if (value)
                        _refreshTimer.Start();
                    else
                        _refreshTimer.Stop();
                }
            }
        }

        /// <summary>
        /// 显示TCP连接
        /// </summary>
        public bool ShowTcp
        {
            get => _showTcp;
            set
            {
                if (SetProperty(ref _showTcp, value))
                {
                    _ = RefreshAsync();
                }
            }
        }

        /// <summary>
        /// 显示UDP连接
        /// </summary>
        public bool ShowUdp
        {
            get => _showUdp;
            set
            {
                if (SetProperty(ref _showUdp, value))
                {
                    _ = RefreshAsync();
                }
            }
        }

        /// <summary>
        /// 过滤文本
        /// </summary>
        public string FilterText
        {
            get => _filterText;
            set
            {
                if (SetProperty(ref _filterText, value ?? string.Empty))
                {
                    ApplyFilter();
                }
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 启动监控
        /// </summary>
        public void Start()
        {
            if (_isAutoRefreshEnabled)
            {
                _refreshTimer.Start();
            }

            _ = RefreshAsync();
        }

        /// <summary>
        /// 停止监控
        /// </summary>
        public void Stop()
        {
            _refreshTimer.Stop();
        }

        /// <summary>
        /// 手动刷新
        /// </summary>
        public async Task RefreshAsync()
        {
            if (_disposed || IsRefreshing) return;

            IsRefreshing = true;

            try
            {
                var connections = await Task.Run(() =>
                {
                    lock (_lockObject)
                    {
                        _monitor.Refresh();
                        return _monitor.GetConnectionsList();
                    }
                });

                UpdateConnections(connections);
            }
            finally
            {
                IsRefreshing = false;
            }
        }

        /// <summary>
        /// 清除进程名称缓存
        /// </summary>
        public void ClearProcessNameCache()
        {
            lock (_processNameCache)
            {
                _processNameCache.Clear();
            }
        }

        #endregion

        #region Private Methods

        private void OnRefreshTimerTick(object? sender, EventArgs e)
        {
            _ = RefreshAsync();
        }

        private void UpdateConnections(List<NetworkConnectionInfo> connections)
        {
            var displayModels = new List<NetworkConnectionDisplayModel>();

            int tcpCount = 0;
            int udpCount = 0;

            foreach (var conn in connections)
            {
                bool isTcp = conn.Protocol == ProtocolType.Tcp;
                bool isUdp = conn.Protocol == ProtocolType.Udp;

                if (isTcp) tcpCount++;
                if (isUdp) udpCount++;

                // 应用协议过滤
                if (isTcp && !_showTcp) continue;
                if (isUdp && !_showUdp) continue;

                var model = new NetworkConnectionDisplayModel
                {
                    LocalAddress = conn.LocalAddressString,
                    LocalPort = conn.LocalPort,
                    RemoteAddress = conn.RemoteAddressString,
                    RemotePort = conn.RemotePort,
                    State = GetStateDisplayName(conn.State),
                    Protocol = conn.Protocol.ToString().ToUpperInvariant(),
                    ProcessId = conn.ProcessId,
                    ProcessName = GetProcessName(conn.ProcessId)
                };

                displayModels.Add(model);
            }

            TcpConnectionCount = tcpCount;
            UdpConnectionCount = udpCount;
            TotalConnectionCount = connections.Count;

            // 在UI线程更新集合
            System.Windows.Application.Current?.Dispatcher.Invoke(() =>
            {
                Connections = new ObservableCollection<NetworkConnectionDisplayModel>(
                    ApplyFilter(displayModels));
            });
        }

        private IEnumerable<NetworkConnectionDisplayModel> ApplyFilter(
            IEnumerable<NetworkConnectionDisplayModel> connections)
        {
            if (string.IsNullOrWhiteSpace(_filterText))
            {
                return connections;
            }

            string filter = _filterText.ToLowerInvariant();

            return connections.Where(c =>
                c.LocalAddress.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                c.RemoteAddress.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                c.LocalPort.ToString().Contains(filter) ||
                c.RemotePort.ToString().Contains(filter) ||
                c.ProcessId.ToString().Contains(filter) ||
                c.ProcessName.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                c.Protocol.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                c.State.Contains(filter, StringComparison.OrdinalIgnoreCase));
        }

        private void ApplyFilter()
        {
            // 重新应用过滤器
            _ = RefreshAsync();
        }

        private string GetProcessName(uint processId)
        {
            if (processId == 0) return "System Idle";
            if (processId == 4) return "System";

            lock (_processNameCache)
            {
                if (_processNameCache.TryGetValue(processId, out var cachedName))
                {
                    return cachedName;
                }
            }

            string processName;
            try
            {
                using var process = Process.GetProcessById((int)processId);
                processName = process.ProcessName;
            }
            catch
            {
                processName = $"<PID:{processId}>";
            }

            lock (_processNameCache)
            {
                _processNameCache[processId] = processName;
            }

            return processName;
        }

        private static string GetStateDisplayName(ConnectionState state)
        {
            return state switch
            {
                ConnectionState.Unknown => "Unknown",
                ConnectionState.Closed => "Closed",
                ConnectionState.Listen => "Listening",
                ConnectionState.SynSent => "Syn Sent",
                ConnectionState.SynReceived => "Syn Received",
                ConnectionState.Established => "Established",
                ConnectionState.FinWait1 => "Fin Wait 1",
                ConnectionState.FinWait2 => "Fin Wait 2",
                ConnectionState.CloseWait => "Close Wait",
                ConnectionState.Closing => "Closing",
                ConnectionState.LastAck => "Last Ack",
                ConnectionState.TimeWait => "Time Wait",
                ConnectionState.DeleteTcb => "Delete TCB",
                _ => state.ToString()
            };
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (_disposed) return;

            _disposed = true;
            _refreshTimer.Stop();
            _monitor.Dispose();
        }

        #endregion
    }
}