using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using SyntheticPen.App.ViewModels;

namespace SyntheticPen.App;

public partial class App : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var vm = Program.Services.GetRequiredService<MainWindowViewModel>();
            var banner = new Views.TopBanner();
            var cta = new Views.CalibrateCallToAction();

            void SyncCta() => cta.IsVisible = vm.CtaVisible;
            vm.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(vm.CtaVisible) || e.PropertyName == nameof(vm.HasRegion))
                    Avalonia.Threading.Dispatcher.UIThread.Post(SyncCta);
            };

            desktop.MainWindow = banner;
            banner.Show();
            cta.Show();
            SyncCta();
        }
        base.OnFrameworkInitializationCompleted();
    }
}
