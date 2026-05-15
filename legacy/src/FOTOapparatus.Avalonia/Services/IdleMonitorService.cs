using System.Runtime.InteropServices;

namespace FOTOapparatus.Avalonia.Services;

public sealed class IdleMonitorService
{
    public double? GetIdleSeconds()
    {
        if (!OperatingSystem.IsLinux())
        {
            return null;
        }

        var display = XOpenDisplay(IntPtr.Zero);
        if (display == IntPtr.Zero)
        {
            return null;
        }

        var infoPointer = XScreenSaverAllocInfo();
        if (infoPointer == IntPtr.Zero)
        {
            XCloseDisplay(display);
            return null;
        }

        try
        {
            var rootWindow = XDefaultRootWindow(display);
            if (XScreenSaverQueryInfo(display, rootWindow, infoPointer) == 0)
            {
                return null;
            }

            var info = Marshal.PtrToStructure<XScreenSaverInfo>(infoPointer);
            return info.Idle / 1000d;
        }
        finally
        {
            XFree(infoPointer);
            XCloseDisplay(display);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct XScreenSaverInfo
    {
        public IntPtr Window;
        public int State;
        public int Kind;
        public ulong Since;
        public ulong Idle;
        public ulong EventMask;
    }

    [DllImport("libX11.so.6")]
    private static extern IntPtr XOpenDisplay(IntPtr display);

    [DllImport("libX11.so.6")]
    private static extern int XCloseDisplay(IntPtr display);

    [DllImport("libX11.so.6")]
    private static extern IntPtr XDefaultRootWindow(IntPtr display);

    [DllImport("libX11.so.6")]
    private static extern int XFree(IntPtr data);

    [DllImport("libXss.so.1")]
    private static extern IntPtr XScreenSaverAllocInfo();

    [DllImport("libXss.so.1")]
    private static extern int XScreenSaverQueryInfo(IntPtr display, IntPtr drawable, IntPtr info);
}
