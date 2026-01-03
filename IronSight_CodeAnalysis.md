# IronSight 项目代码分析文档

## 概述

IronSight 是一个基于 .NET 6 和 WPF 的 Windows 高性能系统监控与优化工具，集成了 C++ 原生组件以提供系统级性能监控功能。项目融合了 Fluent Design 设计语言，打造现代化的视觉体验，同时通过混合语言架构确保高性能和系统级访问能力。

## 项目架构

```
IronSight/
├── IronSight.App.UI/          # WPF 主应用程序
│   ├── Core/                  # 核心配置系统
│   │   └── UserConfig.cs      # 用户配置模型和服务
│   ├── ViewModels/            # 视图模型层
│   │   ├── MainViewModel.cs   # 主视图模型
│   │   ├── NetworkMonitorViewModel.cs  # ⭐ 网络监控视图模型
│   │   ├── ProcessViewModel.cs         # 进程管理视图模型
│   │   └── SettingsViewModel.cs        # ⭐ 设置页面视图模型
│   └── Views/                 # 用户界面层
│       ├── NetworkMonitorView.xaml     # ⭐ 网络监控完整实现
│       ├── ProcessManagerView.xaml     # 进程管理完整实现
│       └── SettingsView.xaml           # ⭐ 设置页面完整实现
├── IronSight.Interop/         # 互操作层
│   ├── Services/              # 服务层
│   │   ├── NetworkService.cs  # ⭐ 网络监控服务
│   │   ├── SystemMonitorServiceEx.cs   # 高级系统监控服务
│   │   └── ...
│   └── Native/                # 原生方法封装
│       ├── Network/           # ⭐ 网络监控原生接口
│       │   └── NetworkMethods.cs
│       └── ...
├── IronSight.Core.Native/     # C++ 原生组件
│   ├── NetworkMonitor.h/.cpp  # ⭐ 网络监控核心实现
│   ├── NetworkMethods.h/.cpp  # ⭐ 网络监控C接口
│   ├── SystemMethods.h/.cpp   # 扩展系统监控实现
│   └── ...
├── IronSight.Extensions.Py/   # Python 扩展模块（空）
└── IronSight/                 # 额外的 .NET 项目（控制台程序）
```

## C# 代码分析

### IronSight.App.UI 命名空间

#### Core 命名空间 ⭐ **新增重要模块**

##### UserConfig
- **位置**: `IronSight.App.UI/Core/UserConfig.cs`
- **功能**: 用户配置模型，对应设置页面的所有持久化项
- **属性**:
  - `Language`: 语言设置（默认"简体中文"）
  - `IsDarkMode`: 是否深色模式（默认true）
  - `SamplingIntervalMs`: 采样间隔毫秒（默认1000）
  - `IsAutoStart`: 是否自动启动（默认false）
  - `AlwaysOnTop`: 是否总是置顶（默认false）

##### ConfigService
- **位置**: `IronSight.App.UI/Core/UserConfig.cs`
- **功能**: 负责配置文件的加载、保存与路径管理
- **配置路径**: `%APPDATA%\IronSight\settings.json`
- **主要方法**:
  - `ConfigService()`: 构造函数，初始化配置路径和加载配置
  - `Load()`: 加载配置文件（私有方法）
  - `Save()`: 保存配置到文件
  - `Reset()`: 重置为默认配置
  - `ResetToDefault()`: 重置为默认配置（别名方法）
- **属性**:
  - `Current`: 当前配置对象
  - `CurrentConfig`: 当前配置对象（别名属性）
- **错误处理**: 配置加载/失败时输出调试信息并使用默认配置

#### 类和结构体

##### MainWindow
- **位置**: `IronSight.App.UI/MainWindow.xaml.cs`
- **继承**: `Window`
- **功能**: 主窗口类，提供完整的窗口控制和导航功能
- **主要方法**:
  - `MainWindow()`: 构造函数，初始化组件
  - `OnClosed(EventArgs e)`: 窗口关闭事件处理，释放资源
  - `TitleBar_MouseDown(object sender, MouseButtonEventArgs e)`: 标题栏鼠标按下事件，实现窗口拖动
  - `Minimize_Click(object sender, RoutedEventArgs e)`: 最小化窗口
  - `Maximize_Click(object sender, RoutedEventArgs e)`: 最大化/还原窗口
  - `Close_Click(object sender, RoutedEventArgs e)`: 关闭窗口
  - `Nav_Checked(object sender, RoutedEventArgs e)`: 导航选择事件

##### App
- **位置**: `IronSight.App.UI/App.xaml.cs`
- **继承**: `Application`
- **功能**: 应用程序入口点，负责初始化
- **主要方法**:
  - `OnStartup(StartupEventArgs e)`: 应用程序启动事件，初始化日志系统和权限提升

##### MainViewModel
- **位置**: `IronSight.App.UI/ViewModels/MainViewModel.cs`
- **继承**: `INotifyPropertyChanged, IDisposable`
- **功能**: 主视图模型，实现MVVM模式的核心逻辑
- **属性**:
  - `StatusMessage`: 状态消息
  - `CpuUsage`: CPU使用率
  - `DiskReadRate`: 磁盘读取速率
  - `DiskWriteRate`: 磁盘写入速率
  - `ClipboardHistory`: 剪贴板历史记录集合
- **命令**:
  - `CleanMemoryCommand`: 内存清理命令
  - `CopyClipboardItemCommand`: 复制剪贴板项命令
