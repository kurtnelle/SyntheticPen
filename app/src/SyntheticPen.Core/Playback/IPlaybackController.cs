using SyntheticPen.Core.Models;

namespace SyntheticPen.Core.Playback;

public interface IPlaybackController
{
    PlaybackState State { get; }
    event Action<PlaybackState>? StateChanged;
    Task PlayAsync(IReadOnlyList<Stroke> strokes, CancellationToken ct = default);
    Task StopAsync(CancellationToken ct = default);
}
