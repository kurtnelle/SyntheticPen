using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using SyntheticPen.App.Win32;
using ModelRect = SyntheticPen.Core.Models.Rect;

namespace SyntheticPen.App.Views;

public partial class PlottingIndicator : Window
{
    public PlottingIndicator()
    {
        InitializeComponent();
        Opened += (_, _) => WindowInterop.MakeClickThrough(this);
    }

    /// <summary>Center the badge horizontally over the plotted region and sit
    /// it just above the region's top edge — where the user is looking —
    /// instead of stranded on the far side of the screen. Drops just inside
    /// the top if there's no room above.</summary>
    public void PositionAbove(ModelRect target)
    {
        int w = (int)Width, h = (int)Height;
        int x = (int)(target.X + target.W / 2.0 - w / 2.0);
        int y = (int)target.Y - h - 8;
        if (y < 0) y = (int)target.Y + 8;
        Position = new PixelPoint(x, y);
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}
