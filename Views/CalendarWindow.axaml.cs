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

namespace st_mark_bulletin_generator.Views;

// The main window: a month calendar where only Sundays can be clicked.
// Sundays that have a saved bulletin are gold. Clicking a Sunday opens (or
// re-uses) the bulletin form for that Sunday.
//
// It also looks after the bulletin folder. On the first launch it simply uses
// Documents\StMarkBulletins, so nobody has to choose anything. Whoever sets up
// a second computer can press "Choose folder..." and pick a synced folder. The
// choice is remembered for this computer, and if the folder can't be reached
// later, the window says so plainly.
public partial class CalendarWindow : Window
{
    private static readonly IBrush SavedBrush = new SolidColorBrush(Color.Parse("#66FFB300"));
    private static readonly IBrush SelectedBrush = Brushes.DodgerBlue;
    private static readonly IBrush QuietBrush = new SolidColorBrush(Color.Parse("#20808080"));
    private static readonly IBrush ProblemBrush = new SolidColorBrush(Color.Parse("#40FFB300"));

    private readonly AppSettings _settings;
    private BulletinStore? _store;             // null until a reachable folder is chosen
    private HymnCorrections? _corrections;
    private MainWindow? _editor;               // the bulletin form, when it is open
    private DateOnly _month;                   // first day of the month being shown
    private DateOnly _selected;                // the Sunday that is selected

    public CalendarWindow(AppSettings settings)
    {
        _settings = settings;
        InitializeComponent();

        _selected = CalendarMath.FirstSundayOnOrAfter(DateOnly.FromDateTime(DateTime.Today));
        _month = new DateOnly(_selected.Year, _selected.Month, 1);
        Connect();
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
                ShowFolderMessage($"Couldn't use the folder {folder}: {ex.Message}", isProblem: true, canRetry: true);
            }
        }

        Render();
    }

    private void ShowFolderMessage(string text, bool isProblem, bool canRetry)
    {
        FolderText.Text = text;
        FolderBorder.Background = isProblem ? ProblemBrush : QuietBrush;
        RetryButton.IsVisible = canRetry;
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

    private void NextSunday_Click(object? sender, RoutedEventArgs e) =>
        Choose(CalendarMath.FirstSundayOnOrAfter(DateOnly.FromDateTime(DateTime.Today)));

    // Selects a Sunday and shows it in the bulletin form.
    private void Choose(DateOnly sunday)
    {
        if (_store == null || _corrections == null) return;

        _selected = sunday;
        _month = new DateOnly(sunday.Year, sunday.Month, 1);
        Render();
        OpenEditor(sunday, _store, _corrections);
    }

    private void OpenEditor(DateOnly sunday, BulletinStore store, HymnCorrections corrections)
    {
        if (_editor == null)
        {
            _editor = new MainWindow(store, corrections);
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
        var button = new Button
        {
            Content = date.Day.ToString(CultureInfo.InvariantCulture),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(2),
            Padding = new Thickness(0, 8),
            Opacity = date.Month == _month.Month ? 1.0 : 0.55,
        };
        if (saved.Contains(date)) button.Background = SavedBrush;
        if (date == _selected)
        {
            button.BorderBrush = SelectedBrush;
            button.BorderThickness = new Thickness(2);
        }
        button.Click += (_, _) => Choose(date);
        return button;
    }

    private static Control MakeGreyDay(DateOnly date) => new TextBlock
    {
        Text = date.Day.ToString(CultureInfo.InvariantCulture),
        HorizontalAlignment = HorizontalAlignment.Center,
        VerticalAlignment = VerticalAlignment.Center,
        Margin = new Thickness(0, 8),
        Opacity = 0.25,
    };

    private void ShowSelected(HashSet<DateOnly> saved)
    {
        SelectedTitle.Text = _selected.ToString("dddd, MMMM d, yyyy", CultureInfo.InvariantCulture);

        var name = SundayNames.Get(_selected, out _);
        var status = saved.Contains(_selected) ? "Saved bulletin" : "Not started yet";
        SelectedInfo.Text = name.Length > 0
            ? $"{char.ToUpperInvariant(name[0])}{name[1..]}. {status}."
            : $"{status}.";
    }
}