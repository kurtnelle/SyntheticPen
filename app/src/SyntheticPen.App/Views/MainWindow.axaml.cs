using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using SyntheticPen.App.ViewModels;
using SyntheticPen.Core.Targeting;
using ModelRect = SyntheticPen.Core.Models.Rect;

namespace SyntheticPen.App.Views;

/// <summary>
/// Single-window UI styled as two rounded panels (preview on top, controls on bottom)
/// with a transparent gap between them. The window itself is borderless and transparent;
/// the controls bar serves as the drag handle, and four corner grips on the preview
/// drive resize.
/// </summary>
public partial class MainWindow : Window
{
    private readonly ITargetRegionProvider _regions;
    private Grid? _previewArea;
    private ModelRect? _pendingFit;

    public MainWindow()
    {
        InitializeComponent();
        var vm = Program.Services.GetRequiredService<MainWindowViewModel>();
        DataContext = vm;
        vm.AttachMainWindow(this);
        _regions = Program.Services.GetRequiredService<ITargetRegionProvider>();

        _previewArea = this.FindControl<Grid>("PreviewArea");

        // Drag-to-move: pressing anywhere on the controls bar moves the whole window.
        var dragBar = this.FindControl<Border>("DragBar");
        if (dragBar is not null)
            dragBar.PointerPressed += (_, e) =>
            {
                if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
                    BeginMoveDrag(e);
            };

        // Resize from the four corner grips on the preview area.
        WireGrip("GripNW", WindowEdge.NorthWest);
        WireGrip("GripNE", WindowEdge.NorthEast);
        WireGrip("GripSW", WindowEdge.SouthWest);
        WireGrip("GripSE", WindowEdge.SouthEast);

        var close = this.FindControl<Button>("CloseButton");
        if (close is not null) close.Click += (_, _) => ShutdownApp();

        Opened += (_, _) => PushRegion();
        PositionChanged += (_, _) => PushRegion();
        if (_previewArea is not null)
        {
            _previewArea.LayoutUpdated += (_, _) =>
            {
                if (_pendingFit is { } pf && _previewArea.Bounds.Width > 0)
                {
                    _pendingFit = null;
                    ApplyFit(pf);
                }
                PushRegion();
            };
        }
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

    private void WireGrip(string name, WindowEdge edge)
    {
        var grip = this.FindControl<Rectangle>(name);
        if (grip is null) return;
        grip.PointerPressed += (_, e) =>
        {
            if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
                BeginResizeDrag(edge, e);
        };
    }

    /// <summary>Compute the preview's screen-space rectangle and push it into the region provider.</summary>
    private void PushRegion()
    {
        if (_previewArea is null) return;
        var b = _previewArea.Bounds;
        if (b.Width <= 0 || b.Height <= 0) return;
        try
        {
            var topLeft = _previewArea.PointToScreen(new Point(0, 0));
            var rect = new ModelRect(topLeft.X, topLeft.Y, b.Width, b.Height);
            _regions.Set(rect);
        }
        catch
        {
            // PointToScreen can throw before the visual tree is fully attached.
        }
    }

    /// <summary>Resize and reposition the window so the preview area matches the given screen rect.</summary>
    public void FitPreviewTo(ModelRect target)
    {
        if (_previewArea is null || _previewArea.Bounds.Width <= 0)
        {
            _pendingFit = target;
            // Pre-position before first layout so the window doesn't flash at (0,0)
            // under WindowStartupLocation=Manual. ApplyFit will refine this once
            // the preview area is measured.
            Position = new PixelPoint((int)target.X, (int)target.Y);
            return;
        }
        ApplyFit(target);
    }

    private void ApplyFit(ModelRect target)
    {
        if (_previewArea is null) return;

        // Chrome offset in screen pixels — preview's top-left relative to the
        // window's outer top-left. With SystemDecorations=None this is just the
        // root margin (typically zero), but PointToScreen is the safe way.
        var previewScreenTL = _previewArea.PointToScreen(new Point(0, 0));
        var chromeLeft = previewScreenTL.X - Position.X;
        var chromeTop = previewScreenTL.Y - Position.Y;

        // Outer size beyond the preview area (gap + controls bar).
        var extraW = Width - _previewArea.Bounds.Width;
        var extraH = Height - _previewArea.Bounds.Height;

        Width = target.W + extraW;
        Height = target.H + extraH;
        Position = new PixelPoint(
            (int)(target.X - chromeLeft),
            (int)(target.Y - chromeTop));
    }

    private static void ShutdownApp()
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.Shutdown();
    }
}
