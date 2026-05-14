using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;

namespace SyntheticPen.App.Views;

public partial class CountdownOverlay : Window
{
    private TextBlock _number = null!;

    public CountdownOverlay()
    {
        InitializeComponent();
        _number = this.FindControl<TextBlock>("Number")!;
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

    public void SetRemaining(TimeSpan remaining)
    {
        var secs = (int)Math.Ceiling(remaining.TotalSeconds);
        Dispatcher.UIThread.Post(() => _number.Text = secs <= 0 ? "GO" : secs.ToString());
    }
}