- **主要方法**:
  - `MainViewModel()`: 构造函数，初始化服务和命令
  - `OnSystemStatsUpdated(object? sender, SystemStatsEventArgs e)`: 系统统计更新事件处理
  - `OnClipboardChanged(object? sender, EventArgs e)`: 剪贴板变化事件处理
  - `ExecuteCleanMemory()`: 执行内存清理
  - `ExecuteCopyClipboardItem(ClipboardItem item)`: 执行复制剪贴板项
  - `Dispose()`: 释放资源

##### ProcessViewModel ⭐ **新增重要组件**
- **位置**: `IronSight.App.UI/ViewModels/ProcessViewModel.cs`
- **继承**: `UserControl, INotifyPropertyChanged, IDisposable`
- **功能**: 进程管理专用视图模型，提供高级进程监控和管理功能
- **属性**:
  - `ProcessList`: 进程详细信息列表
  - `SelectedProcess`: 当前选中的进程
  - `SearchText`: 搜索文本
  - `StatusMessage`: 状态消息
- **命令**:
  - `RefreshCommand`: 刷新进程列表
  - `TerminateProcessCommand`: 终止选中进程
  - `SearchCommand`: 搜索进程
- **主要方法**:
  - `ProcessViewModel()`: 构造函数，初始化SystemMonitorServiceEx
  - `OnProcessListUpdated(object? sender, List<ProcessDetailInfo> e)`: 进程列表更新处理
  - `ExecuteTerminateProcess()`: 执行进程终止
  - `ExecuteRefresh()`: 执行列表刷新
  - `ExecuteSearch()`: 执行进程搜索
  - `IsVisuallyDifferent()`: 差量比较方法，优化UI性能
  - `Dispose()`: 释放资源

##### ClipboardItem
- **位置**: `IronSight.App.UI/ViewModels/MainViewModel.cs`
- **功能**: 剪贴板项数据模型
- **属性**:
  - `Content`: 剪贴板内容
  - `Timestamp`: 时间戳
  - `DisplayText`: 显示文本（截断版本）

##### RelayCommand
- **位置**: `IronSight.App.UI/ViewModels/MainViewModel.cs`
- **继承**: `ICommand`
- **功能**: 标准中继命令实现，支持MVVM模式
- **主要方法**:
  - `RelayCommand(Action execute)`: 构造函数
  - `CanExecute(object? parameter)`: 判断命令是否可执行
  - `Execute(object? parameter)`: 执行命令

##### RelayCommand<T>
- **位置**: `IronSight.App.UI/ViewModels/MainViewModel.cs`
- **继承**: `ICommand`
- **功能**: 泛型中继命令实现
- **主要方法**:
  - `RelayCommand(Action<T> execute)`: 构造函数
  - `CanExecute(object? parameter)`: 判断命令是否可执行
  - `Execute(object? parameter)`: 执行命令

##### RelayCommandEx ⭐ **新增组件**
- **位置**: `IronSight.App.UI/ViewModels/MainViewModel.cs`
- **继承**: `ICommand`
- **功能**: 扩展中继命令，支持CanExecute逻辑
- **主要方法**:
  - `RelayCommandEx(Action execute, Func<bool> canExecute)`: 构造函数
  - `CanExecute(object? parameter)`: 判断命令是否可执行
  - `Execute(object? parameter)`: 执行命令

#### Views 命名空间

##### AboutView
- **位置**: `IronSight.App.UI/Views/AboutView.xaml.cs`
- **继承**: `UserControl`
- **功能**: 关于页面视图（占位符，未实现具体功能）
- **主要方法**:
  - `AboutView()`: 构造函数，初始化组件

##### ClipboardView
- **位置**: `IronSight.App.UI/Views/ClipboardView.xaml.cs`
- **继承**: `UserControl`
- **功能**: 剪贴板管理页面视图
- **主要方法**:
  - `ClipboardView()`: 构造函数，初始化组件
  - `Copy_Click(object sender, RoutedEventArgs e)`: 复制按钮点击事件

##### DashboardView
- **位置**: `IronSight.App.UI/Views/DashboardView.xaml.cs`
- **继承**: `UserControl`
- **功能**: 仪表板页面视图
- **主要方法**:
  - `DashboardView()`: 构造函数，初始化组件
  - `Clear_Click(object sender, RoutedEventArgs e)`: 清除按钮点击事件

##### SettingsView
- **位置**: `IronSight.App.UI/Views/SettingsView.xaml.cs`
- **继承**: `UserControl`
- **功能**: 设置页面视图（占位符，未实现具体功能）
- **主要方法**:
  - `SettingsView()`: 构造函数，初始化组件

##### MemoryCleanerView
- **位置**: `IronSight.App.UI/Views/MemoryCleanerView.xaml.cs`
- **继承**: `UserControl`
- **功能**: 内存清理页面视图
- **主要方法**:
  - `MemoryCleanerView()`: 构造函数，初始化组件

##### NetworkMonitorView ⭐ **功能完整的视图**
- **位置**: `IronSight.App.UI/Views/NetworkMonitorView.xaml` 和 `NetworkMonitorView.xaml.cs`
- **继承**: `UserControl`
- **功能**: 网络监控页面视图，提供完整的网络连接监控功能
- **主要功能**:
  - 实时显示TCP/UDP网络连接
  - 支持协议过滤（TCP/UDP）
  - 连接状态可视化显示
  - 进程名称解析和缓存
  - 自动刷新和手动刷新
  - 搜索过滤功能
  - 连接统计信息显示
