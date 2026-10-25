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
            // This computer's own settings (which shared folder to use). The
            // calendar window opens the folder, or asks for it on the first launch.
            desktop.MainWindow = new CalendarWindow(AppSettings.Load());
        }


        base.OnFrameworkInitializationCompleted();
    }
}