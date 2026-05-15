using System.Runtime.Versioning;
using Avalonia;
using Microsoft.Extensions.DependencyInjection;
using Projektanker.Icons.Avalonia;
using Projektanker.Icons.Avalonia.FontAwesome;
using Microsoft.Extensions.Hosting;
using SyntheticPen.App.ViewModels;
using SyntheticPen.App.Win32;
using SyntheticPen.Core.Playback;
using SyntheticPen.Core.Targeting;
using SyntheticPen.Hotkeys;
using SyntheticPen.Input;
using SyntheticPen.Motion;
using SyntheticPen.Rendering;

namespace SyntheticPen.App;

internal static class Program
{
    public static IServiceProvider Services { get; private set; } = null!;

    /// <summary>Set by App once the MainWindow exists; invoked when another
    /// process (tray helper / second launch) requests we show ourselves.</summary>
    public static Action? ActivateMainWindow { get; set; }

    [STAThread]
    [SupportedOSPlatform("windows")]
    public static void Main(string[] args)
    {
        // Resident hotkey helper — no UI, no DI. Owns Win+Shift+X.
        if (args.Contains("--tray", StringComparer.OrdinalIgnoreCase))
        {
            Environment.Exit(TrayMode.Run());
            return;
        }

        // Single-instance: if we're not the first, ring the doorbell on the
        // live instance and bail so the hotkey/second-launch just focuses the
        // existing window instead of stacking duplicates.
        if (!SingleInstance.TryAcquire())
        {
            SingleInstance.SignalShow();
            return;
        }
        SingleInstance.ListenForShow(() =>
            Avalonia.Threading.Dispatcher.UIThread.Post(() => ActivateMainWindow?.Invoke()));

        var host = Host.CreateApplicationBuilder(args);
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
