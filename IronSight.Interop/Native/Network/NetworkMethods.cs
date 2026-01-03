using System.Runtime.InteropServices;

namespace IronSight.Interop.Native.Network
{
    /// <summary>
    /// TCP连接状态枚举
    /// </summary>
    public enum ConnectionState : int
    {
        Unknown = 0,
        Closed = 1,
        Listen = 2,
        SynSent = 3,
        SynReceived = 4,
        Established = 5,
        FinWait1 = 6,
        FinWait2 = 7,
        CloseWait = 8,
        Closing = 9,
        LastAck = 10,
        TimeWait = 11,
        DeleteTcb = 12
    }

    /// <summary>
    /// 网络协议类型枚举
    /// </summary>
    public enum ProtocolType : int
    {
        Unknown = 0,
        Tcp = 1,
        Udp = 2
    }

    /// <summary>
    /// 网络监控器Native互操作类
    /// </summary>
    public static class NetworkMethods
    {
        public const string DllName = "IronSight.Core.Native.dll";

        #region P/Invoke Declarations

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr NetworkMonitor_Create();

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void NetworkMonitor_Destroy(IntPtr monitor);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool NetworkMonitor_Refresh(IntPtr monitor);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool NetworkMonitor_RefreshTcp(IntPtr monitor);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool NetworkMonitor_RefreshUdp(IntPtr monitor);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern nuint NetworkMonitor_GetConnectionCount(IntPtr monitor);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern nuint NetworkMonitor_CopyConnections(IntPtr monitor, IntPtr buffer, nuint bufferSize);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int NetworkConnectionInfo_GetSize();

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool NetworkMonitor_SetUpdateInterval(IntPtr monitor, uint intervalMs);

        #endregion // P/Invoke Declarations
    }
}

