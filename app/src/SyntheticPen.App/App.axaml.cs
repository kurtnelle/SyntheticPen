using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
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
            // No persistent main window. Calibration is the first user-facing surface;
            // RegionPreview + RegionControls appear once a region is committed.
            // The VM calls desktop.Shutdown() on close-button or initial-Esc.
            desktop.ShutdownMode = Avalonia.Controls.ShutdownMode.OnExplicitShutdown;

            var vm = Program.Services.GetRequiredService<MainWindowViewModel>();

            // Kick off the initial calibrate after the framework has finished initializing.
            Dispatcher.UIThread.Post(async () =>
            {
                // Brief pause so the bundled demo SVG has a chance to parse/render before
                // the calibration overlay grabs the screen.
                await System.Threading.Tasks.Task.Delay(150);
                await vm.RunInitialFlowAsync();
            });
        }
        base.OnFrameworkInitializationCompleted();
    }
}
