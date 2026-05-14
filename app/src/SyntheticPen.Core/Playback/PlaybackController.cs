using SyntheticPen.Core.Models;

namespace SyntheticPen.Core.Playback;

public sealed class PlaybackController : IPlaybackController
{
    public PlaybackState State { get; private set; } = PlaybackState.Idle;

#pragma warning disable CS0067 // event never used — wired up in Phase 1
    public event Action<PlaybackState>? StateChanged;
#pragma warning restore CS0067

    public Task PlayAsync(IReadOnlyList<Stroke> strokes, CancellationToken ct = default)
        => throw new NotImplementedException("Phase 1");

    public Task StopAsync(CancellationToken ct = default)
        => throw new NotImplementedException("Phase 1");
}
