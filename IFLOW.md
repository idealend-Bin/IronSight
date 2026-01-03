# IronSight 项目概述

IronSight 是一个基于 .NET 6 和 WPF 的 Windows 高性能系统监控与优化工具，集成了 C++ 原生组件以提供系统级性能监控功能。项目融合了 Fluent Design 设计语言，打造现代化的视觉体验，同时通过混合语言架构确保高性能和系统级访问能力。

## 项目架构

该项目采用多模块混合语言架构，包含以下主要组件：

- **IronSight.App.UI**: 主 WPF 应用程序，提供基于 Fluent Design 的现代化用户界面
- **IronSight.Interop**: .NET 与原生代码的互操作层，负责托管与非托管代码之间的数据封送
- **IronSight.Core.Native**: C++ 原生 DLL，提供系统级功能（性能监控、内存优化、剪贴板监听）
- **IronSight.Extensions.Py**: Python 扩展模块（当前为空，预留脚本化扩展能力）
- **IronSight**: 额外的 .NET 项目模块，目前为简单的控制台程序

## 核心功能

1. **系统监控 (System Sentinel)**: 实时监控 CPU 使用率、磁盘读写速率（MB/s），支持逻辑核心视图和 IO 向量曲线
2. **剪贴板管理 (Ethereal Clipboard)**: 监听剪贴板变化，维护最近 50 条剪贴板历史记录，支持内容预览、去重和快速复制
3. **内存优化 (Memory Alchemist)**: 提供系统内存清理功能，支持标准模式和智能分析模式
4. **网络监控**: 实时网络流量监控（预留视图，当前功能待实现）
5. **进程管理**: 系统进程查看与管理功能（预留视图，当前功能待实现）
6. **现代化 UI**: 基于 WPF 的 Fluent Design 深色主题界面，包含 Mica/亚克力视觉效果、平滑动画和现代化控件
7. **命令模式**: 实现了 RelayCommand 和 RelayCommand<T> 模式支持 UI 交互

## 技术栈

- **前端**: WPF (.NET 6.0-windows) + Fluent Design 视觉框架
- **后端**: C# (.NET 6.0-windows)，采用 MVVM 模式
- **原生组件**: C++ (Visual Studio 2022, C++17/20)，使用 Windows API 和 PDH 性能计数器
- **Python 扩展**: Python 3.13 (可选，预留扩展接口)
- **构建工具**: MSBuild / dotnet build，支持 Visual Studio 2022 解决方案

## 构建和运行

### 环境要求

- **开发环境**: Visual Studio 2022（包含 .NET 桌面开发、C++ 桌面开发工作负荷）
- **运行时**: .NET 6.0 SDK，Windows 10/11 SDK (10.0)
- **可选**: Python 3.13（用于扩展模块开发）

### 构建步骤

1. **使用 Visual Studio 构建**:
   - 打开 `IronSight.slnx` 解决方案文件
   - 设置启动项目为 `IronSight.App.UI`
   - 选择配置（Debug/Release）和平台（x64）
   - 生成解决方案

2. **使用命令行构建（推荐使用 MSBuild）**:
   ```powershell
   # 恢复 NuGet 包
   dotnet restore IronSight.slnx
   
   # 使用 MSBuild 构建整个解决方案（确保 C++ 项目正确构建）
   msbuild IronSight.slnx /p:Configuration=Debug /p:Platform=x64
   
   # 或使用 dotnet build（可能无法正确处理 C++ 项目）
   dotnet build IronSight.slnx
   ```

3. **运行应用程序**:
   ```bash
   # 从命令行运行
   dotnet run --project IronSight.App.UI/IronSight.App.UI.csproj
   
   # 或直接运行生成的可执行文件
   IronSight.App.UI\bin\Debug\net6.0-windows\IronSight.App.UI.exe
   ```

### 自动化构建

原生组件包含后构建事件，会自动将生成的 DLL 复制到 WPF 应用程序的输出目录：
```
copy /Y "$(TargetDir)$(TargetName).dll" "$(SolutionDir)IronSight.App.UI\bin\$(Configuration)\$(TargetFramework)\net6.0-windows\"
```

## 开发约定

### 代码风格

- **C# 代码**: 使用隐式 using 和可空引用类型，遵循 Microsoft 命名约定
- **C++ 代码**: 使用 C++17/20 标准，预编译头文件 (pch.h)，Level 3-4 警告级别
- **架构模式**: 严格遵循 MVVM 模式进行 UI 开发，视图与逻辑分离
- **日志系统**: 所有日志（包括原生代码）通过 `LoggerService` 统一路由

