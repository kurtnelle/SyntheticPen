using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using SyntheticPen.App.ViewModels;

namespace SyntheticPen.App.Views;

public partial class TopBanner : Window
{
    public TopBanner()
    {
        InitializeComponent();
        DataContext = Program.Services.GetRequiredService<MainWindowViewModel>();
        Opened += (_, _) =>
        {
            var primary = Screens.Primary;
            if (primary is not null)
            {
                var area = primary.WorkingArea;
                Position = new PixelPoint(area.X, area.Y);
                Width = area.Width;
            }
        };
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}
