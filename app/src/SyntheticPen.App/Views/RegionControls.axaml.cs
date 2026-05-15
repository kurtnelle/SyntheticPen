using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using SyntheticPen.App.ViewModels;
using ModelRect = SyntheticPen.Core.Models.Rect;

namespace SyntheticPen.App.Views;

public partial class RegionControls : Window
{
    // Minimum width that comfortably fits the toolbar without clipping the combo + buttons.
    private const int MinBarWidth = 760;

    public RegionControls()
    {
        InitializeComponent();
        DataContext = Program.Services.GetRequiredService<MainWindowViewModel>();

        var close = this.FindControl<Button>("CloseButton");
        if (close is not null) close.Click += (_, _) => ShutdownApp();

        // Closing the controls bar shuts the app down (it's the only chrome the user has).
        Closing += (_, _) => ShutdownApp();
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

    /// <summary>Position the bar directly below the target region, centered on it horizontally.</summary>
    public void PositionBelow(ModelRect target)
    {
        int width = Math.Max(MinBarWidth, (int)target.W);
        int height = (int)Height;
        int centerX = (int)(target.X + target.W / 2.0);
        int x = centerX - width / 2;
        int y = (int)(target.Y + target.H + 4);

        // Clamp to primary monitor working area so the bar never sails off-screen.
        var primary = Screens.Primary;
        if (primary is not null)
        {
            var area = primary.WorkingArea;
            x = Math.Max(area.X, Math.Min(area.X + area.Width - width, x));
            if (y + height > area.Y + area.Height)
                y = (int)target.Y - height - 4;  // flip above if no room below
        }

        Width = width;
        Position = new PixelPoint(x, y);
    }

    private static void ShutdownApp()
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.Shutdown();
    }
}
