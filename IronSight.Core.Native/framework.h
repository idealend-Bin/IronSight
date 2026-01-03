#pragma once

#ifndef WIN32_LEAN_AND_MEAN
#define WIN32_LEAN_AND_MEAN
#endif

// 设置最低支持版本为 Win7 (0x0601) 或更高
#ifndef WINVER
#define WINVER 0x0601
#endif
#ifndef _WIN32_WINNT
#define _WIN32_WINNT 0x0601
#endif

// --- 关键修改：WinSock2 必须在 windows.h 之前 ---
#include <WinSock2.h>
#include <ws2tcpip.h>
#include <windows.h>
// ----------------------------------------------

#include <iphlpapi.h> // 现在包含它就不会有 _WS2IPDEF_ 错误了
#include <tcpestats.h>
#include <pdh.h>
#include <psapi.h>
#include <tlhelp32.h>
#include <fileapi.h>
#include <sysinfoapi.h>

// 标准库
#include <iostream>
#include <memory>
#include <string>
#include <vector>
#include <thread>
#include <atomic>

#pragma comment(lib, "ws2_32.lib")
#pragma comment(lib, "IPHLPAPI.lib")
#pragma comment(lib, "Pdh.lib")
#pragma comment(lib, "advapi32.lib")