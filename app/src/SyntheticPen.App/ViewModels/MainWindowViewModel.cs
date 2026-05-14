using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using SyntheticPen.App.Views;
using SyntheticPen.Core.Playback;
using SyntheticPen.Core.Targeting;

namespace SyntheticPen.App.ViewModels;

public sealed partial class MainWindowViewModel : ObservableObject
{
    private readonly IPlaybackController _playback;
    private readonly ITargetRegionProvider _regions = Program.Services.GetRequiredService<ITargetRegionProvider>();

    public MainWindowViewModel(IPlaybackController playback)
    {
        _playback = playback;
        _playback.StateChanged += s => StateText = s.ToString();
        StateText = _playback.State.ToString();
    }

    [ObservableProperty] private string _stateText = string.Empty;
    [ObservableProperty] private double _speedMultiplier = 1.0;
    [ObservableProperty] private bool _humanize;
    [ObservableProperty] private InjectionMode _selectedInjectionMode = InjectionMode.Mouse;
    [ObservableProperty] private bool _isAlwaysOnTop = true;
    [ObservableProperty] private string _svgFileLabel = "(no file)";
    [ObservableProperty] private string _targetRegionLabel = "(not set)";

    public InjectionMode[] InjectionModes { get; } = Enum.GetValues<InjectionMode>();

    [RelayCommand] private Task OpenSvgAsync() => Task.CompletedTask;  // Task 15
    [RelayCommand] private void Exit() { /* Task 15 */ }
    [RelayCommand] private void About() { /* Task 16 */ }

    [RelayCommand]
    private async Task CalibrateAsync()
    {
        var owner = (Avalonia.Application.Current?.ApplicationLifetime
            as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
        if (owner is null) return;

        owner.WindowState = WindowState.Minimized;
        var overlay = new CalibrationOverlay();
        await overlay.ShowDialog(owner);
        owner.WindowState = WindowState.Normal;
        owner.Activate();

        if (overlay.SelectedRect is { } r)
        {
            _regions.Set(r);
            TargetRegionLabel = $"{(int)r.W}×{(int)r.H} at ({(int)r.X},{(int)r.Y})";
        }
    }

    [RelayCommand] private Task StartAsync() => Task.CompletedTask;     // Task 15
    [RelayCommand] private Task StopAsync() => Task.CompletedTask;      // Task 15
}