- **主要方法**:
  - `NetworkMonitorView()`: 构造函数，初始化组件
  - `OnRefreshClick(object sender, RoutedEventArgs e)`: 手动刷新按钮事件
- **UI特性**:
  - Fluent Design深色主题
  - 虚拟化DataGrid支持大量连接
  - 状态颜色编码（ Established/Listening/TimeWait等）
  - 协议类型标签显示
  - 实时状态指示器

##### NetworkMonitorViewModel ⭐ **新增重要组件**
- **位置**: `IronSight.App.UI/ViewModels/NetworkMonitorViewModel.cs`
- **继承**: `INotifyPropertyChanged, IDisposable`
- **功能**: 网络监控专用视图模型，提供高级网络连接监控功能
- **属性**:
  - `Connections`: 网络连接集合
  - `IsRefreshing`: 是否正在刷新
  - `TcpConnectionCount`: TCP连接数量
  - `UdpConnectionCount`: UDP连接数量
  - `TotalConnectionCount`: 总连接数量
  - `RefreshInterval`: 刷新间隔（毫秒）
  - `IsAutoRefreshEnabled`: 是否启用自动刷新
  - `ShowTcp`: 显示TCP连接
  - `ShowUdp`: 显示UDP连接
  - `FilterText`: 过滤文本
- **主要方法**:
  - `NetworkMonitorViewModel()`: 构造函数，初始化NetworkService
  - `Start()`: 启动监控
  - `Stop()`: 停止监控
  - `RefreshAsync()`: 异步刷新连接列表
  - `ClearProcessNameCache()`: 清除进程名称缓存
  - `UpdateConnections(List<NetworkConnectionInfo> connections)`: 更新连接数据
  - `ApplyFilter()`: 应用过滤条件
  - `GetProcessName(uint processId)`: 获取进程名称（带缓存）
- **性能优化**:
  - 进程名称缓存机制
  - 异步数据加载
  - 虚拟化UI支持
  - 差量更新优化

##### SettingsViewModel ⭐ **新增重要组件**
- **位置**: `IronSight.App.UI/ViewModels/SettingsViewModel.cs`
- **继承**: `INotifyPropertyChanged`
- **功能**: 设置页面视图模型，管理用户配置
- **属性**:
  - `SelectedLanguage`: 选择的语言
  - `IsDarkMode`: 是否深色模式
  - `SamplingInterval`: 采样间隔
  - `IsAutoStart`: 是否自动启动
- **命令**:
  - `SaveCommand`: 保存设置命令
  - `ResetCommand`: 重置设置命令
- **主要方法**:
  - `SettingsViewModel(NetworkService networkService, ConfigService configService)`: 构造函数
  - `LoadSettings()`: 加载设置
  - `ExecuteSave()`: 执行保存
  - `ExecuteReset()`: 执行重置
  - `ApplyTheme()`: 应用主题

##### NetworkConnectionDisplayModel
- **位置**: `IronSight.App.UI/ViewModels/NetworkMonitorViewModel.cs` 和 `IronSight.Interop/Services/NetworkService.cs`
- **继承**: `INotifyPropertyChanged`
- **功能**: 网络连接显示模型，用于UI数据绑定
- **属性**:
  - `LocalAddress`: 本地地址
  - `LocalPort`: 本地端口
  - `RemoteAddress`: 远程地址
  - `RemotePort`: 远程端口
  - `State`: 连接状态
  - `Protocol`: 协议类型
  - `ProcessId`: 进程ID
  - `ProcessName`: 进程名称
  - `LocalEndPoint`: 本地端点字符串
  - `RemoteEndPoint`: 远程端点字符串

##### ProcessManagerView ⭐ **功能完整的视图**
- **位置**: `IronSight.App.UI/Views/ProcessManagerView.xaml.cs`
- **继承**: `UserControl`
- **功能**: 进程管理页面视图，提供完整的进程管理功能
- **主要方法**:
  - `ProcessManagerView()`: 构造函数，初始化组件
  - `DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)`: 双击事件处理
  - `SearchBox_TextChanged(object sender, TextChangedEventArgs e)`: 搜索文本变化处理
  - `TerminateButton_Click(object sender, RoutedEventArgs e)`: 终止按钮点击事件
  - `RefreshButton_Click(object sender, RoutedEventArgs e)`: 刷新按钮点击事件

### IronSight.Interop 命名空间

#### Core 命名空间

##### DllMain
- **位置**: `IronSight.Interop/Core/DllMain.cs`
- **功能**: DLL入口点管理类
- **属性**:
  - `IsInitialized`: 初始化状态
- **主要方法**:
  - `Initialize()`: 初始化日志系统和权限提升
  - `EnableDebugPrivilege(bool enableFlag)`: 启用/禁用调试权限（P/Invoke）

##### LoggerService
- **位置**: `IronSight.Interop/Core/LoggerService.cs`
- **功能**: 日志服务，提供统一的日志记录功能
- **枚举**:
  - `LogLevel`: 日志级别（Trace, Debug, Info, Warn, Error, Fatal）
- **委托**:
  - `LogDispatcherCallback`: 日志分发回调委托
- **主要方法**:
  - `Log(LogLevel level, string message)`: 记录日志
  - `Initialize(string logFileName)`: 初始化日志服务
  - `RegisterLogCallback(LogDispatcherCallback callback)`: 注册日志回调（P/Invoke）

#### Events 命名空间

