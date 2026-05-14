using SyntheticPen.Core.Models;

namespace SyntheticPen.Core.Playback;

public interface IPlaybackController
{
    PlaybackState State { get; }
    event Action<PlaybackState> StateChanged;
    event Action<TimeSpan> CountdownTick;
    Task PlayAsync(IReadOnlyList<Stroke> screenStrokes, PlaybackOptions opts, CancellationToken ct = default);
    void RequestStop();
}
