using System.Runtime.InteropServices;
using System.Text;

namespace SyntheticPen.Input.Win32;

internal static class ForegroundClassName
{
    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

    private static readonly HashSet<string> DenyList = new(StringComparer.OrdinalIgnoreCase)
    {
        "Credential Dialog Xaml Host",
        "LockScreen",
        "LogonUI",
        "ConsentUI",
        "UACBlackScreen"
    };

    public static bool IsDenied(out string? matched)
    {
        var hwnd = GetForegroundWindow();
        if (hwnd == IntPtr.Zero) { matched = null; return false; }
        var sb = new StringBuilder(256);
        GetClassName(hwnd, sb, sb.Capacity);
        var name = sb.ToString();
        if (DenyList.Contains(name)) { matched = name; return true; }
        matched = null;
        return false;
    }
}