### 项目结构（基于当前实际结构）

```
IronSight/
├── IronSight.App.UI/                  # WPF 主应用程序
│   ├── ViewModels/                    # 视图模型
│   │   └── MainViewModel.cs           # 主视图模型（包含 ClipboardItem、RelayCommand 定义）
│   ├── Views/                         # 用户控件视图
│   │   ├── DashboardView.xaml         # 仪表板视图
│   │   ├── ClipboardView.xaml         # 剪贴板历史视图
│   │   ├── MemoryCleanerView.xaml     # 内存优化视图
│   │   ├── NetworkMonitorView.xaml    # 网络监控视图（预留）
│   │   ├── ProcessManagerView.xaml    # 进程管理视图（预留）
│   │   ├── SettingsView.xaml          # 设置视图
│   │   ├── AboutView.xaml             # 关于视图
│   │   ├── *.xaml.cs                  # 各视图的代码后置文件
│   ├── Resources/
│   │   └── Styles.xaml                # Fluent Design 样式系统定义
│   ├── App.xaml                       # 应用程序入口
│   ├── MainWindow.xaml                # 主窗口定义
│   └── IronSight.App.UI.csproj        # 项目文件
├── IronSight.Interop/                 # 互操作层
│   ├── Services/                      # 服务层
│   │   ├── ClipboardService.cs        # 剪贴板服务
│   │   └── SystemMonitorService.cs    # 系统监控服务
│   ├── Native/                        # 原生方法封装
│   │   ├── Clipboard/
│   │   │   └── ClipboardMethods.cs    # 剪贴板相关 P/Invoke
│   │   ├── Memory/
│   │   │   ├── MemoryManager.cs
│   │   │   └── MemoryMethods.cs       # 内存管理相关 P/Invoke
│   │   └── System/
│   │       └── SystemMethods.cs       # 系统监控相关 P/Invoke
│   ├── Events/                        # 事件定义
│   │   └── SystemStatsEventArgs.cs    # 系统统计事件参数
│   ├── Core/                          # 核心功能
│   │   ├── DllMain.cs                 # DLL 入口
│   │   └── LoggerService.cs           # 统一的日志服务
│   └── IronSight.Interop.csproj       # 项目文件
├── IronSight.Core.Native/             # C++ 原生组件
│   ├── Clipboard/                     # 剪贴板功能
│   │   ├── ClipboardListener.cpp
│   │   └── ClipboardListener.h
│   ├── System/                        # 系统监控
│   │   ├── SystemMonitor.cpp
│   │   └── SystemMonitor.h
│   ├── Memory/                        # 内存优化
│   │   ├── MemoryOptimizer.cpp
│   │   └── MemoryOptimizer.h
│   ├── Common/                        # 通用工具函数
│   │   ├── Utilities.cpp
│   │   └── Utilities.h
│   ├── Utilities.cpp                  # 根目录工具函数（与 Common 目录可能存在重复）
│   ├── Utilities.h
│   ├── DllMain.cpp                    # DLL 入口点
│   ├── framework.h                    # 框架头文件
│   ├── pch.h                          # 预编译头文件
│   ├── pch.cpp                        # 预编译源文件
│   └── IronSight.Core.Native.vcxproj  # C++ 项目文件
├── IronSight.Extensions.Py/           # Python 扩展
│   ├── IronSight.Extensions.Py.py     # Python 脚本（当前为空）
│   └── IronSight.Extensions.Py.pyproj # Python 项目文件
├── IronSight/                         # 额外的 .NET 项目
│   ├── Program.cs                     # 简单的 "Hello, World!" 程序
│   └── IronSight.csproj               # 项目文件
├── IronSight.slnx                     # Visual Studio 解决方案文件
├── IFLOW.md                           # 项目文档（本文档）
├── GEMINI.md                          # 项目上下文文档
└── IronSightRPD.md                    # 产品设计文档（详细功能规格）
```

### 互操作设计

- **P/Invoke 通信**: 使用 DllImport 进行 C# 与 C++ 之间的函数调用
- **回调机制**: 原生回调通过委托封送到 .NET，使用静态引用防止 GC 回收
- **线程安全**: 原生回调运行在后台线程，UI 更新必须通过 `Application.Current.Dispatcher` 封送到主线程
- **事件驱动**: 使用事件驱动架构进行组件间通信，如 `SystemStatsEventArgs` 传递监控数据

## 关键文件说明

