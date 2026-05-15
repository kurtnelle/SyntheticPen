using SyntheticPen.Core.Models;
using SyntheticPen.Core.Playback;
using SyntheticPen.Input.Win32;
using static SyntheticPen.Input.Win32.SyntheticPointerNative;

namespace SyntheticPen.Input;

public sealed class SyntheticPointerInjector : ICursorInjector, IDisposable
{
    private readonly IntPtr _device;
    private bool _disposed;
    private PointF _lastPoint;
    private bool _contact;
    // POINTER_FLAGS.NEW marks the arrival of a new pointer. The Windows pen
    // injection samples pair NEW with the first DOWN event and never repeat
    // it for the same pointer ID while it remains tracked. Sending NEW twice
    // (e.g. on a second DOWN after the prime tap's UP) makes
    // InjectSyntheticPointerInput reject the event.
    private bool _needsNew = true;

    public SyntheticPointerInjector()
    {
        // mode = POINTER_FEEDBACK_DEFAULT (1)
        _device = CreateSyntheticPointerDevice((uint)POINTER_INPUT_TYPE_PEN, 1, 1);
        if (_device == IntPtr.Zero)
            throw new InjectionBlockedException("CreateSyntheticPointerDevice failed (Windows 10 1809+ required)");
    }

    public Task MoveAsync(PointF p, CancellationToken ct = default) => Inject(p, drag: true, down: false, up: false);
    public Task PenDownAsync(CancellationToken ct = default) => Inject(_lastPoint, drag: false, down: true, up: false);
    public Task PenUpAsync(CancellationToken ct = default) => Inject(_lastPoint, drag: false, down: false, up: true);

    private Task Inject(PointF p, bool drag, bool down, bool up)
    {
        if (ForegroundClassName.IsDenied(out var name) && !up)
            throw new InjectionBlockedException($"foreground window class '{name}' is on the deny list");

        _lastPoint = p;
        if (down) _contact = true;

        POINTER_FLAGS flags = POINTER_FLAGS.INRANGE;
        // INCONTACT/FIRSTBUTTON belong on contact-or-drag events. Including
        // them on UP contradicts the "contact is ending" semantics and can
        // make Windows fully terminate the pointer.
        if (_contact && !up) flags |= POINTER_FLAGS.INCONTACT | POINTER_FLAGS.FIRSTBUTTON;
        if (down)
        {
            flags |= POINTER_FLAGS.DOWN;
            if (_needsNew) { flags |= POINTER_FLAGS.NEW; _needsNew = false; }
        }
        else if (up) flags |= POINTER_FLAGS.UP;
        else if (drag) flags |= POINTER_FLAGS.UPDATE;

        var info = new POINTER_TYPE_INFO
        {
            type = POINTER_INPUT_TYPE_PEN,
            penInfo = new POINTER_PEN_INFO
            {
                pointerInfo = new POINTER_INFO
                {
                    pointerType = POINTER_INPUT_TYPE_PEN,
                    pointerId = 1,
                    pointerFlags = flags,
                    ptPixelLocation = new POINT { X = (int)Math.Round(p.X), Y = (int)Math.Round(p.Y) }
                },
                penMask = POINTER_PEN_MASK.PRESSURE,
                pressure = _contact ? 512u : 0u   // 512 of 1024 = mid pressure; constant for Phase 1
            }
        };

        if (!InjectSyntheticPointerInput(_device, ref info, 1))
        {
            var err = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
            throw new InjectionBlockedException(
                $"InjectSyntheticPointerInput failed (Win32 err {err}, flags=0x{(uint)flags:X}, contact={_contact}, pt=({p.X:F0},{p.Y:F0}))");
        }

        if (up) _contact = false;
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        if (_device != IntPtr.Zero) DestroySyntheticPointerDevice(_device);
    }
}
