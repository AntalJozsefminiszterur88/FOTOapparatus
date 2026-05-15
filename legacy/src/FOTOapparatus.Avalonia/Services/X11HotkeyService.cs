using System.Runtime.InteropServices;

namespace FOTOapparatus.Avalonia.Services;

public sealed class X11HotkeyService
{
    public bool SendCtrlNumber(int number)
    {
        if (!OperatingSystem.IsLinux())
        {
            return false;
        }

        var display = XOpenDisplay(IntPtr.Zero);
        if (display == IntPtr.Zero)
        {
            return false;
        }

        try
        {
            var controlKeysym = XStringToKeysym("Control_L");
            var numberKeysym = XStringToKeysym(number.ToString());
            if (controlKeysym == 0 || numberKeysym == 0)
            {
                return false;
            }

            var controlKeyCode = XKeysymToKeycode(display, controlKeysym);
            var numberKeyCode = XKeysymToKeycode(display, numberKeysym);
            if (controlKeyCode == 0 || numberKeyCode == 0)
            {
                return false;
            }

            XTestFakeKeyEvent(display, controlKeyCode, true, 0);
            XFlush(display);
            Thread.Sleep(40);

            XTestFakeKeyEvent(display, numberKeyCode, true, 0);
            XFlush(display);
            Thread.Sleep(40);

            XTestFakeKeyEvent(display, numberKeyCode, false, 0);
            XFlush(display);
            Thread.Sleep(40);

            XTestFakeKeyEvent(display, controlKeyCode, false, 0);
            XFlush(display);
            return true;
        }
        finally
        {
            XCloseDisplay(display);
        }
    }

    [DllImport("libX11.so.6")]
    private static extern IntPtr XOpenDisplay(IntPtr display);

    [DllImport("libX11.so.6")]
    private static extern int XCloseDisplay(IntPtr display);

    [DllImport("libX11.so.6")]
    private static extern ulong XStringToKeysym(string key);

    [DllImport("libX11.so.6")]
    private static extern byte XKeysymToKeycode(IntPtr display, ulong keysym);

    [DllImport("libX11.so.6")]
    private static extern int XFlush(IntPtr display);

    [DllImport("libXtst.so.6")]
    private static extern int XTestFakeKeyEvent(IntPtr display, uint keycode, bool isPress, ulong delay);
}
