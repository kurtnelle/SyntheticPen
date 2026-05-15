using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace SyntheticPen.App.Win32;

/// <summary>
/// Headless resident mode (<c>SyntheticPen.App.exe --tray</c>): owns the
/// global <c>Win+Shift+X</c> hotkey and, when pressed, either rings the
/// doorbell on a running instance or cold-launches the app. No Avalonia / DI
/// is loaded in this path — it's just a thread with a message pump.
/// </summary>
[SupportedOSPlatform("windows")]
internal static class TrayMode
{
    private const int WM_HOTKEY = 0x0312;
    private const int WM_QUIT = 0x0012;
    private const uint MOD_SHIFT = 0x0004;
    private const uint MOD_WIN = 0x0008;
    private const uint MOD_NOREPEAT = 0x4000;
    private const uint VK_X = 0x58;
    private const int HotkeyId = 1;

    public static int Run()
    {
        // Only one tray helper at a time.
        using var trayMutex = new System.Threading.Mutex(true, "SyntheticPen.App.Tray", out bool firstTray);
        if (!firstTray) return 0;

        if (!RegisterHotKey(IntPtr.Zero, HotkeyId, MOD_WIN | MOD_SHIFT | MOD_NOREPEAT, VK_X))
            return 1; // combo already owned by something else

        // Lets "disable autostart" stop this helper without a reboot.
        Autostart.InstallTrayQuitListener();

        try
        {
            while (GetMessage(out var msg, IntPtr.Zero, 0, 0))
            {
                if (msg.message == WM_HOTKEY && (int)msg.wParam == HotkeyId)
                    OnHotkey();
            }
        }
        finally
        {
            UnregisterHotKey(IntPtr.Zero, HotkeyId);
        }
        return 0;
    }

    private static void OnHotkey()
    {
        // App already running → just bring it forward.
        if (SingleInstance.SignalShow()) return;

        // Cold launch: relaunch this exe with no args (normal app mode).
        var exe = Environment.ProcessPath;
        if (string.IsNullOrEmpty(exe)) return;
        try
        {
            Process.Start(new ProcessStartInfo(exe) { UseShellExecute = false });
        }
        catch { /* best effort — nothing sensible to do if the launch fails */ }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MSG
    {
        public IntPtr hwnd; public uint message; public IntPtr wParam; public IntPtr lParam;
        public uint time; public int pt_x; public int pt_y;
    }

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    [DllImport("user32.dll")]
    private static extern bool GetMessage(out MSG lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax);
}
