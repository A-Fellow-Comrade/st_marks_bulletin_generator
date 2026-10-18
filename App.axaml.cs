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
            //saved bulletins: one file per per sunda
            var store = new BulletinStore(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "St. Mark Bulletins", "Bulletins"));
            //the calendar is the main window, and the form opens from it
            desktop.MainWindow = new CalendarWindow(store);
        }
        base.OnFrameworkInitializationCompleted();
    }
}