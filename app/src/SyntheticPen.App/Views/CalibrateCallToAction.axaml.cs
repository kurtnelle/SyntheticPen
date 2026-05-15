using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using SyntheticPen.App.ViewModels;

namespace SyntheticPen.App.Views;

public partial class CalibrateCallToAction : Window
{
    public CalibrateCallToAction()
    {
        InitializeComponent();
        DataContext = Program.Services.GetRequiredService<MainWindowViewModel>();
        Opened += (_, _) => CenterOnPrimary();
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

    private void CenterOnPrimary()
    {
        var primary = Screens.Primary;
        if (primary is null) return;
        var area = primary.WorkingArea;
        var x = area.X + (area.Width  - (int)Width)  / 2;
        var y = area.Y + (area.Height - (int)Height) / 2;
        Position = new PixelPoint(x, y);
    }
}
