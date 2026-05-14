using FluentAssertions;
using SyntheticPen.Core.Models;
using SyntheticPen.Core.Targeting;
using Xunit;

namespace SyntheticPen.Core.Tests.Targeting;

public class TargetRegionProviderTests
{
    [Fact]
    public void Set_updates_Current_and_raises_Changed()
    {
        var p = new TargetRegionProvider();
        Rect? received = null;
        var count = 0;
        p.Changed += r => { received = r; count++; };

        var rect = new Rect(10, 20, 300, 200);
        p.Set(rect);

        p.Current.Should().Be(rect);
        received.Should().Be(rect);
        count.Should().Be(1);
    }

    [Fact]
    public void Set_to_null_clears_Current()
    {
        var p = new TargetRegionProvider();
        p.Set(new Rect(0, 0, 100, 100));
        p.Set(null);
        p.Current.Should().BeNull();
    }
}
