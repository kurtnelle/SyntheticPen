using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using SyntheticPen.App.Win32;

namespace SyntheticPen.App.Views;

public partial class AboutDialog : Window
{
    private bool _suppress;

    public AboutDialog()
    {
        InitializeComponent();
        var check = this.FindControl<CheckBox>("AutostartCheck");
        if (check is not null)
        {
            // Reflect the live registry state without re-triggering the handler.
            _suppress = true;
            check.IsChecked = Autostart.IsEnabled;
            _suppress = false;
        }
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

    private void OnAutostartChanged(object? sender, RoutedEventArgs e)
    {
        if (_suppress || sender is not CheckBox cb) return;
        if (cb.IsChecked == true) Autostart.Enable();
        else Autostart.Disable();
    }

    private void OnClose(object? sender, RoutedEventArgs e) => Close();
}
