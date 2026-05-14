using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SyntheticPen.Core.Playback;

namespace SyntheticPen.App.ViewModels;

public sealed partial class MainWindowViewModel : ObservableObject
{
    private readonly IPlaybackController _playback;

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
    [RelayCommand] private Task CalibrateAsync() => Task.CompletedTask; // Task 12
    [RelayCommand] private Task StartAsync() => Task.CompletedTask;     // Task 15
    [RelayCommand] private Task StopAsync() => Task.CompletedTask;      // Task 15
}
