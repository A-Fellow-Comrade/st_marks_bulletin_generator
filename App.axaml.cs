using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Styling;
using st_mark_bulletin_generator.Data;
using st_mark_bulletin_generator.Views;

namespace st_mark_bulletin_generator;

public partial class App : Application
{
    // The colours that differ between light and dark mode. App.axaml defines each one
    // twice: "<Name>" for light and "Dark<Name>" for dark. To add another themed colour,
    // define both in App.axaml and add its name here.
    private static readonly string[] ThemedBrushes =
    {
        "SurfaceBrush", "CardBrush", "BorderBrush", "InputBackgroundBrush", "InputBorderBrush",
        "TextBrush", "LabelBrush", "MutedBrush",
        "RubricBrush", "GoldBrush", "HeaderBrush", "PrimaryBrush", "PrimaryHoverBrush",
        "SeasonWhiteBrush", "SeasonGreenBrush", "SeasonVioletBrush", "SeasonRedBrush",
    };

    // Captured once at startup so switching back to light still works after
    // the light keys have been overwritten by dark brushes.
    private readonly Dictionary<string, IBrush> _lightBrushes = new();

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    // Raised whenever the light/dark theme is switched, so open windows can
    // refresh anything that isn't driven by DynamicResource.
    public event Action<string>? ThemeChanged;

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // This computer's own settings (which shared folder to use). The
            // calendar window opens the folder, or asks for it on the first launch.
            var settings = AppSettings.Load();
            CaptureLightBrushes();
            ApplyTheme(settings.Theme);
            desktop.MainWindow = new CalendarWindow(settings);
        }

        base.OnFrameworkInitializationCompleted();
    }

    // Snapshots the light brushes before they can ever be overwritten.
    private void CaptureLightBrushes()
    {
        foreach (var name in ThemedBrushes)
            _lightBrushes[name] = (IBrush)Resources[name]!;
    }

    // Switches the app theme ("Light" or "Dark"), swaps the colour
    // resources and tells the FluentTheme to use the matching variant.
    public void ApplyTheme(string theme)
    {
        bool dark = string.Equals(theme, "Dark", StringComparison.OrdinalIgnoreCase);
        var resources = Resources;

        foreach (var name in ThemedBrushes)
            resources[name] = dark ? resources["Dark" + name] : _lightBrushes[name];

        RequestedThemeVariant = dark ? ThemeVariant.Dark : ThemeVariant.Light;

        ThemeChanged?.Invoke(dark ? "Dark" : "Light");
    }
}