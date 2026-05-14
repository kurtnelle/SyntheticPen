using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using SyntheticPen.App.Win32;

namespace SyntheticPen.App.Views;

public partial class PlottingIndicator : Window
{
    public PlottingIndicator()
    {
        InitializeComponent();
        var primary = Screens.Primary;
        if (primary is not null)
        {
            var area = primary.WorkingArea;
            Position = new PixelPoint(area.X + area.Width - 240, area.Y + 20);
        }
        Opened += (_, _) => WindowInterop.MakeClickThrough(this);
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}
