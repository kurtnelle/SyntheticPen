using System.Runtime.InteropServices;
using SyntheticPen.Core.Models;

namespace SyntheticPen.Input;

public sealed class MouseSendInputInjector : ICursorInjector
{
    public Task MoveAsync(PointF point, CancellationToken ct = default)
        => throw new NotImplementedException("Phase 1");

    public Task PenDownAsync(CancellationToken ct = default)
        => throw new NotImplementedException("Phase 1");

    public Task PenUpAsync(CancellationToken ct = default)
        => throw new NotImplementedException("Phase 1");

    // P/Invoke signatures reserved for Phase 1.
#pragma warning disable IDE0051, CA1812
    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, ref INPUT pInputs, int cbSize);

    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT
    {
        public uint type;
        public InputUnion U;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion
    {
        [FieldOffset(0)] public MOUSEINPUT mi;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MOUSEINPUT
    {
        public int dx;
        public int dy;
        public uint mouseData;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }
#pragma warning restore IDE0051, CA1812
}