##### SystemStatsEventArgs
- **位置**: `IronSight.Interop/Events/SystemStatsEventArgs.cs`
- **继承**: `EventArgs`
- **功能**: 系统统计事件参数
- **属性**:
  - `CpuUsage`: CPU使用率
  - `DiskReadRate`: 磁盘读取速率
  - `DiskWriteRate`: 磁盘写入速率
- **主要方法**:
  - `SystemStatsEventArgs(double cpu, double diskRead, double diskWrite)`: 构造函数

#### Services 命名空间

##### ClipboardService
- **位置**: `IronSight.Interop/Services/ClipboardService.cs`
- **继承**: `IDisposable`
- **功能**: 剪贴板服务，管理剪贴板监听和事件处理
- **事件**:
  - `ClipboardChanged`: 剪贴板变化事件
- **主要方法**:
  - `Start()`: 启动剪贴板监听
  - `Stop()`: 停止剪贴板监听
  - `OnNativeClipboardChanged()`: 原生剪贴板变化回调
  - `Dispose()`: 释放资源

##### SystemMonitorService
- **位置**: `IronSight.Interop/Services/SystemMonitorService.cs`
- **继承**: `IDisposable`
- **功能**: 基础系统监控服务，定期获取系统性能数据
- **事件**:
  - `StatsUpdated`: 统计数据更新事件
- **主要方法**:
  - `SystemMonitorService()`: 构造函数，初始化系统监控
  - `Start()`: 启动监控
  - `Stop()`: 停止监控
  - `OnTimerElapsed(object sender, ElapsedEventArgs e)`: 定时器事件处理
  - `Dispose()`: 释放资源

##### NetworkService ⭐ **新增重要组件**
- **位置**: `IronSight.Interop/Services/NetworkService.cs`
- **继承**: `IDisposable`
- **功能**: 网络监控服务，提供高性能的网络连接监控功能
- **核心特性**:
  - 原生C++互操作，高性能连接获取
  - 预分配缓冲区，避免频繁GC
  - 支持TCP/UDP协议监控
  - 结构体大小验证确保跨语言兼容
- **主要方法**:
  - `NetworkService()`: 构造函数，初始化原生监控器
  - `Refresh()`: 刷新所有网络连接信息
  - `RefreshTcp()`: 仅刷新TCP连接
  - `RefreshUdp()`: 仅刷新UDP连接
  - `GetConnections()`: 获取连接信息只读跨度
  - `GetConnectionsList()`: 获取连接信息列表
  - `UpdateTickRate(TimeSpan tickRate)`: 更新采样频率
- **内存管理**:
  - 自动缓冲区扩容
  - GCHandle固定内存
  - 1.5倍增长因子
  - 默认2048连接容量

##### SystemMonitorServiceEx ⭐ **新增重要组件**
- **位置**: `IronSight.Interop/Services/SystemMonitorServiceEx.cs`
- **继承**: `IDisposable`
- **功能**: 高级系统监控服务扩展版，提供深度进程分析和系统性能快照
- **更新频率**: 可配置（默认2秒）
- **事件**:
  - `ProcessListUpdated`: 进程列表更新事件
  - `GlobalSnapshotUpdated`: 全局系统快照更新事件
- **主要方法**:
  - `SystemMonitorServiceEx()`: 构造函数，初始化扩展监控
  - `Start()`: 启动扩展监控
  - `Stop()`: 停止监控
  - `GetDetailedProcessList()`: 获取详细进程列表
  - `GetSystemPerformanceSnapshot()`: 获取系统性能快照
  - `TerminateSelectedProcess(uint pid)`: 终止指定进程
  - `OnTimerElapsed(object sender, ElapsedEventArgs e)`: 定时器事件处理
  - `Dispose()`: 释放资源

#### Native 命名空间

##### Clipboard 命名空间

###### ClipboardMethods
- **位置**: `IronSight.Interop/Native/Clipboard/ClipboardMethods.cs`
- **功能**: 剪贴板相关的P/Invoke方法封装
- **委托**:
  - `OnClipboardChangedCallback`: 剪贴板变化回调委托
- **主要方法**:
  - `StartClipboardListener(OnClipboardChangedCallback callback)`: 启动剪贴板监听（P/Invoke）
  - `StopClipboardListener()`: 停止剪贴板监听（P/Invoke）

##### Memory 命名空间

###### MemoryManager
- **位置**: `IronSight.Interop/Native/Memory/MemoryManager.cs`
- **功能**: 内存管理器，提供进程内存信息获取功能
- **主要方法**:
  - `GetRankedProcesses(int limit)`: 获取按内存使用量排序的进程列表

###### MemoryMethods
- **位置**: `IronSight.Interop/Native/Memory/MemoryMethods.cs`
- **功能**: 内存相关的P/Invoke方法封装
- **结构体**:
  - `CleanupResult`: 清理结果结构体
  - `ProcessInfo`: 进程信息结构体
- **主要方法**:
  - `EmptyWorkingSet(IntPtr hProcess)`: 清空进程工作集（P/Invoke）
  - `GetTopMemoryConsumers(ProcessInfo[] buffer, int maxCount)`: 获取内存占用最高的进程（P/Invoke）
  - `CleanSystemMemory()`: 清理系统内存（P/Invoke）

##### Network 命名空间 ⭐ **新增重要模块**

###### NetworkMethods
- **位置**: `IronSight.Interop/Native/Network/NetworkMethods.cs`
- **功能**: 网络监控相关的P/Invoke方法封装
- **枚举**:
  - `ConnectionState`: TCP连接状态枚举（Unknown, Closed, Listen, Established等）
  - `ProtocolType`: 网络协议类型枚举（Unknown, Tcp, Udp）
