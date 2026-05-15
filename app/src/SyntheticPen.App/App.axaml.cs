using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
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
                // Brief pause so the bundled demo SVG has time to parse/render
                // before the calibration overlay grabs the screen.
                await System.Threading.Tasks.Task.Delay(150);

                var rect = await AwaitCalibrationAsync();
                if (rect is null)
                {
                    desktop.Shutdown();
                    return;
                }

                var window = new MainWindow();
                window.FitPreviewTo(rect.Value);
                window.Closed += (_, _) => desktop.Shutdown();
                window.Show();
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
