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
    // Captured once at startup so switching back to light still works after
    // the light surface keys have been overwritten by dark brushes.
    private IBrush? _lightSurfaceBrush;
    private IBrush? _lightCardBrush;
    private IBrush? _lightBorderBrush;
    private IBrush? _lightInputBackgroundBrush;
    private IBrush? _lightInputBorderBrush;
    private IBrush? _lightTextBrush;
    private IBrush? _lightLabelBrush;
    private IBrush? _lightMutedBrush;

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

    // Snapshots the light surface brushes before they can ever be overwritten.
    private void CaptureLightBrushes()
    {
        var resources = Resources;
        _lightSurfaceBrush = (IBrush)resources["SurfaceBrush"];
        _lightCardBrush = (IBrush)resources["CardBrush"];
        _lightBorderBrush = (IBrush)resources["BorderBrush"];
        _lightInputBackgroundBrush = (IBrush)resources["InputBackgroundBrush"];
        _lightInputBorderBrush = (IBrush)resources["InputBorderBrush"];
        _lightTextBrush = (IBrush)resources["TextBrush"];
        _lightLabelBrush = (IBrush)resources["LabelBrush"];
        _lightMutedBrush = (IBrush)resources["MutedBrush"];
    }

    // Switches the app theme ("Light" or "Dark"), swaps the surface colour
    // resources and tells the FluentTheme to use the matching variant.
    public void ApplyTheme(string theme)
    {
        bool dark = string.Equals(theme, "Dark", StringComparison.OrdinalIgnoreCase);
        var resources = Resources;

        resources["SurfaceBrush"] = dark ? resources["DarkSurfaceBrush"] : _lightSurfaceBrush;
        resources["CardBrush"] = dark ? resources["DarkCardBrush"] : _lightCardBrush;
        resources["BorderBrush"] = dark ? resources["DarkBorderBrush"] : _lightBorderBrush;
        resources["InputBackgroundBrush"] = dark ? resources["DarkInputBackgroundBrush"] : _lightInputBackgroundBrush;
        resources["InputBorderBrush"] = dark ? resources["DarkInputBorderBrush"] : _lightInputBorderBrush;
        resources["TextBrush"] = dark ? resources["DarkTextBrush"] : _lightTextBrush;
        resources["LabelBrush"] = dark ? resources["DarkLabelBrush"] : _lightLabelBrush;
        resources["MutedBrush"] = dark ? resources["DarkMutedBrush"] : _lightMutedBrush;

        RequestedThemeVariant = dark ? ThemeVariant.Dark : ThemeVariant.Light;

        ThemeChanged?.Invoke(dark ? "Dark" : "Light");
    }
}