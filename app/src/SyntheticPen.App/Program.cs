using Avalonia;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SyntheticPen.App.ViewModels;
using SyntheticPen.Core.Playback;
using SyntheticPen.Core.Targeting;
using SyntheticPen.Input;
using SyntheticPen.Motion;
using SyntheticPen.Rendering;
using SyntheticPen.Svg;

namespace SyntheticPen.App;

internal static class Program
{
    public static IServiceProvider Services { get; private set; } = null!;

    [STAThread]
    public static void Main(string[] args)
    {
        var host = Host.CreateApplicationBuilder(args);
        host.Services.AddSingleton<ISvgPathLoader, SkiaSvgPathLoader>();
        host.Services.AddSingleton<IMotionPlanner, DefaultMotionPlanner>();
        host.Services.AddSingleton<ICursorInjector, MouseSendInputInjector>();
        host.Services.AddSingleton<IStrokePreviewRenderer, StrokePreviewRenderer>();
        host.Services.AddSingleton<IPlaybackController, PlaybackController>();
        host.Services.AddSingleton<ITargetRegionProvider, TargetRegionProvider>();
        host.Services.AddSingleton<MainWindowViewModel>();

        Services = host.Build().Services;

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
