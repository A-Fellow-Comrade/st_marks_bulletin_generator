using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using LiturgyTools;
using st_mark_bulletin_generator.Data;
using st_mark_bulletin_generator.Liturgical;
using st_mark_bulletin_generator.Models;

namespace st_mark_bulletin_generator.Views;

// The main window: a month calendar where only Sundays can be clicked.
// Sundays that have a saved bulletin are gold, and each Sunday has an edge in the color
// of its place in the church year. Clicking a Sunday opens (or
// re-uses) the bulletin form for that Sunday.
//
// It also looks after the bulletin folder. On the first launch it simply uses
// Documents\StMarkBulletins, so nobody has to choose anything. Whoever sets up
// a second computer can press "Choose folder..." and pick a synced folder. The
// choice is remembered for this computer, and if the folder can't be reached
// later, the window says so plainly.
public partial class CalendarWindow : Window
{
    private static readonly IBrush SelectedFg    = Brushes.White;
    private static readonly IBrush ProblemBrush  = new SolidColorBrush(Color.Parse("#FFF3CD"));

    private readonly AppSettings _settings;
    private BulletinStore? _store;             // null until a reachable folder is chosen
    private HymnCorrections? _corrections;
    private ChurchSettingsStore? _churchStore;  // the church's own wording, kept in the shared folder
    private MainWindow? _editor;               // the bulletin form, when it is open
    private DateOnly _month;                   // first day of the month being shown
    private DateOnly _selected;                // the Sunday that is selected

    //added so Avalonia's XAML loader and previewer can find the window
    public CalendarWindow() => throw new InvalidOperationException(
        "CalendarWindow needs the app settings: use CalendarWindow(AppSettings).");
    public CalendarWindow(AppSettings settings)
    {
        _settings = settings;
        InitializeComponent();

        _selected = CalendarMath.FirstSundayOnOrAfter(DateOnly.FromDateTime(DateTime.Today));
        _month = new DateOnly(_selected.Year, _selected.Month, 1);
        Connect();

        UpdateThemeToggle();
        // If the bulletin form switches theme, refresh this window and keep the
        // toggle label in sync.
        ((App)Application.Current).ThemeChanged += _ =>
        {
            UpdateThemeToggle();
            Render();
        };
    }

    // Closing the calendar closes the form too (the form saves its edits as it closes).
    protected override void OnClosing(WindowClosingEventArgs e)
    {
        _editor?.Close();
        base.OnClosing(e);
    }

    // ───────────────────────────── the shared folder ─────────────────────────────

    // The folder used when nobody has chosen one: Documents\StMarkBulletins.
    private static string DefaultFolder()
    {
        var documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        if (string.IsNullOrWhiteSpace(documents))
            documents = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(documents, "StMarkBulletins");
    }

    // Opens the remembered folder. On the very first launch there is none yet,
    // so the standard one is created and remembered. After that the folder is
    // never created again: if it has gone missing (a OneDrive folder that isn't
    // available, an unplugged drive), say so and let the person choose again or
    // try again, rather than quietly starting over in an empty folder.
    private void Connect()
    {
        _editor?.Close();
        _editor = null;
        _store = null;
        _corrections = null;
        _churchStore = null;

        var folder = _settings.SharedFolder;
        if (string.IsNullOrWhiteSpace(folder))
        {
            try
            {
                folder = DefaultFolder();
                Directory.CreateDirectory(folder);
                _settings.SharedFolder = folder;
                _settings.Save();
            }
            catch (Exception ex)
            {
                _settings.SharedFolder = null;
                ShowFolderMessage(
                    $"Couldn't set up the bulletin folder ({ex.Message}). " +
                    "Please press \"Choose folder...\" and pick a folder for the bulletins.",
                    isProblem: true, canRetry: false);
                Render();
                return;
            }
        }

        if (!Directory.Exists(folder))
        {
            ShowFolderMessage(
                $"Can't reach the bulletin folder:\n{folder}\n" +
                "If it is on OneDrive or a network drive, make sure it is available, then try again.",
                isProblem: true, canRetry: true);
        }
        else
        {
            try
            {
                _store = new BulletinStore(Path.Combine(folder, "Bulletins"));
                _corrections = new HymnCorrections(
                    Path.Combine(AppSettings.DataFolder, "hymn_corrections_local.tsv"),
                    Path.Combine(folder, "hymn_corrections.tsv"));
                _churchStore = new ChurchSettingsStore(Path.Combine(folder, "church_settings.json"));
                ShowFolderMessage(
                    $"Bulletins are kept in:\n{folder}\n" +
                    "(Only needed when setting up a second computer: press \"Choose folder...\" " +
                    "and pick a folder that both computers can see.)",
                    isProblem: false, canRetry: false);
            }
            catch (Exception ex)
            {
                _store = null;
                _corrections = null;
                _churchStore = null;
                ShowFolderMessage($"Couldn't use the folder {folder}: {ex.Message}", isProblem: true, canRetry: true);
            }
        }

        Render();
    }

