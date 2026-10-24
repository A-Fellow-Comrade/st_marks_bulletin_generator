using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using st_mark_bulletin_generator.Data;
using st_mark_bulletin_generator.Views;

namespace st_mark_bulletin_generator;

public partial class App : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new CalendarWindow(AppSettings.Load());
        }
        base.OnFrameworkInitializationCompleted();
    }
}