- **主要方法**:
  - `NetworkMonitor_Create()`: 创建网络监控器（P/Invoke）
  - `NetworkMonitor_Destroy()`: 销毁网络监控器（P/Invoke）
  - `NetworkMonitor_Refresh()`: 刷新所有网络连接（P/Invoke）
  - `NetworkMonitor_RefreshTcp()`: 仅刷新TCP连接（P/Invoke）
  - `NetworkMonitor_RefreshUdp()`: 仅刷新UDP连接（P/Invoke）
  - `NetworkMonitor_GetConnectionCount()`: 获取连接数量（P/Invoke）
  - `NetworkMonitor_CopyConnections()`: 复制连接数据（P/Invoke）
  - `NetworkConnectionInfo_GetSize()`: 获取结构体大小（P/Invoke）
  - `NetworkMonitor_SetUpdateInterval()`: 设置更新间隔（P/Invoke）

##### System 命名空间

###### SystemMethods ⭐ **功能最丰富的模块**
- **位置**: `IronSight.Interop/Native/System/SystemMethods.cs`
- **功能**: 扩展系统监控相关的P/Invoke方法封装
- **结构体**:
  - `SystemPerformanceSnapshot`: 系统性能快照结构体（包含CPU温度、内存、进程数等）
  - `ProcessDetailInfo`: 详细进程信息结构体（8字节对齐，解决WPF绑定问题）
- **主要方法**:
  - **基础监控**:
    - `InitializeSystemMonitor()`: 初始化系统监控（P/Invoke）
    - `UpdateSystemStats()`: 更新系统统计（P/Invoke）
    - `GetCpuUsage()`: 获取CPU使用率（P/Invoke）
    - `GetDiskReadRate()`: 获取磁盘读取速率（P/Invoke）
    - `GetDiskWriteRate()`: 获取磁盘写入速率（P/Invoke）
    - `CleanupSystemMonitor()`: 清理系统监控（P/Invoke）
  - **扩展监控**:
    - `InitializeSystemMethods()`: 初始化扩展系统方法（P/Invoke）
    - `GetSystemPerformanceSnapshot()`: 获取系统性能快照（P/Invoke）
    - `GetDetailedProcessList(ProcessDetailInfo[] buffer, int maxCount)`: 获取详细进程列表（P/Invoke）
    - `CleanupSystemMethods()`: 清理扩展系统方法（P/Invoke）
  - **进程管理**:
    - `TerminateSelectedProcess(uint pid)`: 终止指定进程（P/Invoke）

### IronSight 命名空间

##### Program
- **位置**: `IronSight/Program.cs`
- **功能**: 简单的控制台程序入口点
- **主要方法**:
  - `Main(string[] args)`: 主函数，输出"Hello, World!"

### IronSight.Extensions.Py 命名空间

##### IronSight.Extensions.Py
- **位置**: `IronSight.Extensions.Py/IronSight.Extensions.Py.py`
- **功能**: Python扩展模块（当前为空文件，预留扩展能力）
- **项目配置**: Python 3.13，使用Visual Studio Python Tools

## C++ 代码分析

### Utils 命名空间

#### 枚举和委托

##### LogLevel
- **位置**: `IronSight.Core.Native/Utilities.h`
- **类型**: `enum class`
- **功能**: 日志级别枚举
- **值**:
  - `Trace = 0`: 跟踪级别
  - `Debug = 1`: 调试级别
  - `Info = 2`: 信息级别
  - `Warn = 3`: 警告级别
  - `Error = 4`: 错误级别
  - `Fatal = 5`: 致命错误级别

##### LogDispatcherCallback
- **位置**: `IronSight.Core.Native/Utilities.h`
- **类型**: `typedef`
- **功能**: 日志分发回调函数指针类型
- **签名**: `void(__stdcall* LogDispatcherCallback)(LogLevel level, const char* message)`

#### 导出函数

##### RegisterLogCallback
- **位置**: `IronSight.Core.Native/Utilities.cpp`
- **功能**: 注册日志分发回调函数
- **参数**: `LogDispatcherCallback callback` - 回调函数指针
- **实现**: 保存全局回调函数指针

##### DebugPrintEx
- **位置**: `IronSight.Core.Native/Utilities.cpp`
- **功能**: 格式化打印日志到调试输出和回调
- **参数**: 
  - `LogLevel level` - 日志级别
  - `const char* format` - 格式化字符串
  - `...` - 可变参数
- **实现**: 使用vsnprintf格式化，输出到调试器和回调

##### EnableDebugPrivilege
- **位置**: `IronSight.Core.Native/Utilities.cpp`
- **功能**: 启用或禁用当前进程的调试权限
- **参数**: `BOOL enableFlag` - 启用标志
- **返回值**: `BOOL` - 成功返回非零，失败返回零
- **实现**: 通过OpenProcessToken、LookupPrivilegeValue和AdjustTokenPrivileges实现

#### 便捷宏

- **LOG_INFO**: 信息级别日志宏
- **LOG_ERROR**: 错误级别日志宏
- **LOG_WARN**: 警告级别日志宏
- **LOG_FATAL**: 致命错误级别日志宏
- **LOG_TRACE**: 跟踪级别日志宏（仅在Debug模式下）
- **LOG_DEBUG**: 调试级别日志宏（仅在Debug模式下）

