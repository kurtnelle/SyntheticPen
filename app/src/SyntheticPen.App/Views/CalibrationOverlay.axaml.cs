using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using ModelRect = SyntheticPen.Core.Models.Rect;

namespace SyntheticPen.App.Views;

public partial class CalibrationOverlay : Window
{
    private Point? _dragStart;
    private Rectangle _selRect = null!;
    private Border _readout = null!;
    private TextBlock _dimLabel = null!;
    private TextBlock _originLabel = null!;

    public ModelRect? SelectedRect { get; private set; }

    public CalibrationOverlay()
    {
        InitializeComponent();
        _selRect = this.FindControl<Rectangle>("SelectionRect")!;
        _readout = this.FindControl<Border>("ReadoutChip")!;
        _dimLabel = this.FindControl<TextBlock>("DimLabel")!;
        _originLabel = this.FindControl<TextBlock>("OriginLabel")!;

        var bounds = ComputeVirtualBounds(Screens);
        Position = new PixelPoint(bounds.X, bounds.Y);
        Width = bounds.Width;
        Height = bounds.Height;

        PointerPressed += OnPointerPressed;
        PointerMoved += OnPointerMoved;
        PointerReleased += OnPointerReleased;
        KeyDown += OnKeyDown;
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

    private static PixelRect ComputeVirtualBounds(Screens screens)
    {
        int minX = int.MaxValue, minY = int.MaxValue, maxX = int.MinValue, maxY = int.MinValue;
        foreach (var s in screens.All)
        {
            var b = s.Bounds;
            if (b.X < minX) minX = b.X;
            if (b.Y < minY) minY = b.Y;
            if (b.X + b.Width > maxX) maxX = b.X + b.Width;
            if (b.Y + b.Height > maxY) maxY = b.Y + b.Height;
        }
        if (minX == int.MaxValue) return new PixelRect(0, 0, 1920, 1080);
        return new PixelRect(minX, minY, maxX - minX, maxY - minY);
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var props = e.GetCurrentPoint(this).Properties;
        if (props.IsRightButtonPressed)
        {
            ClearSelection();
            return;
        }
        _dragStart = e.GetPosition(this);
        _selRect.IsVisible = true;
        _readout.IsVisible = true;
        UpdateRect(e.GetPosition(this));
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_dragStart is null) return;
        UpdateRect(e.GetPosition(this));
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_dragStart is null) return;
        var rect = NormalizeRect(_dragStart.Value, e.GetPosition(this));
        _dragStart = null;
        if (rect.Width >= 4 && rect.Height >= 4)
        {
            var topLeft = Position;
            SelectedRect = new ModelRect(
                topLeft.X + rect.X,
                topLeft.Y + rect.Y,
                rect.Width,
                rect.Height);
            Close();
        }
        else
        {
            ClearSelection();
        }
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            SelectedRect = null;
            Close();
        }
    }

    private void UpdateRect(Point current)
    {
        var rect = NormalizeRect(_dragStart!.Value, current);
        Canvas.SetLeft(_selRect, rect.X);
        Canvas.SetTop(_selRect, rect.Y);
        _selRect.Width = rect.Width;
        _selRect.Height = rect.Height;

        _dimLabel.Text = $"{(int)rect.Width} × {(int)rect.Height}";
        _originLabel.Text = $"({(int)rect.X}, {(int)rect.Y})";
        Canvas.SetLeft(_readout, rect.X + rect.Width + 8);
        Canvas.SetTop(_readout, rect.Y + rect.Height + 8);
    }

    private void ClearSelection()
    {
        _dragStart = null;
        _selRect.IsVisible = false;
        _readout.IsVisible = false;
    }

    private static Avalonia.Rect NormalizeRect(Point a, Point b)
    {
        var x = Math.Min(a.X, b.X);
        var y = Math.Min(a.Y, b.Y);
        var w = Math.Abs(a.X - b.X);
        var h = Math.Abs(a.Y - b.Y);
        return new Avalonia.Rect(x, y, w, h);
    }
}
