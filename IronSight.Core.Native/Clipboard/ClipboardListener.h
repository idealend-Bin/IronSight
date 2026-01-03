#pragma once


typedef void(__stdcall* OnClipboardChangedCallback)();
namespace IronSight::Core::Native::Clipboard
{
    class ClipboardListener
    {
        // Callback type: void(int formatId) - simplified, or just void()
        // The requirement says "Push updates". We can just notify "Changed" and let C# read the clipboard (easier for marshaling complex data).
        // Or we can pass data. RPD says "C++ listening ... via callback push to WPF".
        // Let's pass a simple signal first. "Something changed".

        private:
        inline static OnClipboardChangedCallback _callback = nullptr;
        inline static HWND _hMessageWindow = nullptr;
        inline static std::thread _listenerThread;
        inline static std::atomic<bool> _isRunning;

        public:
        /// <summary>
        /// 剪贴板窗口过程（回调），用于接收并处理发送到与剪贴板相关的窗口消息（例如 WM_DRAWCLIPBOARD、WM_CHANGECBCHAIN 等）。
        /// </summary>
        /// <param name="hwnd">接收消息的窗口句柄。</param>
        /// <param name="msg">要处理的窗口消息标识符（例如 WM_DRAWCLIPBOARD）。</param>
        /// <param name="wParam">与消息相关的附加参数，其含义取决于 msg。</param>
        /// <param name="lParam">与消息相关的附加参数，其含义取决于 msg。</param>
        /// <returns>消息处理的结果（LRESULT）。对于未处理的消息通常应调用 DefWindowProc 并返回其结果。</returns>
        static LRESULT CALLBACK ClipboardWndProc(HWND hwnd, UINT msg, WPARAM wParam, LPARAM lParam);
        /// <summary>
        /// 监听线程的入口函数，在单独线程中运行以接收或处理事件/连接。
        /// </summary>
        static void ListenerThreadProc();
        /// <summary>
        /// 启动剪贴板监听器并注册回调，用于在剪贴板内容变化时接收通知。
        /// </summary>
        /// <param name="callback">当剪贴板内容发生变化时调用的回调函数（类型为 OnClipboardChangedCallback）。</param>
        /// <returns>如果监听器成功启动并注册回调则返回 true，否则返回 false。</returns>
        static bool StartClipboardListener(OnClipboardChangedCallback callback);
        /// <summary>
        /// 停止剪贴板监听器并释放相关资源。
        /// </summary>
        static void StopClipboardListener();
    };
}



extern "C"
{
    __declspec(dllexport) bool StartClipboardListener(OnClipboardChangedCallback callback) { return IronSight::Core::Native::Clipboard::ClipboardListener::StartClipboardListener(callback); };
    __declspec(dllexport) void StopClipboardListener() { return IronSight::Core::Native::Clipboard::ClipboardListener::StopClipboardListener(); };
}