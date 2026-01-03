#include <pch.h>
#include "ClipboardListener.h"
#include "Utilities.h"

namespace IronSight::Core::Native::Clipboard
{
	LRESULT CALLBACK ClipboardListener::ClipboardWndProc(HWND hwnd, UINT msg, WPARAM wParam, LPARAM lParam)
	{
		switch (msg)
		{
		case WM_CREATE:
			if (!AddClipboardFormatListener(hwnd))
			{
				LOG_ERROR("AddClipboardFormatListener failed: %d", GetLastError());
				return -1;
			}
			return 0;

		case WM_CLIPBOARDUPDATE:
			if (_callback)
			{
				_callback();
			}
			return 0;

		case WM_DESTROY:
			RemoveClipboardFormatListener(hwnd);
			PostQuitMessage(0);
			return 0;

		default:
			return DefWindowProc(hwnd, msg, wParam, lParam);
		}
	}

	void ClipboardListener::ListenerThreadProc()
	{
		WNDCLASSEX wc = { 0 };
		wc.cbSize = sizeof(WNDCLASSEX);
		wc.lpfnWndProc = ClipboardWndProc;
		wc.hInstance = GetModuleHandle(NULL);
		wc.lpszClassName = L"IronSightClipboardListener";

		RegisterClassEx(&wc);

		_hMessageWindow = CreateWindowEx(0, wc.lpszClassName, L"IronSightHidden", 0, 0, 0, 0, 0, HWND_MESSAGE, NULL, wc.hInstance, NULL);

		if (!_hMessageWindow)
		{
			LOG_ERROR("Failed to create message window: %d", GetLastError());
			return;
		}

		MSG msg;
		while (GetMessage(&msg, NULL, 0, 0))
		{
			TranslateMessage(&msg);
			DispatchMessage(&msg);
		}

		_hMessageWindow = NULL;
	}

	bool ClipboardListener::StartClipboardListener(OnClipboardChangedCallback callback)
	{
		if (_isRunning) return true;

		_callback = callback;
		_isRunning = true;
		_listenerThread = std::thread(ListenerThreadProc);
		_listenerThread.detach(); // Let it run independently
		
		return true;
	}


	void ClipboardListener::StopClipboardListener()
	{
		if (_isRunning && _hMessageWindow)
		{
			// Send WM_CLOSE or DestroyWindow? 
			// Since it's another thread, we should PostMessage.
			PostMessage(_hMessageWindow, WM_CLOSE, 0, 0);
			// Actually WM_CLOSE isn't handled in my switch, DefWindowProc handles it by calling DestroyWindow?
			// DefWindowProc for WM_CLOSE calls DestroyWindow. 
			// Let's ensure we destroy it.
			PostMessage(_hMessageWindow, WM_DESTROY, 0, 0);

			_isRunning = false;
			_callback = nullptr;
		}
	}
}

