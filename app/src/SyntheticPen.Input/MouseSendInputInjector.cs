using System.Runtime.InteropServices;
using SyntheticPen.Core.Models;
using SyntheticPen.Core.Playback;
using SyntheticPen.Input.Win32;
using static SyntheticPen.Input.Win32.SendInputNative;

namespace SyntheticPen.Input;

public sealed class MouseSendInputInjector : ICursorInjector
{
    public Task MoveAsync(PointF screenPoint, CancellationToken ct = default)
    {
        EnforceDenyList();
        SendMouse((int)Math.Round(screenPoint.X), (int)Math.Round(screenPoint.Y),
            MOUSEEVENTF_MOVE | MOUSEEVENTF_ABSOLUTE | MOUSEEVENTF_VIRTUALDESK);
        return Task.CompletedTask;
    }

    public Task PenDownAsync(CancellationToken ct = default)
    {
        EnforceDenyList();
        Send(MOUSEEVENTF_LEFTDOWN);
        return Task.CompletedTask;
    }

    public Task PenUpAsync(CancellationToken ct = default)
    {
        // Pen-up is always allowed — we want to RELEASE the button even if the
        // foreground became a credential surface mid-stroke. Never leave it stuck.
        Send(MOUSEEVENTF_LEFTUP);
        return Task.CompletedTask;
    }

    private static void EnforceDenyList()
    {
        if (ForegroundClassName.IsDenied(out var name))
            throw new InjectionBlockedException($"foreground window class '{name}' is on the deny list");
    }

    private static void SendMouse(int physX, int physY, uint flags)
    {
        int vx = GetSystemMetrics(SM_XVIRTUALSCREEN);
        int vy = GetSystemMetrics(SM_YVIRTUALSCREEN);
        int vw = GetSystemMetrics(SM_CXVIRTUALSCREEN);
        int vh = GetSystemMetrics(SM_CYVIRTUALSCREEN);
        int nx = (int)Math.Round((physX - vx) * 65535.0 / Math.Max(1, vw - 1));
        int ny = (int)Math.Round((physY - vy) * 65535.0 / Math.Max(1, vh - 1));

        var input = new INPUT
        {
            type = INPUT_MOUSE,
            U = new InputUnion { mi = new MOUSEINPUT { dx = nx, dy = ny, dwFlags = flags } }
        };
        var sent = SendInput(1, ref input, Marshal.SizeOf<INPUT>());
        if (sent == 0)
            throw new InjectionBlockedException("SendInput returned 0 (UIPI / integrity?)");
    }

    private static void Send(uint flags)
    {
        var input = new INPUT
        {
            type = INPUT_MOUSE,
            U = new InputUnion { mi = new MOUSEINPUT { dwFlags = flags } }
        };
        var sent = SendInput(1, ref input, Marshal.SizeOf<INPUT>());
        if (sent == 0)
            throw new InjectionBlockedException("SendInput returned 0 (UIPI / integrity?)");
    }
}