### IronSight::Core::Native::Clipboard 命名空间

#### ClipboardListener 类

- **位置**: `IronSight.Core.Native/Clipboard/ClipboardListener.h` 和 `ClipboardListener.cpp`
- **功能**: 剪贴板监听器，监听系统剪贴板变化
- **静态成员**:
  - `_Callback`: 剪贴板变化回调函数
  - `_hMessageWindow`: 消息窗口句柄
  - `_ListenerThread`: 监听线程
  - `_IsRunning`: 运行状态标志
- **主要方法**:
  - `ClipboardWndProc(HWND hwnd, UINT msg, WPARAM wParam, LPARAM lParam)`: 窗口过程，处理剪贴板消息
  - `ListenerThreadProc()`: 监听线程入口函数
  - `StartClipboardListener(OnClipboardChangedCallback callback)`: 启动剪贴板监听
  - `StopClipboardListener()`: 停止剪贴板监听

#### 导出函数

- **StartClipboardListener**: 启动剪贴板监听的C接口
- **StopClipboardListener**: 停止剪贴板监听的C接口

### IronSight::Core::Native::Memory 命名空间

#### 结构体

##### CleanupResult
- **位置**: `IronSight.Core.Native/Memory/MemoryOptimizer.h`
- **功能**: 内存清理结果
- **成员**:
  - `DWORD ProcessedProcesses`: 处理的进程数
  - `long long TotalBytesReleased`: 释放的总字节数

##### ProcessInfo
- **位置**: `IronSight.Core.Native/Memory/MemoryOptimizer.h`
- **功能**: 进程信息
- **成员**:
  - `uint32_t Pid`: 进程ID
  - `double WorkingSetMB`: 工作集大小（MB）
  - `char Name[260]`: 进程名称

#### MemoryOptimizer 类

- **位置**: `IronSight.Core.Native/Memory/MemoryOptimizer.h` 和 `MemoryOptimizer.cpp`
- **功能**: 内存优化器，提供内存清理和进程信息获取功能
- **主要方法**:
  - `GetTopMemoryConsumers(int topN)`: 获取内存占用最高的前N个进程
  - `ExecuteGlobalCleanup()`: 执行全局内存清理

#### 导出函数

- **CleanSystemMemory**: 清理系统内存的C接口
- **GetTopMemoryConsumers**: 获取内存占用最高进程的C接口

### IronSight::Core::Native::Network 命名空间 ⭐ **新增重要模块**

#### 枚举和结构体

##### ConnectionState
- **位置**: `IronSight.Core.Native/NetworkMonitor.h`
- **类型**: `enum class`
- **功能**: TCP连接状态枚举
- **值**:
  - `Unknown = 0`: 未知状态
  - `Closed = 1`: 已关闭
  - `Listen = 2`: 监听中
  - `SynSent = 3`: SYN已发送
  - `SynReceived = 4`: SYN已接收
  - `Established = 5`: 已建立
  - `FinWait1 = 6`: FIN等待1
  - `FinWait2 = 7`: FIN等待2
  - `CloseWait = 8`: 关闭等待
  - `Closing = 9`: 正在关闭
  - `LastAck = 10`: 最后ACK
  - `TimeWait = 11`: 时间等待
  - `DeleteTcb = 12`: 删除TCB

##### ProtocolType
- **位置**: `IronSight.Core.Native/NetworkMonitor.h`
- **类型**: `enum class`
- **功能**: 网络协议类型枚举
- **值**:
  - `Unknown = 0`: 未知协议
  - `Tcp = 1`: TCP协议
  - `Udp = 2`: UDP协议

##### NetworkConnectionInfo
- **位置**: `IronSight.Core.Native/NetworkMonitor.h`
- **类型**: `struct` (1字节对齐)
- **功能**: 网络连接信息结构体，用于跨边界传输
- **成员**:
  - `uint32_t LocalAddress`: 本地IP地址（网络字节序）
  - `uint32_t RemoteAddress`: 远程IP地址（网络字节序）
  - `uint16_t LocalPort`: 本地端口
  - `uint16_t RemotePort`: 远程端口
  - `ConnectionState State`: 连接状态
  - `ProtocolType Protocol`: 协议类型
  - `uint32_t ProcessId`: 进程ID
  - `uint64_t Reserved`: 保留字段，用于扩展
- **大小**: 32字节（静态断言验证）

#### NetworkMonitor 类

- **位置**: `IronSight.Core.Native/NetworkMonitor.h` 和 `NetworkMonitor.cpp`
- **功能**: 高性能网络监控器类
- **核心特性**:
  - 线程安全的连接信息获取
  - 预分配缓冲区避免频繁内存分配
  - 支持TCP和UDP协议监控
  - 使用Windows IP Helper API
- **主要方法**:
  - `NetworkMonitor()`: 构造函数，初始化缓冲区
  - `~NetworkMonitor()`: 析构函数
  - `Refresh()`: 刷新所有网络连接信息
  - `RefreshTcp()`: 仅刷新TCP连接
  - `RefreshUdp()`: 仅刷新UDP连接
  - `GetConnectionCount()`: 获取连接数量
  - `GetConnections()`: 获取连接数据指针
  - `CopyConnectionsTo()`: 复制连接到外部缓冲区
- **私有方法**:
  - `RefreshTcpConnectionsInternal()`: 内部TCP连接刷新
  - `RefreshUdpConnectionsInternal()`: 内部UDP连接刷新
