using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using SyntheticPen.App.Win32;
using ModelRect = SyntheticPen.Core.Models.Rect;

namespace SyntheticPen.App.Views;

public partial class CountdownOverlay : Window
{
    private TextBlock _number = null!;

    public CountdownOverlay()
    {
        InitializeComponent();
        _number = this.FindControl<TextBlock>("Number")!;
        Opened += (_, _) => WindowInterop.MakeClickThrough(this);
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

    public void PositionOver(ModelRect target)
    {
        Position = new PixelPoint((int)target.X, (int)target.Y);
        Width = target.W;
        Height = target.H;
        // Scale the digit so it always reads as ~40% of the shorter side.
        var fontSize = Math.Max(36, Math.Min(target.W, target.H) * 0.4);
        _number.FontSize = fontSize;
    }

    public void SetRemaining(TimeSpan remaining)
    {
        var secs = (int)Math.Ceiling(remaining.TotalSeconds);
        Dispatcher.UIThread.Post(() => _number.Text = secs <= 0 ? "GO" : secs.ToString());
    }
}
