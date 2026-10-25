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
// It also looks after the shared folder: asks for it on the first launch,
// remembers it for this computer, and says so plainly if it can't be reached.
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

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);

        // First launch on this computer: ask where the bulletins are kept.
        if (_store == null && string.IsNullOrWhiteSpace(_settings.SharedFolder))
            PickFolder();
    }

    // Closing the calendar closes the form too (the form saves its edits as it closes).
    protected override void OnClosing(WindowClosingEventArgs e)
    {
        _editor?.Close();
        base.OnClosing(e);
    }

    // ───────────────────────────── the shared folder ─────────────────────────────

    // Opens the remembered folder. Never creates the shared folder itself: if it
    // isn't there, say so and let the pastor choose again or try again.
    private void Connect()
    {
        _editor?.Close();
        _editor = null;
        _store = null;
        _corrections = null;

        var folder = _settings.SharedFolder;
        if (string.IsNullOrWhiteSpace(folder))
        {
            ShowFolderMessage(
                "Choose the shared folder where the bulletins are kept, for example a " +
                "folder in OneDrive or Dropbox that every computer can see.",
                isProblem: true, canRetry: false);
        }
        else if (!Directory.Exists(folder))
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
                ShowFolderMessage($"Bulletins are kept in:\n{folder}", isProblem: false, canRetry: false);
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