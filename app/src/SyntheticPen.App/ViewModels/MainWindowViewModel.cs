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
using SyntheticPen.Svg;
using ModelRect = SyntheticPen.Core.Models.Rect;

namespace SyntheticPen.App.ViewModels;

public sealed partial class MainWindowViewModel : ObservableObject
{
    private readonly IPlaybackController _playback;
    private readonly ISvgPathLoader _loader;
    private readonly IStrokePreviewRenderer _previewRenderer;
    private readonly ITargetRegionProvider _regions;
    private readonly IGlobalHotkeyService _hotkeys;
    private readonly InjectorFactory _injectorFactory;
    private readonly IMotionPlanner _planner;

    private SvgDocument? _doc;
    private CountdownOverlay? _countdown;
    private PlottingIndicator? _indicator;
    private MainWindow? _mainWindow;
    // The controller that's actually running a playback. ESC / RequestStop must
    // hit this one, not the DI singleton (which only exists to expose state at
    // rest — each playback builds its own controller to pick up the user's
    // current injection-mode selection).
    private IPlaybackController? _activePlayback;

    public void AttachMainWindow(MainWindow window) => _mainWindow = window;

    public MainWindowViewModel(
        IPlaybackController playback,
        ISvgPathLoader loader,
        IStrokePreviewRenderer previewRenderer,
        ITargetRegionProvider regions,
        IGlobalHotkeyService hotkeys,
        InjectorFactory injectorFactory,
        IMotionPlanner planner)
    {
        _playback = playback;
        _loader = loader;
        _previewRenderer = previewRenderer;
        _regions = regions;
        _hotkeys = hotkeys;
        _injectorFactory = injectorFactory;
        _planner = planner;

        _playback.StateChanged += OnStateChanged;
        _playback.CountdownTick += OnCountdownTick;
        _hotkeys.EmergencyStopRequested += () => (_activePlayback ?? _playback).RequestStop();
        _regions.Changed += _ => OnPropertyChanged(nameof(HasRegion));
        StateText = _playback.State.ToString();

        _ = LoadDemoSvgAsync();
    }

    private async Task LoadDemoSvgAsync()
    {
        try
        {
            var asm = typeof(MainWindowViewModel).Assembly;
            var resourceName = asm.GetManifestResourceNames()
                .FirstOrDefault(n => n.EndsWith("demo_circle_square.svg", StringComparison.Ordinal));
            if (resourceName is null) return;
            await using var s = asm.GetManifestResourceStream(resourceName);
            if (s is null) return;
            _doc = await _loader.LoadAsync(s, new FlattenOptions(0.25));
            SvgFileLabel = "demo_circle_square.svg (built-in)";
            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                PreviewGeometry = (Geometry)_previewRenderer.BuildGeometry(_doc.Strokes);
                PreviewSourceWidth = _doc.SourceViewBox.W;
                PreviewSourceHeight = _doc.SourceViewBox.H;
            });
        }
        catch
        {
            // Demo load is best-effort.
        }
    }

    [ObservableProperty] private string _stateText = string.Empty;
    [ObservableProperty] private double _speedMultiplier = 1.0;
    [ObservableProperty] private bool _humanize;
    [ObservableProperty] private InjectionMode _selectedInjectionMode = InjectionMode.SyntheticPointer;
    [ObservableProperty] private string _svgFileLabel = "(no file)";
    [ObservableProperty] private string _targetRegionLabel = "(not set)";
    [ObservableProperty] private Geometry? _previewGeometry;
    [ObservableProperty] private double _previewSourceWidth = 100;
    [ObservableProperty] private double _previewSourceHeight = 100;

    public bool HasRegion => _regions.Current is not null;

    public InjectionMode[] InjectionModes { get; } = Enum.GetValues<InjectionMode>();

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

        await using var s = await files[0].OpenReadAsync();
        _doc = await _loader.LoadAsync(s, new FlattenOptions(0.25));
        SvgFileLabel = files[0].Name;
        PreviewGeometry = (Geometry)_previewRenderer.BuildGeometry(_doc.Strokes);
        PreviewSourceWidth = _doc.SourceViewBox.W;
        PreviewSourceHeight = _doc.SourceViewBox.H;
    }

    [RelayCommand]
    private async Task CalibrateAsync()
    {
        // Hide the main window so the user can drag-select over the target app.
        _mainWindow?.Hide();

        var rect = await AwaitCalibrationAsync();

        if (rect is { } r)
        {
            TargetRegionLabel = $"{(int)r.W}×{(int)r.H} at ({(int)r.X},{(int)r.Y})";
            _mainWindow?.FitPreviewTo(r);
        }

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
        if (_doc is null || _regions.Current is null) return;

        var screenStrokes = StrokeTransform.FitToScreen(_doc.Strokes, _doc.SourceViewBox, _regions.Current.Value);

        var injector = _injectorFactory.Create(SelectedInjectionMode);
        var ctrl = new PlaybackController(injector, _planner);
        ctrl.StateChanged += OnStateChanged;
        ctrl.CountdownTick += OnCountdownTick;
        _activePlayback = ctrl;

        try
        {
            await ctrl.PlayAsync(screenStrokes,
                new PlaybackOptions(
                    SpeedMultiplier: SpeedMultiplier,
                    Mode: SelectedInjectionMode,
                    Countdown: TimeSpan.FromSeconds(3),
                    PrimeTapHold: TimeSpan.FromMilliseconds(40),
                    PrimeTapSettle: TimeSpan.FromMilliseconds(60)));
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

    private static void Shutdown()
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.Shutdown();
    }
}
