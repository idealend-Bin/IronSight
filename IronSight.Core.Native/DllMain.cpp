// dllmain.cpp : 定义 DLL 应用程序的入口点。
#include <pch.h>
#include "Utilities.h"

HANDLE g_hModule = NULL;

BOOL APIENTRY DllMain( HMODULE hModule,
                       DWORD  ul_reason_for_call,
                       LPVOID lpReserved
                     )
{
    switch (ul_reason_for_call)
    {
    case DLL_PROCESS_ATTACH:
        LOG_INFO("IronSight Native Core Attached. Base Address: %p\n", hModule);
        g_hModule = hModule;
        // 性能优化：不希望这个 DLL 接收到线程创建/销毁的通知
        DisableThreadLibraryCalls(hModule);
    case DLL_THREAD_ATTACH:
    case DLL_THREAD_DETACH:
    case DLL_PROCESS_DETACH:
        break;
    }
    return TRUE;
}

