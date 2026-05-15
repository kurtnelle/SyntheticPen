using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using SyntheticPen.App.Views;
using SyntheticPen.Core;
using SyntheticPen.Core.Playback;
using SyntheticPen.Core.Targeting;
using SyntheticPen.Hotkeys;
using SyntheticPen.Rendering;
using SyntheticPen.Vectorize;
using ModelRect = SyntheticPen.Core.Models.Rect;
using CoreStroke = SyntheticPen.Core.Models.Stroke;

namespace SyntheticPen.App.ViewModels;

public sealed partial class MainWindowViewModel : ObservableObject
{
    private readonly IPlaybackController _playback;
    private readonly IStrokePreviewRenderer _previewRenderer;
    private readonly ITargetRegionProvider _regions;
    private readonly IGlobalHotkeyService _hotkeys;
    private readonly InjectorFactory _injectorFactory;
    private readonly IMotionPlanner _planner;

    // Extracted centerline (the pen path) with per-point pressure, in SVG
    // coordinate space, plus its tight bounding box used to fit it to the
    // calibrated target region.
    private IReadOnlyList<CoreStroke>? _strokes;
    private ModelRect _sourceViewBox;
    private CountdownOverlay? _countdown;
    private PlottingIndicator? _indicator;
    private MainWindow? _mainWindow;
    // The controller that's actually running a playback. ESC / RequestStop must
    // hit this one, not the DI singleton (which only exists to expose state at
    // rest — each playback builds its own controller to pick up the user's
    // current injection-mode selection).
    private IPlaybackController? _activePlayback;

    // ESC handling state. _inCalibration suppresses idle close-arming while the
    // CalibrationOverlay is open (it handles ESC itself and triggers shutdown
    // via the null-rect path). _disarmCts cancels the 1-second "armed" window
    // if a second ESC arrives quickly, so we can distinguish single from
    // double presses without a separate input thread.
    private bool _inCalibration;
    private CancellationTokenSource? _disarmCts;

    public void AttachMainWindow(MainWindow window) => _mainWindow = window;

    public MainWindowViewModel(
        IPlaybackController playback,
        IStrokePreviewRenderer previewRenderer,
        ITargetRegionProvider regions,
        IGlobalHotkeyService hotkeys,
        InjectorFactory injectorFactory,
        IMotionPlanner planner)
    {
        _playback = playback;
        _previewRenderer = previewRenderer;
        _regions = regions;
        _hotkeys = hotkeys;
        _injectorFactory = injectorFactory;
        _planner = planner;

        _playback.StateChanged += OnStateChanged;
        _playback.CountdownTick += OnCountdownTick;
        _hotkeys.EmergencyStopRequested += OnEscapeFromHotkey;
        _regions.Changed += _ => OnPropertyChanged(nameof(HasRegion));
        StateText = _playback.State.ToString();
    }

    [ObservableProperty] private string _stateText = string.Empty;
    [ObservableProperty] private double _speedMultiplier = 1.0;
    [ObservableProperty] private bool _humanize;
    [ObservableProperty] private string _svgFileLabel = "(no file)";
    [ObservableProperty] private string _targetRegionLabel = "(not set)";
    [ObservableProperty] private Geometry? _previewGeometry;
    [ObservableProperty] private double _previewSourceWidth = 100;
    [ObservableProperty] private double _previewSourceHeight = 100;
    [ObservableProperty] private bool _isCloseArmed;

    public bool HasRegion => _regions.Current is not null;

