#pragma once

namespace Utils
{ // 建议放入命名空间，保持一致性

	// 显式继承 uint32_t 确保与 C# UInt32 对齐
	enum class LogLevel : uint32_t
	{
		Trace = 0,
		Debug = 1,
		Info = 2,
		Warn = 3,
		Error = 4,
		Fatal = 5
	};

	// 回调签名：使用 __stdcall (WinAPI 标准) 确保堆栈平衡
	typedef void(__stdcall* LogDispatcherCallback)(LogLevel level, const char* message);

	extern "C"
	{
		/// <summary>
		/// 注册日志分发回调函数，使库在产生日志时调用该回调。该函数通过 __declspec(dllexport) 导出（Windows DLL）。
		/// </summary>
		/// <param name="callback">要注册的回调函数（类型为 LogDispatcherCallback）。当有日志消息需要分发时将调用该回调。调用时机、线程语境和生命周期由实现决定。</param>
		__declspec(dllexport) void RegisterLogCallback(LogDispatcherCallback callback);

		/// <summary>
		/// 导出函数：打印日志到Debug Output中，具有扩展功能。
		/// </summary>
		/// <param name="level">指示日志的级别。</param>
		/// <param name="format">用于格式化日志的字符串。</param>
		/// <param name="...">可变参数，用于格式化日志的变量。</param>
		/// <returns>返回输出的字符串长度，如果函数失败返回-1。</returns>
		__declspec(dllexport) int DebugPrintEx(LogLevel level, const char* format, ...);

		/// <summary>
		/// 导出函数：启用或禁用当前进程的调试权限（用于允许或禁止调试/访问其他进程）。
		/// </summary>
		/// <param name="enableFlag">非零值表示启用调试权限；零表示禁用。</param>
		/// <returns>操作成功返回非零（TRUE）；失败返回零（FALSE）。</returns>
		__declspec(dllexport) BOOL EnableDebugPrivilege(BOOL enableFlag);
	}
}

// 定义便捷宏，方便在 C++ 内部调用
#define LOG_INFO(fmt, ...)  Utils::DebugPrintEx(Utils::LogLevel::Info,  fmt, ##__VA_ARGS__)
#define LOG_ERROR(fmt, ...) Utils::DebugPrintEx(Utils::LogLevel::Error, fmt, ##__VA_ARGS__)
#define LOG_WARN(fmt, ...)  Utils::DebugPrintEx(Utils::LogLevel::Warn,  fmt, ##__VA_ARGS__)
#define LOG_FATAL(fmt, ...) Utils::DebugPrintEx(Utils::LogLevel::Fatal, fmt, ##__VA_ARGS__)


#ifdef _DEBUG
#define LOG_TRACE(fmt, ...) Utils::DebugPrintEx(Utils::LogLevel::Trace, fmt, ##__VA_ARGS__)
#define LOG_DEBUG(fmt, ...) Utils::DebugPrintEx(Utils::LogLevel::Debug, fmt, ##__VA_ARGS__)
#else
#define LOG_TRACE(fmt, ...) ((void)0)
#define LOG_DEBUG(fmt, ...) ((void)0)
#endif