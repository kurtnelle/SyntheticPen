namespace SyntheticPen.Hotkeys;

public interface IGlobalHotkeyService : IDisposable
{
    bool IsInstalled { get; }
    event Action? EmergencyStopRequested;
    void Install();
}