- **常量**:
  - `InitialBufferSize = 65536`: 初始缓冲区大小
  - `InitialConnectionCapacity = 1024`: 初始连接容量

#### 导出函数

- **NetworkMonitor_Create**: 创建网络监控器实例的C接口
- **NetworkMonitor_Destroy**: 销毁网络监控器实例的C接口
- **NetworkMonitor_Refresh**: 刷新所有连接的C接口
- **NetworkMonitor_RefreshTcp**: 刷新TCP连接的C接口
- **NetworkMonitor_RefreshUdp**: 刷新UDP连接的C接口
- **NetworkMonitor_GetConnectionCount**: 获取连接数量的C接口
- **NetworkMonitor_CopyConnections**: 复制连接数据的C接口
- **NetworkConnectionInfo_GetSize**: 获取结构体大小的C接口

#### 技术实现细节

1. **高性能实现**:
   - 使用`GetExtendedTcpTable`和`GetExtendedUdpTable` API
   - 动态缓冲区扩展机制
   - 预分配内存减少GC压力

2. **线程安全**:
   - `std::mutex`保护共享数据
   - 线程局部存储的监控器实例

3. **内存管理**:
   - 预分配向量避免频繁重新分配
   - 自动缓冲区扩容（+4096字节增量）

### IronSight::Core::Native::System 命名空间

#### SystemMonitor 类

- **位置**: `IronSight.Core.Native/System/SystemMonitor.h` 和 `SystemMonitor.cpp`
- **功能**: 基础系统监控器，使用PDH API获取系统性能数据
- **静态成员**:
  - `_hQuery`: PDH查询句柄
  - `_hCpuCounter`: CPU计数器句柄
  - `_hDiskReadCounter`: 磁盘读取计数器句柄
  - `_hDiskWriteCounter`: 磁盘写入计数器句柄
- **主要方法**:
  - `InitializeSystemMonitor()`: 初始化系统监控
  - `UpdateSystemStats()`: 更新系统统计
  - `GetCpuUsage()`: 获取CPU使用率
  - `GetDiskReadRate()`: 获取磁盘读取速率
  - `GetDiskWriteRate()`: 获取磁盘写入速率
  - `CleanupSystemMonitor()`: 清理系统监控

#### SystemMethods 类 ⭐ **新增重要组件**
- **位置**: `IronSight.Core.Native/SystemMethods.h` 和 `SystemMethods.cpp`
- **功能**: 扩展系统监控器，提供深度进程分析和系统性能快照
- **核心结构体**:
  - `SystemPerformanceSnapshot`: 系统性能快照（包含CPU温度、内存使用、进程数等）
  - `ProcessDetailInfo`: 详细进程信息（PID、CPU、内存、磁盘IO等）
  - `ProcessHistory`: 进程历史数据（用于计算速率）
- **静态成员**:
  - `_hQuery`: PDH查询句柄
  - `_hCpuCounter`: CPU计数器句柄
  - `_hDiskReadCounter`: 磁盘读取计数器句柄
  - `_hDiskWriteCounter`: 磁盘写入计数器句柄
  - `_processHistoryMap`: 进程历史映射
- **主要方法**:
  - `Initialize()`: 初始化扩展系统监控
  - `GetPerformanceSnapshot()`: 获取系统性能快照
  - `GetDetailedProcessList()`: 获取详细进程列表（支持CPU和磁盘IO速率计算）
  - `TerminateSelectedProcess(uint pid)`: 终止指定进程
  - `Cleanup()`: 清理资源

#### 导出函数

**基础监控函数**:
- **InitializeSystemMonitor**: 初始化系统监控的C接口
- **UpdateSystemStats**: 更新系统统计的C接口
- **GetCpuUsage**: 获取CPU使用率的C接口
- **GetDiskReadRate**: 获取磁盘读取速率的C接口
- **GetDiskWriteRate**: 获取磁盘写入速率的C接口
- **CleanupSystemMonitor**: 清理系统监控的C接口

**扩展监控函数**:
- **InitializeSystemMethods**: 初始化扩展系统方法的C接口
- **GetSystemPerformanceSnapshot**: 获取系统性能快照的C接口
- **GetDetailedProcessList**: 获取详细进程列表的C接口
- **TerminateSelectedProcess**: 终止指定进程的C接口
- **CleanupSystemMethods**: 清理扩展系统方法的C接口

### DLL 入口点

#### DllMain 函数

- **位置**: `IronSight.Core.Native/DllMain.cpp`
- **功能**: DLL入口点，处理进程附加和分离
- **参数**:
  - `HMODULE hModule`: 模块句柄
  - `DWORD ul_reason_for_call`: 调用原因
  - `LPVOID lpReserved`: 保留参数
- **实现**: 
  - `DLL_PROCESS_ATTACH`: 记录日志，禁用线程库调用
  - 其他情况：不执行特殊操作

### 预编译头文件

#### pch.h 和 pch.cpp

- **位置**: `IronSight.Core.Native/pch.h` 和 `pch.cpp`
- **功能**: 预编译头文件，提高编译性能
- **包含**: `framework.h` 和其他常用头文件

#### framework.h

- **位置**: `IronSight.Core.Native/framework.h`
- **功能**: 框架头文件，定义Windows版本和包含系统头文件
- **定义**:
  - `WIN32_LEAN_AND_MEAN`: 排除极少使用的Windows内容
  - `WINVER` 和 `_WIN32_WINNT`: 设置Windows版本为0x0601（Windows 7）