    [RelayCommand]
    private async Task OpenSvgAsync()
    {
        var owner = ActiveOwner();
        if (owner is null) return;
        var files = await owner.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open SVG",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("SVG files") { Patterns = new[] { "*.svg" } }
            }
        });
        if (files.Count == 0) return;

        byte[] svgBytes;
        await using (var s = await files[0].OpenReadAsync())
        using (var buf = new MemoryStream())
        {
            await s.CopyToAsync(buf);
            svgBytes = buf.ToArray();
        }

        // Centerline extraction is CPU-heavy (raster + EDT + thinning) — keep
        // it off the UI thread. The result is the actual pen path with
        // per-point pressure, which we use for both preview and replay.
        var (strokes, viewBox) = await Task.Run(() =>
        {
            using var ms = new MemoryStream(svgBytes);
            var centerlines = new CenterlineExtractor().Extract(ms);
            return CenterlineStrokeAdapter.ToStrokes(centerlines);
        });

        _strokes = strokes;
        _sourceViewBox = viewBox;
        SvgFileLabel = files[0].Name;
        PreviewGeometry = (Geometry)_previewRenderer.BuildGeometry(strokes);
        PreviewSourceWidth = viewBox.W;
        PreviewSourceHeight = viewBox.H;
    }

    [RelayCommand]
    private async Task CalibrateAsync()
    {
        // Hide the main window so the user can drag-select over the target app.
        _mainWindow?.Hide();
        _inCalibration = true;

        ModelRect? rect;
        try { rect = await AwaitCalibrationAsync(); }
        finally { _inCalibration = false; }

        // ESC during selection mode = quit the app. Matches the initial-launch
        // calibration flow (App.OnFrameworkInitializationCompleted shuts down
        // on a null rect there too).
        if (rect is null)
        {
            Shutdown();
            return;
        }

        var r = rect.Value;
        TargetRegionLabel = $"{(int)r.W}×{(int)r.H} at ({(int)r.X},{(int)r.Y})";
        _mainWindow?.FitPreviewTo(r);
        _mainWindow?.Show();
        _mainWindow?.Activate();
    }

    private static Task<ModelRect?> AwaitCalibrationAsync()
    {
        var overlay = new CalibrationOverlay();
        var tcs = new TaskCompletionSource<ModelRect?>();
        overlay.Closed += (_, _) => tcs.TrySetResult(overlay.SelectedRect);
        overlay.Show();
        overlay.Activate();
        return tcs.Task;
    }

    [RelayCommand]
    private async Task StartAsync()
    {
        if (_strokes is null || _regions.Current is null) return;

        var screenStrokes = StrokeTransform.FitToScreen(_strokes, _sourceViewBox, _regions.Current.Value);

        var injector = _injectorFactory.Create(InjectionMode.SyntheticPointer);
        var ctrl = new PlaybackController(injector, _planner);
        ctrl.StateChanged += OnStateChanged;
        ctrl.CountdownTick += OnCountdownTick;
        _activePlayback = ctrl;

        try
        {
            await ctrl.PlayAsync(screenStrokes,
                new PlaybackOptions(
                    SpeedMultiplier: SpeedMultiplier,
                    Mode: InjectionMode.SyntheticPointer,
                    Countdown: TimeSpan.FromSeconds(3),
                    PrimeTapHold: TimeSpan.FromMilliseconds(40),
                    PrimeTapSettle: TimeSpan.FromMilliseconds(60),
                    // Pace injection so catch-up bursts can't overrun the
                    // Windows synthetic-pointer pipeline (root cause of the
                    // ERROR_INVALID_PARAMETER aborts).
                    MinEventInterval: TimeSpan.FromMilliseconds(2),
                    ContactSettle: TimeSpan.FromMilliseconds(8)));
        }
        finally
        {
            _activePlayback = null;
            if (injector is IDisposable d) d.Dispose();
        }
    }

    [RelayCommand]
    private void Exit() => Shutdown();

    [RelayCommand]
    private async Task AboutAsync()
    {
        var owner = ActiveOwner();
        if (owner is null)
        {
            new Views.AboutDialog().Show();
            return;
        }
        await new Views.AboutDialog().ShowDialog(owner);
    }

    private void OnStateChanged(PlaybackState s)
    {
        StateText = s.ToString();
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            if (s == PlaybackState.CountingDown)
            {
                _mainWindow?.Hide();
                _countdown ??= new CountdownOverlay();
                if (_regions.Current is { } r) _countdown.PositionOver(r);
                _countdown.Show();
            }
            else if (s == PlaybackState.Playing)
            {
                _countdown?.Close(); _countdown = null;
                // Focus the window under the center of the target rect so the first
                // SendInput / pen event lands on the intended app, not on our own window.
                if (_regions.Current is { } r)
                {
                    Win32.WindowInterop.FocusWindowAt(
                        (int)(r.X + r.W / 2.0),
                        (int)(r.Y + r.H / 2.0));
                }
                _indicator ??= new PlottingIndicator();
                _indicator.Show();
            }
            else // Idle, Cancelling, end
            {
                _countdown?.Close(); _countdown = null;
                _indicator?.Close(); _indicator = null;
                _mainWindow?.Show();
            }
        });
    }

    private void OnCountdownTick(TimeSpan remaining)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() => _countdown?.SetRemaining(remaining));
    }

    private Window? ActiveOwner() => _mainWindow;

    /// <summary>
    /// Routes global ESC: stops playback if running; otherwise enters a 1-second
    /// "armed to close" window that turns the X button red, with a second ESC
    /// in that window quitting the app. Calibration mode is handled separately
    /// by <see cref="CalibrateAsync"/>.
    /// </summary>
    private void OnEscapeFromHotkey()
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(HandleEscapeOnUi);
    }

    private async void HandleEscapeOnUi()
    {
        if (_inCalibration) return;

        if (_activePlayback is not null)
        {
            _activePlayback.RequestStop();
            return;
        }

        if (IsCloseArmed)
        {
            Shutdown();
            return;
        }

        IsCloseArmed = true;
        _disarmCts?.Cancel();
        _disarmCts = new CancellationTokenSource();
        var token = _disarmCts.Token;
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(1), token);
            IsCloseArmed = false;
        }
        catch (OperationCanceledException)
        {
            // Re-armed or shutdown — either way, no further action needed here.
        }
    }

    private static void Shutdown()
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.Shutdown();
    }
}
