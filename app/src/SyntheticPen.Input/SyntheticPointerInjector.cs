using SyntheticPen.Core;
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

    // Virtual-desktop bounds, cached for the injector's lifetime (one per
    // playback). InjectSyntheticPointerInput rejects the whole event with
    // ERROR_INVALID_PARAMETER for any coordinate outside the inclusive pixel
    // range, and FitToScreen can legitimately land the far edge of the fitted
    // content exactly on origin+size (one pixel past the last valid index).
    private readonly int _vx, _vy, _vw, _vh;

    /// <summary>Pen pressure in [0,1]; mapped to the Win32 0..1024 range while
    /// in contact. Clamped to ≥1 in contact so a 0 reading can't be mistaken
    /// for "no contact" by the target.</summary>
    public float Pressure { get; set; } = 1f;

    public SyntheticPointerInjector()
    {
        // mode = POINTER_FEEDBACK_DEFAULT (1)
        _device = CreateSyntheticPointerDevice((uint)POINTER_INPUT_TYPE_PEN, 1, 1);
        if (_device == IntPtr.Zero)
            throw new InjectionBlockedException("CreateSyntheticPointerDevice failed (Windows 10 1809+ required)");

        _vx = SendInputNative.GetSystemMetrics(SendInputNative.SM_XVIRTUALSCREEN);
        _vy = SendInputNative.GetSystemMetrics(SendInputNative.SM_YVIRTUALSCREEN);
        _vw = SendInputNative.GetSystemMetrics(SendInputNative.SM_CXVIRTUALSCREEN);
        _vh = SendInputNative.GetSystemMetrics(SendInputNative.SM_CYVIRTUALSCREEN);
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

        // Clamp into the inclusive valid pixel range. A coordinate equal to
        // origin+size (FitToScreen's far edge can land exactly there) is one
        // pixel out of range and makes InjectSyntheticPointerInput fail the
        // whole event with ERROR_INVALID_PARAMETER.
        var (px, py) = VirtualScreen.ClampPixel(
            (int)Math.Round(p.X), (int)Math.Round(p.Y), _vx, _vy, _vw, _vh);

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
                    ptPixelLocation = new POINT { X = px, Y = py }
                },
                penMask = POINTER_PEN_MASK.PRESSURE,
                // Win32 pen pressure is 0..1024. While in contact, map the
                // [0,1] Pressure but never report 0 (some targets read 0 as a
                // lift). Out of contact reports 0.
                pressure = _contact
                    ? (uint)Math.Clamp((int)MathF.Round(Math.Clamp(Pressure, 0f, 1f) * 1024f), 1, 1024)
                    : 0u
            }
        };

        if (!InjectSyntheticPointerInput(_device, ref info, 1))
        {
            var err = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
            throw new InjectionBlockedException(
                $"InjectSyntheticPointerInput failed (Win32 err {err}, flags=0x{(uint)flags:X}, " +
                $"contact={_contact}, pt=({px},{py}))");
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
