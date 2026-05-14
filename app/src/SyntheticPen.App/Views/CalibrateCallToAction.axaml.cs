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
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}
