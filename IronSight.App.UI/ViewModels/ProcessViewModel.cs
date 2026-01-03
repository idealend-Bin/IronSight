using IronSight.App.UI.Core;
using IronSight.Interop.Core;
using IronSight.Interop.Native.System;
using IronSight.Interop.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

namespace IronSight.App.UI.ViewModels
{
    /// <summary>
    /// 进程管理器 ViewModel
    /// 负责进程列表的差量更新、右键菜单指令分发以及系统性能快照同步
    /// </summary>
    public class ProcessViewModel : BaseViewModel, IDisposable
    {
        private readonly SystemMonitorServiceEx _monitorService;
        private string _searchText = string.Empty;
        private ProcessDetailInfo? _selectedProcess;
        private uint _processCount;
        private uint _totalHandleCount;

        // 关键：操作锁。
        // 当弹出 MessageBox 或执行同步阻塞操作时，防止后台服务更新集合导致选中的对象在内存中位移或失效。
        private bool _isActionLocked = false;

        private ObservableCollection<ProcessDetailInfo> _processes = new();
        public ICollectionView ProcessView { get; }

        #region 命令定义

        public ICommand TerminateProcessCommand { get; }
        public ICommand OpenLocationCommand { get; }
        public ICommand SetPriorityCommand { get; }
        public ICommand SearchOnlineCommand { get; }
        public ICommand ShowPropertiesCommand { get; }
        public ICommand RefreshCommand { get; }

        #endregion

        public ProcessViewModel()
        {
            ProcessView = CollectionViewSource.GetDefaultView(_processes);
            ProcessView.Filter = FilterProcess;

            // 命令初始化 - 这里统一使用 RelayCommand (已整合原有 Ex 的功能)
            TerminateProcessCommand = new RelayCommand(ExecuteTerminateProcess, CanExecuteOnSelected);
            OpenLocationCommand = new RelayCommand(ExecuteOpenLocation, CanExecuteOnSelected);
            SetPriorityCommand = new RelayCommand<string>(ExecuteSetPriority, _ => CanExecuteOnSelected());
            SearchOnlineCommand = new RelayCommand(ExecuteSearchOnline, CanExecuteOnSelected);
            ShowPropertiesCommand = new RelayCommand(ExecuteShowProperties, CanExecuteOnSelected);
            RefreshCommand = new RelayCommand(() => ProcessView.Refresh());

            // 初始化监控服务，采样频率 2000ms
            _monitorService = new SystemMonitorServiceEx(2000);
            _monitorService.ProcessListUpdated += OnProcessListUpdated;
            _monitorService.GlobalSnapshotUpdated += OnGlobalSnapshotUpdated;

            _monitorService.Start();
        }