- **`IronSight.slnx`**: Visual Studio 解决方案文件，定义所有项目的依赖关系
- **`IronSight.App.UI/Views/MainWindow.xaml`**: 主窗口定义，包含导航框架和整体布局
- **`IronSight.App.UI/Resources/Styles.xaml`**: Fluent Design 样式系统，定义颜色、画刷、控件模板和动画
- **`IronSight.App.UI/ViewModels/MainViewModel.cs`**: 主视图模型，协调各服务，包含 `ClipboardItem` 模型和 `RelayCommand` 命令实现
- **`IronSight.Interop/Services/ClipboardService.cs`**: 剪贴板功能的核心实现，管理原生剪贴板监听
- **`IronSight.Interop/Services/SystemMonitorService.cs`**: 系统监控服务，收集 CPU 和磁盘性能数据
- **`IronSight.Interop/Core/LoggerService.cs`**: 统一的日志服务，支持从原生代码到托管代码的日志路由
- **`IronSight.Core.Native/Clipboard/ClipboardListener.h`**: 原生剪贴板监听接口定义
- **`IronSight.Core.Native/System/SystemMonitor.h`**: 原生系统监控接口定义

## 扩展开发

### 添加新功能的标准流程

1. **在原生层实现** (`IronSight.Core.Native`):
   - 在相应目录（System、Memory、Clipboard 或新建目录）添加 `.cpp`/`.h` 文件
   - 实现高性能的 Windows API 调用
   - 通过 `DebugPrintEx` 或回调函数将数据传递到托管层

2. **在互操作层添加包装** (`IronSight.Interop/Native`):
   - 在相应目录添加 C# 包装类
   - 使用 `DllImport` 声明原生函数
   - 定义委托类型用于回调
   - 添加适当的错误处理和类型转换

3. **创建服务层** (`IronSight.Interop/Services`):
   - 创建新的服务类，封装原生功能
   - 实现事件驱动接口，供 UI 层订阅
   - 处理线程封送和异常处理

4. **在 UI 层集成** (`IronSight.App.UI`):
   - 在 `MainViewModel` 中添加新属性和命令
   - 创建对应的视图（XAML 文件）
   - 更新导航系统（如果需要）
   - 遵循 Fluent Design 规范设计界面

5. **更新项目引用**:
   - 确保项目间引用正确
   - 测试构建和功能完整性

### Python 扩展

Python 扩屔模块当前为空，设计用于：
- **脚本化功能**: 通过 Python 脚本扩展应用功能
- **插件系统**: 支持第三方插件开发
- **自动化接口**: 提供自动化脚本接口

## 构建配置

### 平台支持

- **x64**: 主要支持平台，支持完整功能
- **x86**: 兼容性支持（Win32 配置）
- **Any CPU**: .NET 项目通用配置

### 配置类型

- **Debug**: 开发调试配置，包含完整调试信息，启用所有安全检查
- **Release**: 发布配置，启用优化，减小二进制体积

### C++ 编译设置

- **Debug x64**: C++17 标准，Level 4 警告级别，生成调试信息
- **Release x64**: C++20 标准，启用全程序优化（WholeProgramOptimization）
- **Win32 平台**: C++20 标准，支持 Unicode 字符集，兼容 Windows 7+

## 注意事项

1. **平台限制**: 该项目仅支持 Windows 平台，依赖 Windows API 和 PDH 性能计数器
2. **构建要求**: 原生组件需要 Visual Studio 2022 C++ 工具链（v145）
3. **权限要求**:
   - 应用程序需要适当的权限访问系统剪贴板
   - 性能计数器访问需要常规用户权限
   - 内存优化功能需要管理员权限才能完全生效
4. **兼容性**: 原生代码设计兼容 Windows 7+，使用 `WINVER 0x0601`
5. **剪贴板历史**: 限制为 50 条记录，自动清理旧记录，支持内容去重
6. **性能数据**: 系统监控数据以 MB/s 为单位显示磁盘读写速率
7. **UI 线程**: 所有从原生层回调的 UI 更新必须通过 `Dispatcher.Invoke` 封送到主线程
8. **Fluent Design**: 样式系统使用深色主题，支持 Mica/亚克力视觉效果，需要 Windows 10/11 以获得最佳体验

## 相关文档

- **`GEMINI.md`**: 项目上下文和技术架构概述
- **`IronSightRPD.md`**: 详细的产品设计文档，包含功能规格和设计规范
- **Visual Studio 解决方案**: 包含完整的项目配置和构建设置

---

*最后更新: 基于 2025年12月22日 的项目状态分析*