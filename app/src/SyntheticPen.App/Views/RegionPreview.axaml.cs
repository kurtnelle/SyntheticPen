using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using SyntheticPen.App.ViewModels;
using SyntheticPen.App.Win32;
using ModelRect = SyntheticPen.Core.Models.Rect;

namespace SyntheticPen.App.Views;

public partial class RegionPreview : Window
{
    public RegionPreview()
    {
        InitializeComponent();
        DataContext = Program.Services.GetRequiredService<MainWindowViewModel>();
        Opened += (_, _) => WindowInterop.MakeClickThrough(this);
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

    public void PositionOver(ModelRect target)
    {
        Position = new PixelPoint((int)target.X, (int)target.Y);
        Width = target.W;
        Height = target.H;
    }
}