        #region 绑定属性

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    ProcessView.Refresh();
                }
            }
        }

        public ProcessDetailInfo? SelectedProcess
        {
            get => _selectedProcess;
            set
            {
                if (SetProperty(ref _selectedProcess, value))
                {
                    // 统一触发所有依赖选中项的命令状态检查
                    RefreshAllCommands();
                }
            }
        }

        public uint ProcessCount
        {
            get => _processCount;
            set => SetProperty(ref _processCount, value);
        }

        public uint TotalHandleCount
        {
            get => _totalHandleCount;
            set => SetProperty(ref _totalHandleCount, value);
        }

        #endregion

        #region 核心业务实现 - 终止进程

        private void ExecuteTerminateProcess()
        {
            if (SelectedProcess == null) return;

            // 1. 抓取副本 (Value Copy)，避免异步刷新导致的引用漂移
            var target = SelectedProcess.Value;

            // 2. 加锁
            _isActionLocked = true;

            try
            {
                var result = MessageBox.Show(
                    $"确定要结束进程 {target.Name} (PID: {target.Pid}) 吗？\n该操作可能导致未保存的数据丢失。",
                    "系统安全警告",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        LoggerService.Log(LogLevel.Warn, $"尝试终结进程: {target.Name}, PID: {target.Pid}");

                        // 调用你实现的自定义终止逻辑
                        bool isSuccess = SystemMethods.TerminateSelectedProcess(target.Pid);

                        if (isSuccess)
                        {
                            LoggerService.Log(LogLevel.Warn, $"成功终结进程: {target.Name}, PID: {target.Pid}");
                        }
                        else
                        {
                            LoggerService.Log(LogLevel.Error, $"失败终结进程: {target.Name}, PID: {target.Pid}");
                            MessageBox.Show("结束进程失败：可能没有足够的权限或进程已结束。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                    catch (Exception ex)
                    {
                        LoggerService.Log(LogLevel.Error, $"执行终结进程时发生异常：{ex.Message}");
                        MessageBox.Show($"执行操作时发生异常：{ex.Message}", "异常", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            finally
            {
                // 3. 释放锁
                _isActionLocked = false;
            }
        }

        #endregion

        #region 扩展业务实现 - 菜单指令

        private void ExecuteOpenLocation()
        {
            if (SelectedProcess == null) return;

            var pathBuffer = new StringBuilder(260);
            if (SystemMethods.GetProcessFullPath(SelectedProcess.Value.Pid, pathBuffer, (uint)pathBuffer.Capacity))
            {
                try
                {
                    string path = pathBuffer.ToString();
                    System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{path}\"");
                }
                catch (Exception ex)
                {
                    LoggerService.Log(LogLevel.Error, $"无法打开文件位置: {ex.Message}");
                }
            }
        }

        private void ExecuteSetPriority(string? level)
        {
            if (SelectedProcess == null || string.IsNullOrEmpty(level)) return;

            uint priorityClass = level switch
            {
                "Realtime" => 0x00000100,
                "High" => 0x00000080,
                "AboveNormal" => 0x00008000,
                "Normal" => 0x00000020,
                "BelowNormal" => 0x00004000,
                "Low" => 0x00000040,
                _ => 0x00000020
            };

            bool success = SystemMethods.SetProcessPriority(SelectedProcess.Value.Pid, priorityClass);
            if (!success)
            {
                LoggerService.Log(LogLevel.Error, $"设置进程优先级失败 PID: {SelectedProcess.Value.Pid}");
            }
        }

        private void ExecuteSearchOnline()
        {
            if (SelectedProcess == null) return;
            string url = $"https://www.bing.com/search?q={Uri.EscapeDataString(SelectedProcess.Value.Name)}";
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = url, UseShellExecute = true });
            }
            catch { /* 忽略浏览器启动异常 */ }
        }

        private void ExecuteShowProperties()
        {
            if (SelectedProcess == null) return;
            var pathBuffer = new StringBuilder(260);
            if (SystemMethods.GetProcessFullPath(SelectedProcess.Value.Pid, pathBuffer, (uint)pathBuffer.Capacity))
            {
                SystemMethods.ShowFileProperties(pathBuffer.ToString());
            }
        }

        #endregion

        #region 后台回调处理

        private void OnProcessListUpdated(object? sender, List<ProcessDetailInfo> newList)
        {
            if (_isActionLocked) return;

            Application.Current.Dispatcher.Invoke(() =>
            {
                uint? savedSelectedPid = SelectedProcess?.Pid;
                var newPids = new HashSet<uint>(newList.Select(p => p.Pid));

                // A. 移除
                for (int i = _processes.Count - 1; i >= 0; i--)
                {
                    if (!newPids.Contains(_processes[i].Pid)) _processes.RemoveAt(i);
                }

                // B. 更新或添加
                foreach (var newItem in newList)
                {
                    var existingIndex = _processes.Cast<ProcessDetailInfo?>().Select((p, idx) => new { p, idx }).FirstOrDefault(x => x.p?.Pid == newItem.Pid)?.idx ?? -1;

                    if (existingIndex >= 0)
                    {
                        if (_processes[existingIndex].IsVisuallyDifferent(newItem))
                        {
                            _processes[existingIndex] = newItem;
                        }
                    }
                    else
                    {
                        _processes.Add(newItem);
                    }
                }

                ProcessCount = (uint)newList.Count;

                // 恢复选中
                if (savedSelectedPid.HasValue)
                {
                    var updatedSelection = _processes.FirstOrDefault(p => p.Pid == savedSelectedPid.Value);
                    if (updatedSelection.Pid != 0) SelectedProcess = updatedSelection;
                }
            });
        }

        private void OnGlobalSnapshotUpdated(object? sender, SystemPerformanceSnapshot snapshot)
        {
            Application.Current.Dispatcher.Invoke(() => TotalHandleCount = snapshot.HandleCount);
        }

        #endregion

        #region 辅助方法

        private bool FilterProcess(object obj)
        {
            if (string.IsNullOrWhiteSpace(SearchText)) return true;
            return obj is ProcessDetailInfo info &&
                  (info.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase) || info.Pid.ToString().Contains(SearchText));
        }

        private bool CanExecuteOnSelected() => SelectedProcess != null && SelectedProcess.Value.Pid > 4;

        private void RefreshAllCommands()
        {
            (TerminateProcessCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (OpenLocationCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (ShowPropertiesCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (SearchOnlineCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }

        #endregion

        public void Dispose()
        {
            _monitorService.ProcessListUpdated -= OnProcessListUpdated;
            _monitorService.GlobalSnapshotUpdated -= OnGlobalSnapshotUpdated;
            _monitorService.Stop();
            _monitorService.Dispose();
        }
    }
}