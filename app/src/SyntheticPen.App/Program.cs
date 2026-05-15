using System.Runtime.Versioning;
using Avalonia;
using Microsoft.Extensions.DependencyInjection;
using Projektanker.Icons.Avalonia;
using Projektanker.Icons.Avalonia.FontAwesome;
using Microsoft.Extensions.Hosting;
using SyntheticPen.App.ViewModels;
using SyntheticPen.Core.Playback;
using SyntheticPen.Core.Targeting;
using SyntheticPen.Hotkeys;
using SyntheticPen.Input;
using SyntheticPen.Motion;
using SyntheticPen.Rendering;
using SyntheticPen.Svg;

namespace SyntheticPen.App;

internal static class Program
{
    public static IServiceProvider Services { get; private set; } = null!;

    [STAThread]
    [SupportedOSPlatform("windows")]
    public static void Main(string[] args)
    {
        var host = Host.CreateApplicationBuilder(args);
        host.Services.AddSingleton<ISvgPathLoader, SkiaSvgPathLoader>();
        host.Services.AddSingleton<IMotionPlanner, DefaultMotionPlanner>();
        host.Services.AddSingleton<IStrokePreviewRenderer, StrokePreviewRenderer>();
        host.Services.AddSingleton<ITargetRegionProvider, TargetRegionProvider>();
        host.Services.AddSingleton<InjectorFactory>();
        host.Services.AddSingleton<IPlaybackController>(sp =>
        {
            var factory = sp.GetRequiredService<InjectorFactory>();
            return new PlaybackController(factory.Create(InjectionMode.Mouse), sp.GetRequiredService<IMotionPlanner>());
        });
        host.Services.AddSingleton<IGlobalHotkeyService, GlobalHotkeyService>();
        host.Services.AddSingleton<MainWindowViewModel>();

        Services = host.Build().Services;
        try { Services.GetRequiredService<IGlobalHotkeyService>().Install(); }
        catch { /* fall back: app still runs, hotkey unavailable */ }

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp()
    {
        IconProvider.Current.Register<FontAwesomeIconProvider>();
        return AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
    }
}

public sealed class InjectorFactory
{
    public ICursorInjector Create(InjectionMode mode) => mode switch
    {
        InjectionMode.SyntheticPointer => new SyntheticPointerInjector(),
        _ => new MouseSendInputInjector()
    };
}