- **包含库**:
  - `Pdh.lib`: 性能计数器API
  - `IPHLPAPI.lib`: IP帮助API
  - `advapi32.lib`: 高级API

## 互操作设计

### C# 与 C++ 通信机制

1. **P/Invoke**: 用于C#调用C++导出函数
2. **回调函数**: 用于C++向C#发送通知
3. **结构体封送**: 用于复杂数据类型的传递，支持8字节对齐

### 日志系统集成

1. **C++日志宏**: 在C++中使用LOG_*宏记录日志
2. **日志回调**: C++通过回调将日志传递给C#
3. **日志分发**: C#统一处理日志输出到文件和控制台

### 权限管理

1. **初始化流程**: 应用启动时调用`DllMain.Initialize()`
2. **权限提升**: 通过`EnableDebugPrivilege`获取调试权限
3. **错误处理**: 记录权限提升失败的情况

### 双重服务架构

1. **基础监控服务**: `SystemMonitorService` 提供基本的CPU和磁盘IO监控
2. **高级扩展服务**: `SystemMonitorServiceEx` 提供深度进程分析和系统性能快照
3. **数据同步**: 两个服务可以并行运行，提供不同粒度的监控数据

## 关键技术点

### 剪贴板监听

1. **隐藏窗口**: 创建隐藏的消息窗口接收剪贴板通知
2. **格式监听**: 使用`AddClipboardFormatListener`API
3. **线程安全**: 在独立线程中处理剪贴板事件

### 系统监控

1. **PDH API**: 使用Windows性能数据助手API
2. **计数器**: 创建CPU和磁盘IO计数器
3. **数据格式化**: 将原始数据转换为百分比和速率
4. **进程历史跟踪**: 使用映射表存储进程历史数据，计算速率变化

### 内存优化

1. **工作集清理**: 使用`EmptyWorkingSet`API
2. **进程枚举**: 使用`EnumProcesses`获取进程列表
3. **内存统计**: 使用`GetProcessMemoryInfo`获取内存信息

### 进程管理

1. **进程终止**: 使用`TerminateProcess`API
2. **权限检查**: 确保有足够权限终止目标进程
3. **差量更新**: 使用`IsVisuallyDifferent()`方法优化UI性能

### 性能优化

1. **8字节对齐**: ProcessDetailInfo结构体使用Pack=8确保与C++兼容
2. **Unicode处理**: SystemMethods中显式处理Unicode到ANSI转换
3. **内存管理**: 进程历史映射的自动清理机制

## 项目配置与构建

### 目标框架
- **C# 项目**: .NET 6.0-windows
- **C++ 项目**: Visual Studio 2022 (v145工具链)
- **Python 项目**: Python 3.13

### 项目依赖关系
1. **IronSight.App.UI** 依赖：
   - IronSight.Interop
   - IronSight.Core.Native
2. **IronSight.Interop** 依赖：
   - IronSight.Core.Native
3. **IronSight.Extensions.Py**: 独立项目（当前为空）

### 构建配置
- **平台**: x64（主要），Win32（兼容性）
- **配置**: Debug（开发）/ Release（发布）
- **C++ 标准**: C++17/20
- **字符集**: Unicode

## 功能实现状态

### ✅ 完全实现的功能
1. **系统监控 (System Sentinel)**: CPU使用率、磁盘读写速率监控
2. **剪贴板管理 (Ethereal Clipboard)**: 剪贴板监听、历史记录、去重
3. **内存优化 (Memory Alchemist)**: 内存清理、进程内存排序
4. **进程管理**: 进程列表显示、详细信息、搜索、终止功能
5. **网络监控 (Network Guardian)**: ⭐ **完整实现** - TCP/UDP连接实时监控、进程关联、状态可视化、过滤搜索
6. **设置管理 (Settings Hub)**: ⭐ **完整实现** - 用户配置持久化、主题切换、采样频率调整、多语言支持
7. **现代化UI**: Fluent Design深色主题、Mica效果、平滑动画

### 🚧 占位符功能
1. **关于页面**: AboutView.xaml仅为占位符

### 📋 预留扩展
1. **Python扩展**: IronSight.Extensions.Py项目为空，预留脚本化能力
2. **插件系统**: 架构支持未来扩展

## 总结

IronSight项目采用了清晰的分层架构，通过C++原生组件提供高性能的系统监控功能，通过C# WPF提供现代化的用户界面。项目使用了多种Windows API和.NET技术，实现了系统监控、剪贴板管理、内存优化和进程管理等功能。

**关键特点**:
1. **混合语言架构**: C# + C++ 提供性能与可维护性的平衡
2. **MVVM模式**: 严格的视图与逻辑分离
3. **Fluent Design**: 现代化的用户界面设计
4. **双重监控架构**: 基础监控 + 高级扩展监控
5. **深度进程分析**: 详细的进程性能数据和管理功能
6. **网络连接监控**: ⭐ **高性能TCP/UDP连接实时监控**，支持进程关联和状态可视化
7. **用户配置系统**: ⭐ **完整的配置持久化**，支持主题切换、采样频率调整
8. **统一日志系统**: 跨语言的日志集成
9. **高性能网络引擎**: 原生C++实现，预分配缓冲区，支持大规模连接监控
10. **智能缓存机制**: 进程名称缓存、差量更新优化，提升UI响应性能

代码结构良好，遵循了面向对象设计原则和最佳实践，为未来的功能扩展奠定了坚实的基础。

---

*最后更新: 基于 2025年12月25日 的完整项目代码分析*