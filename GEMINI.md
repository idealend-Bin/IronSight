# IronSight (Ethereal) - Project Context

**IronSight** is a high-performance system monitoring and optimization utility that blends a modern "Fluent Design" UI with "hardcore" low-level system access. It is designed to be the "neural center" of the OS, offering advanced process management, memory optimization, and clipboard history features.

## 🏗 Architecture

The project follows a mixed-language architecture to balance UI aesthetics with system-level performance.

*   **UI Layer (`IronSight.App.UI`)**:
    *   **Framework**: WPF (.NET 6).
    *   **Design**: Fluent Design System (Mica/Acrylic effects), MVVM pattern.
    *   **Responsibility**: User interaction, data visualization (charts), navigation.

*   **Interop Layer (`IronSight.Interop`)**:
    *   **Language**: C#.
    *   **Framework**: .NET 6.
    *   **Responsibility**: Marshaling data between Managed (C#) and Unmanaged (C++) worlds. Handles callback registration (e.g., Logging).

*   **Core Layer (`IronSight.Core.Native`)**:
    *   **Language**: C++ (Native Windows API using C++17/20).
    *   **Toolset**: Visual Studio 2022 (v145).
    *   **Compatibility**: Windows 7+ (`WINVER 0x0601`).
    *   **Responsibility**: High-performance data collection (PDH, PSAPI), memory optimization (`EmptyWorkingSet`), global hooks.

*   **Extensions (`IronSight.Extensions.Py`)**:
    *   **Language**: Python 3.13.
    *   **Responsibility**: Scriptable extensions and plugins.

## 📂 Key Files & Directories

*   `IronSight.slnx`: Visual Studio Solution file for the entire project.
*   `IronSightRPD.md`: **Product Design Document** (contains detailed feature specs).
*   `IronSight.App.UI/`: Main WPF application project.
    *   `MainWindow.xaml`: The main application window.
    *   `ViewModels/`: Contains the ViewModels for the MVVM pattern.
*   `IronSight.Core.Native/`: C++ DLL source code.
    *   `framework.h`: Core Windows API headers.
    *   `*.cpp / *.h`: Header and source files for core features (Clipboard, Memory, System).
*   `IronSight.Interop/`: C# wrapper library project.
    *   `LoggerService.cs`: Centralized logging service for Native -> Managed dispatch.
*   `IronSight.Extensions.Py/`: Python project for scripting.

## 🚀 Building & Running

**Prerequisites:**
*   Visual Studio 2022 (or later).
*   Workloads: .NET Desktop Development, Desktop Development with C++, Python Development.

**Build:**
This is a mixed-language solution. Build it using Visual Studio or MSBuild.

```powershell
# Restore NuGet packages for all C# projects
dotnet restore IronSight.slnx

# Build the entire solution using MSBuild
# (Recommended over `dotnet build` to ensure C++ project compatibility)
msbuild IronSight.slnx /p:Configuration=Debug /p:Platform=x64
```

**Run:**
Set `IronSight.App.UI` as the startup project in Visual Studio or run the executable directly from `IronSight.App.UI\bin\Debug\net6.0-windows\`.

## 💻 Development Conventions

*   **Interop Safety**: When passing delegates to C++, **always** keep a static reference to prevent the Garbage Collector from collecting the delegate while C++ holds the function pointer (see `LoggerService.cs`).
*   **Compatibility**: Native C++ code must compile and run on Windows 7. Avoid APIs introduced in newer Windows versions unless there is a runtime check and a proper fallback.
*   **Logging**: All logging, including from the native C++ library, should be routed through the `IronSight.Interop.LoggerService`.
*   **UI Threading**: Callbacks from the native library will arrive on background threads. Use `Application.Current.Dispatcher` to marshal any UI updates to the main thread to prevent exceptions.

## 🛠 Core Features & Status

The following are the primary features as defined in the RPD.

1.  **System Sentinel**: CPU/IO monitoring, "High Temperature" warnings.
2.  **Memory Alchemist**: Smart memory compression, process whitelist support.
3.  **Ethereal Clipboard**: Clipboard history with privacy masking and favorites.

### Current Development Tasks (from RPD)
*   `[ ]` Implement the C++-side clipboard listener (`WM_CLIPBOARDUPDATE`).
*   `[ ]` Create the `ClipboardItemViewModel` in the WPF project.
*   `[ ]` Optimize real-time performance chart rendering to prevent UI stuttering at high refresh rates.