using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Microsoft.Extensions.DependencyInjection;
using SyntheticPen.App.ViewModels;
using SyntheticPen.App.Views;
using ModelRect = SyntheticPen.Core.Models.Rect;

namespace SyntheticPen.App;

public partial class App : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Stay alive until we explicitly shut down: the MainWindow isn't created
            // until the user finishes the initial calibration, so the lifetime can't
            // be tied to "main window closes."
            desktop.ShutdownMode = Avalonia.Controls.ShutdownMode.OnExplicitShutdown;

            Dispatcher.UIThread.Post(async () =>
            {
                var rect = await AwaitCalibrationAsync();
                if (rect is null)
                {
                    desktop.Shutdown();
                    return;
                }

                var window = new MainWindow();
                window.FitPreviewTo(rect.Value);
                window.Closed += (_, _) => desktop.Shutdown();
                Program.ActivateMainWindow = window.BringToFront;
                window.Show();

                // Auto-open the SVG picker right after the user finishes the
                // initial calibration — they almost certainly came here to load
                // an SVG, so skip the extra click. Fire-and-forget; the user
                // can cancel the dialog and use the window normally.
                var vm = Program.Services.GetRequiredService<MainWindowViewModel>();
                if (vm.OpenSvgCommand.CanExecute(null))
                    vm.OpenSvgCommand.Execute(null);
            });
        }
        base.OnFrameworkInitializationCompleted();
    }

    private static System.Threading.Tasks.Task<ModelRect?> AwaitCalibrationAsync()
    {
        var overlay = new CalibrationOverlay();
        var tcs = new System.Threading.Tasks.TaskCompletionSource<ModelRect?>();
        overlay.Closed += (_, _) => tcs.TrySetResult(overlay.SelectedRect);
        overlay.Show();
        overlay.Activate();
        return tcs.Task;
    }
}
