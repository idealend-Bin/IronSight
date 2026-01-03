using System;
using System.Runtime.InteropServices;

namespace IronSight.Interop.Native.Clipboard
{
    public static class ClipboardMethods
    {
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate void OnClipboardChangedCallback();

        [DllImport("IronSight.Core.Native.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool StartClipboardListener(OnClipboardChangedCallback callback);

        [DllImport("IronSight.Core.Native.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void StopClipboardListener();
    }
}