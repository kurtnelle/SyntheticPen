using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Avalonia.Controls;

namespace SyntheticPen.App.Win32;

[SupportedOSPlatform("windows")]
internal static class WindowInterop
{
    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_TRANSPARENT = 0x00000020;
    private const int WS_EX_NOACTIVATE = 0x08000000;
    private const int WS_EX_TOOLWINDOW = 0x00000080;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    [DllImport("user32.dll")]
    private static extern IntPtr WindowFromPoint(POINT pt);

    [DllImport("user32.dll")]
    private static extern IntPtr GetAncestor(IntPtr hwnd, uint flags);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT { public int X; public int Y; }

    /// <summary>Make a window click-through and non-activating. Call from the window's Opened handler.</summary>
    public static void MakeClickThrough(Window window)
    {
        var handle = window.TryGetPlatformHandle();
        if (handle is null) return;
        var hwnd = handle.Handle;
        if (hwnd == IntPtr.Zero) return;
        var style = GetWindowLong(hwnd, GWL_EXSTYLE);
        SetWindowLong(hwnd, GWL_EXSTYLE, style | WS_EX_TRANSPARENT | WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW);
    }

    /// <summary>
    /// Bring the top-level window at the given screen pixel into the foreground so subsequent
    /// SendInput events land there. Returns true if a window was focused.
    /// </summary>
    public static bool FocusWindowAt(int screenX, int screenY)
    {
        var hwnd = WindowFromPoint(new POINT { X = screenX, Y = screenY });
        if (hwnd == IntPtr.Zero) return false;
        // GA_ROOT = 2 — get the top-level (root) ancestor, not a child control
        var root = GetAncestor(hwnd, 2);
        if (root == IntPtr.Zero) root = hwnd;
        return SetForegroundWindow(root);
    }
}