    private void ShowFolderMessage(string text, bool isProblem, bool canRetry)
    {
        FolderText.Text = text;
        FolderBorder.Background = isProblem ? ProblemBrush : null;
        FolderBorder.BorderBrush = isProblem ? new SolidColorBrush(Color.Parse("#F0C040")) : new SolidColorBrush(Color.Parse("#E0D8D0"));
        RetryButton.IsVisible = canRetry;
    }

    // Opens the church settings (the wording printed on every bulletin). They live in
    // the shared folder, so this needs a reachable folder.
    private async void Settings_Click(object? sender, RoutedEventArgs e)
    {
        if (_churchStore == null) return;
        await new SettingsWindow(_churchStore).ShowDialog<bool>(this);
    }


    private void ChooseFolder_Click(object? sender, RoutedEventArgs e) => PickFolder();

    private void Retry_Click(object? sender, RoutedEventArgs e) => Connect();

    private async void PickFolder()
    {
        try
        {
            var picked = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "Choose the shared folder for the St. Mark bulletins",
                AllowMultiple = false,
            });
            if (picked.Count == 0) return;

            var path = picked[0].Path.LocalPath;
            if (string.IsNullOrWhiteSpace(path))
            {
                ShowFolderMessage("That folder can't be used. Please choose a folder on this computer, or in a synced folder.",
                    isProblem: true, canRetry: false);
                return;
            }

