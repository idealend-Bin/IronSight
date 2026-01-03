#include <pch.h>
#include "Utilities.h"

namespace Utils 
{
    static LogDispatcherCallback g_LogCallback = nullptr;

    extern "C" BOOL EnableDebugPrivilege(BOOL enableFlag) 
    {
        HANDLE hToken;
        if (!OpenProcessToken(GetCurrentProcess(), TOKEN_ADJUST_PRIVILEGES | TOKEN_QUERY, &hToken))
            return GetLastError();

        LUID luid;
        if (!LookupPrivilegeValue(NULL, SE_DEBUG_NAME, &luid)) {
            CloseHandle(hToken);
            return GetLastError();
        }

        TOKEN_PRIVILEGES tp;
        tp.PrivilegeCount = 1;
        tp.Privileges[0].Luid = luid;
        // 根据传入的布尔值决定开启还是关闭
        tp.Privileges[0].Attributes = enableFlag ? SE_PRIVILEGE_ENABLED : 0;

        if (!AdjustTokenPrivileges(hToken, FALSE, &tp, sizeof(TOKEN_PRIVILEGES), NULL, NULL)) {
            DWORD err = GetLastError();
            CloseHandle(hToken);
            return err;
        }

        CloseHandle(hToken);
        return GetLastError() == ERROR_NOT_ALL_ASSIGNED ? ERROR_NOT_ALL_ASSIGNED : 0;
    }

    extern "C" void RegisterLogCallback(LogDispatcherCallback callback) 
    {
        g_LogCallback = callback;
    }

    extern "C" int DebugPrintEx(LogLevel level, const char* format, ...) 
    {
        if (!format) return -1;

        char buffer[4096]; // 足够大的缓冲区
        va_list args;
        va_start(args, format);
        int result = vsnprintf(buffer, sizeof(buffer), format, args);
        va_end(args);

        if (result > 0) 
        {
            // 1. 发送到系统调试器 (OutputDebugString)
            OutputDebugStringA(buffer);

            // 2. 发送到 C# 分发器
            if (g_LogCallback) 
            {
                g_LogCallback(level, buffer);
            }
        }
        return result;
    }
}