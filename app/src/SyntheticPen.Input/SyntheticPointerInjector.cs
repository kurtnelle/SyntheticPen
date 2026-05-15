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

        Diag.Record((uint)flags, _contact, (int)Math.Round(p.X), (int)Math.Round(p.Y), px, py);

        if (!InjectSyntheticPointerInput(_device, ref info, 1))
        {
            var err = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
            // MONITOR_DEFAULTTONULL (0): null => the point is inside the
            // virtual bounding rect but on NO physical monitor (multi-monitor
            // dead-zone), which InjectSyntheticPointerInput rejects.
            var hMon = MonitorFromPoint(new POINT { X = px, Y = py }, 0);
            Diag.Dump(new
            {
                win32err = err,
                flags = $"0x{(uint)flags:X}",
                contact = _contact,
                raw = new { x = (int)Math.Round(p.X), y = (int)Math.Round(p.Y) },
                injected = new { x = px, y = py },
                vbounds = new { x = _vx, y = _vy, w = _vw, h = _vh },
                maxValid = new { x = _vx + _vw - 1, y = _vy + _vh - 1 },
                onMonitor = hMon != IntPtr.Zero,
                winUnderPt = ClassAt(px, py),
                foreground = ForegroundClass()
            });
            throw new InjectionBlockedException(
                $"InjectSyntheticPointerInput failed (Win32 err {err}, flags=0x{(uint)flags:X}, " +
                $"contact={_contact}, raw=({p.X:F0},{p.Y:F0}), injected=({px},{py}), " +
                $"vbounds=({_vx},{_vy},{_vw},{_vh}) maxValid=({_vx + _vw - 1},{_vy + _vh - 1}), " +
                $"onMonitor={(hMon != IntPtr.Zero)}, " +
                $"winUnderPt='{ClassAt(px, py)}', foreground='{ForegroundClass()}')");
        }

        if (up) _contact = false;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Diagnostic ring buffer of the last N injected pointer events with
    /// monotonic timestamps. On the first injection failure it writes a JSON
    /// dump (failure context + the recent timeline with per-event gaps) next
    /// to the executable, so inter-stroke timing can be inspected without
    /// copy-pasting log lines. One-shot per process.
    /// </summary>
    private static class Diag
    {
        private const int Cap = 400;
        private static readonly object Gate = new();
        private static readonly System.Diagnostics.Stopwatch Clock = System.Diagnostics.Stopwatch.StartNew();
        private static readonly System.Collections.Generic.Queue<object> Events = new();
        private static bool _dumped;

        public static void Record(uint flags, bool contact, int rawX, int rawY, int px, int py)
        {
            lock (Gate)
            {
                Events.Enqueue(new
                {
                    tMs = Math.Round(Clock.Elapsed.TotalMilliseconds, 2),
                    flags = $"0x{flags:X}",
                    kind = (flags & 0x10000) != 0 ? "DOWN"
                         : (flags & 0x40000) != 0 ? "UP"
                         : (flags & 0x20000) != 0 ? "UPDATE" : "?",
                    contact,
                    raw = new { x = rawX, y = rawY },
                    injected = new { x = px, y = py }
                });
                while (Events.Count > Cap) Events.Dequeue();
            }
        }

        public static void Dump(object failure)
        {
            lock (Gate)
            {
                if (_dumped) return;
                _dumped = true;
                try
                {
                    var payload = new
                    {
                        utc = DateTime.UtcNow.ToString("o"),
                        failure,
                        recentCount = Events.Count,
                        recent = Events.ToArray()
                    };
                    var json = System.Text.Json.JsonSerializer.Serialize(
                        payload, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                    var path = System.IO.Path.Combine(AppContext.BaseDirectory, "syntheticpen-diag-latest.json");
                    System.IO.File.WriteAllText(path, json);
                }
                catch { /* diagnostics must never mask the original failure */ }
            }
        }
    }

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern IntPtr MonitorFromPoint(POINT pt, uint dwFlags);

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern IntPtr WindowFromPoint(POINT pt);

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
    private static extern int GetClassName(IntPtr hWnd, System.Text.StringBuilder buf, int max);

    private static string ClassName(IntPtr hWnd)
    {
        if (hWnd == IntPtr.Zero) return "<null>";
        var sb = new System.Text.StringBuilder(256);
        GetClassName(hWnd, sb, sb.Capacity);
        return sb.ToString();
    }

    private static string ClassAt(int x, int y) => ClassName(WindowFromPoint(new POINT { X = x, Y = y }));
    private static string ForegroundClass() => ClassName(GetForegroundWindow());

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        if (_device != IntPtr.Zero) DestroySyntheticPointerDevice(_device);
    }
}