            _settings.SharedFolder = path;
            _settings.Save();
            Connect();
        }
        catch (Exception ex)
        {
            ShowFolderMessage($"Couldn't choose the folder: {ex.Message}", isProblem: true, canRetry: false);
        }
    }

    // Switches light/dark mode, persists the choice, and refreshes both windows.
    private void ThemeToggle_Click(object? sender, RoutedEventArgs e)
    {
        var next = _settings.Theme == "Dark" ? "Light" : "Dark";
        _settings.Theme = next;
        _settings.Save();

        var app = (App)Application.Current;
        app.ApplyTheme(next);
        UpdateThemeToggle();
        Render();
    }

    // The button offers the mode you would switch to: a sun and "Light" while the
    // app is dark, a moon and "Dark" while it is light.
    private void UpdateThemeToggle()
    {
        bool dark = _settings.Theme == "Dark";
        ThemeLabel.Text = dark ? "Light" : "Dark";
        ThemeSunIcon.IsVisible = dark;
        ThemeMoonIcon.IsVisible = !dark;
    }

    // ───────────────────────────── months and Sundays ─────────────────────────────

    private void PrevMonth_Click(object? sender, RoutedEventArgs e)
    {
        _month = _month.AddMonths(-1);
        Render();
    }

    private void NextMonth_Click(object? sender, RoutedEventArgs e)
    {
        _month = _month.AddMonths(1);
        Render();
    }

    //steps one week from the sunday that is selected
    private void PrevSunday_Click(object? sender, RoutedEventArgs e) => Choose(_selected.AddDays(-7));
    private void NextSunday_Click(object? sender, RoutedEventArgs e) => Choose(_selected.AddDays(7));
   
    //goes back to the sunday that is coming up
    private void ThisSunday_Click(object? sender, RoutedEventArgs e) =>
        Choose(CalendarMath.FirstSundayOnOrAfter(DateOnly.FromDateTime(DateTime.Today)));

    // Selects a Sunday and shows it in the bulletin form.
    private void Choose(DateOnly sunday)
    {
        if (_store == null || _corrections == null || _churchStore == null) return;

        _selected = sunday;
        _month = new DateOnly(sunday.Year, sunday.Month, 1);
        Render();
        OpenEditor(sunday, _store, _corrections, _churchStore);
    }

    private void OpenEditor(DateOnly sunday, BulletinStore store, HymnCorrections corrections, ChurchSettingsStore churchStore)
    {
        if (_editor == null)
        {
            _editor = new MainWindow(store, corrections, churchStore, _settings);
            _editor.BulletinSaved += Render;               // keeps the gold marks up to date
            _editor.Closed += (_, _) => _editor = null;
            _editor.ShowSunday(sunday);
            _editor.Show();
        }
        else
        {
            _editor.ShowSunday(sunday);
            _editor.Activate();
        }
    }

    // A color of the current theme (the theme swaps these when light or dark is chosen).
    private static IBrush ThemeBrush(string key) => (IBrush)Application.Current!.Resources[key]!;

    // The wash on Sundays that have a saved bulletin: the theme's gold, see-through.
    private static IBrush SavedWash() =>
        new SolidColorBrush(((ISolidColorBrush)ThemeBrush("GoldBrush")).Color, 0.35);

    private HashSet<DateOnly> SavedSundays()
    {
        try { return _store == null ? new HashSet<DateOnly>() : new HashSet<DateOnly>(_store.ListDates()); }
        catch { return new HashSet<DateOnly>(); }
    }

    // Draws the month: six weeks, Sundays as buttons, other days greyed out.
    private void Render()
    {
        MonthTitle.Text = _month.ToString("MMMM yyyy", CultureInfo.InvariantCulture);
        var saved = SavedSundays();

        // Nothing can be opened until a reachable folder is chosen.
        WeekGrid.IsEnabled = _store != null;
        SettingsButton.IsEnabled = _churchStore != null;
        PrevSundayButton.IsEnabled = _store != null;
        ThisSundayButton.IsEnabled = _store != null;
        NextSundayButton.IsEnabled = _store != null;

        WeekGrid.Children.Clear();
        var first = _month.AddDays(-(int)_month.DayOfWeek);   // the Sunday on or before the 1st
        for (int i = 0; i < 42; i++)
        {
            var date = first.AddDays(i);
            Control cell = date.DayOfWeek == DayOfWeek.Sunday
                ? MakeSundayButton(date, saved)
                : MakeGreyDay(date);
            Grid.SetRow(cell, i / 7);
            Grid.SetColumn(cell, i % 7);
            WeekGrid.Children.Add(cell);
        }

        ShowSelected(saved);
    }

    private Control MakeSundayButton(DateOnly date, HashSet<DateOnly> saved)
    {
        var season = ChurchYear.For(date);

        var button = new Button
        {
            Content = date.Day.ToString(CultureInfo.InvariantCulture),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(2),
            Padding = new Thickness(0, 8),
        };
        if (saved.Contains(date)) button.Background = SavedWash();
        if (date == _selected)
        {
            button.Background = ThemeBrush("PrimaryBrush");
            button.Foreground = SelectedFg;
        }
        button.Click += (_, _) => Choose(date);
        ToolTip.SetTip(button, $"{season.Name} ({season.ColorWord})");

        // The edge in the color of the church year. It sits on top of the button but
        // lets clicks through, so the whole cell still works as one button.
        var edge = new Border
        {
            Width = 4,
            CornerRadius = new CornerRadius(2),
            Background = SeasonBrushes.For(season.Color),
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Stretch,
            Margin = new Thickness(6, 8, 0, 8),
            IsHitTestVisible = false,
        };

        var cell = new Grid { Opacity = date.Month == _month.Month ? 1.0 : 0.55 };
        cell.Children.Add(button);
        cell.Children.Add(edge);
        return cell;
    }

    private static Control MakeGreyDay(DateOnly date) => new TextBlock
    {
        Text = date.Day.ToString(CultureInfo.InvariantCulture),
        HorizontalAlignment = HorizontalAlignment.Center,
        VerticalAlignment = VerticalAlignment.Center,
        Margin = new Thickness(0, 8),
        Opacity = 0.25,
    };

    // The lectionary a Sunday uses: its own saved bulletin's, otherwise the nearest
    // earlier one's (as the form does when it starts a new Sunday), otherwise three-year.
    private Lectionary LectionaryFor(DateOnly date)
    {
        try
        {
            if (_store == null) return Lectionary.ThreeYear;

            var record = _store.Load(date);

            if (record == null && _store.FindNearestEarlier(date) is DateOnly earlier)
                record = _store.Load(earlier);

            return LectionaryChoice.FromText(record?.Lectionary);
        }
        catch
        {
            return Lectionary.ThreeYear;
        }
    }

    private void ShowSelected(HashSet<DateOnly> saved)
    {
        SelectedTitle.Text = _selected.ToString("dddd, MMMM d, yyyy", CultureInfo.InvariantCulture);

        var name = SundayNames.Get(_selected, LectionaryFor(_selected), out _);
        var status = saved.Contains(_selected) ? "Saved bulletin" : "Not started yet";
        SelectedInfo.Text = name.Length > 0
            ? $"{char.ToUpperInvariant(name[0])}{name[1..]}. {status}."
            : $"{status}.";

        // The color of the church year, written out as well as shown.
        var season = ChurchYear.For(_selected);
        SelectedSeason.Text = $"{season.Name} · {season.ColorWord}";
        SelectedEdge.Background = SeasonBrushes.For(season.Color);
    }
}