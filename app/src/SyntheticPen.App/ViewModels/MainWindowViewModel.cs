using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SyntheticPen.Core.Playback;
using SyntheticPen.Input;

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

    public InjectionMode[] InjectionModes { get; } = Enum.GetValues<InjectionMode>();

    [RelayCommand] private Task OpenSvgAsync() => Task.CompletedTask;
    [RelayCommand] private void Exit() { /* TODO Phase 1: graceful shutdown */ }
    [RelayCommand] private void About() { /* TODO Phase 1: about dialog */ }
    [RelayCommand] private Task StartAsync() => Task.CompletedTask;
    [RelayCommand] private Task StopAsync() => Task.CompletedTask;
}
