using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
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

        var close = this.FindControl<Button>("CloseButton");
        if (close is not null) close.Click += (_, _) => ShutdownApp();

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

        // When the banner closes for any reason, close every other window so the user
        // never sees orphan overlays sitting on screen.
        Closing += (_, _) => CloseAllOtherWindows();
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

    private static void ShutdownApp()
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.Shutdown();
    }

    private void CloseAllOtherWindows()
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            return;
        foreach (var w in desktop.Windows.ToArray())
        {
            if (!ReferenceEquals(w, this)) w.Close();
        }
    }
}
