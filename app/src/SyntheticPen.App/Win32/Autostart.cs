using System.Diagnostics;
using System.Runtime.Versioning;
using System.Threading;
using Microsoft.Win32;

namespace SyntheticPen.App.Win32;

/// <summary>
/// Opt-in "launch with Win+Shift+X" support. Enabling registers the
/// <c>--tray</c> helper under the per-user Run key (survives reboot) and
/// starts it immediately so the hotkey works without logging out. Disabling
/// removes the key and stops the running helper. Strictly HKCU — never writes
/// machine-wide state, never needs elevation.
/// </summary>
[SupportedOSPlatform("windows")]
internal static class Autostart
{
    private const string RunKeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "SyntheticPen";
    private const string TrayQuitEventName = "SyntheticPen.App.TrayQuit";

    private static string TrayCommand
    {
        get
        {
            var exe = Environment.ProcessPath ?? "";
            return $"\"{exe}\" --tray";
        }
    }

    public static bool IsEnabled
    {
        get
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath);
            return key?.GetValue(ValueName) is string v && v.Length > 0;
        }
    }

    public static void Enable()
    {
        using (var key = Registry.CurrentUser.CreateSubKey(RunKeyPath))
            key.SetValue(ValueName, TrayCommand, RegistryValueKind.String);
        StartTrayHelper();
    }

    public static void Disable()
    {
        using (var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true))
            key?.DeleteValue(ValueName, throwOnMissingValue: false);
        StopTrayHelper();
    }

    /// <summary>Start the tray helper now (no-op if one is already running —
    /// the helper self-guards with a mutex).</summary>
    public static void StartTrayHelper()
    {
        var exe = Environment.ProcessPath;
        if (string.IsNullOrEmpty(exe)) return;
        try
        {
            Process.Start(new ProcessStartInfo(exe, "--tray") { UseShellExecute = false });
        }
        catch { /* best effort */ }
    }

    /// <summary>Signal any running tray helper to exit.</summary>
    public static void StopTrayHelper()
    {
        try
        {
            using var ev = EventWaitHandle.OpenExisting(TrayQuitEventName);
            ev.Set();
        }
        catch (WaitHandleCannotBeOpenedException)
        {
            // No helper running — nothing to stop.
        }
    }

    /// <summary>
    /// Called from the tray process: blocks a background thread until a quit
    /// signal arrives (e.g. user disabled autostart), then hard-exits. The
    /// helper holds no state worth unwinding beyond the hotkey, which the OS
    /// releases on process exit.
    /// </summary>
    public static void InstallTrayQuitListener()
    {
        var ev = new EventWaitHandle(false, EventResetMode.AutoReset, TrayQuitEventName);
        var t = new Thread(() =>
        {
            ev.WaitOne();
            Environment.Exit(0);
        })
        { IsBackground = true, Name = "SyntheticPen.TrayQuit" };
        t.Start();
    }
}
