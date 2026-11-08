using System.Diagnostics;
using System.Globalization;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using QuestPDF.Fluent;
using st_mark_bulletin_generator.Data;
using st_mark_bulletin_generator.Liturgical;
using st_mark_bulletin_generator.Models;
using st_mark_bulletin_generator.Pdf;

namespace st_mark_bulletin_generator.Views;

public partial class MainWindow : Window
{
    // Page in the Lutheran Service Book where each Divine Service setting
    // starts. Edit a number here if the book says otherwise.
    private static readonly Dictionary<string, int> SettingPages = new()
    {
        ["Setting One"]   = 151,
        ["Setting Two"]   = 167,
        ["Setting Three"] = 184,
        ["Setting Four"]  = 203,
        ["Setting Five"]  = 213,
    };

    // LSB pages printed after each creed choice. Check these against the book.
    private static readonly Dictionary<string, string> CreedPages = new()
    {
        ["Nicene or Apostles' Creed"] = "158-159",
        ["Nicene Creed"]              = "158",
        ["Apostles' Creed"]           = "159",
        ["Athanasian Creed"]          = "319",
    };

    private readonly HymnRepository _repo;

    private readonly BulletinStore _store;
    private readonly HymnCorrections _corrections;
    private readonly DispatcherTimer _saveTimer;

    // True while a Sunday is being loaded into the form, so loading it isn't
    // mistaken for the pastor editing it.
    private bool _loading;

    // The Sunday currently in the form, and whether it has unsaved edits.
    private DateOnly? _currentDate;
    private bool _dirty;

    // Set when the form was filled by copying an earlier Sunday that the
    // pastor hasn't touched yet (nothing is saved for this Sunday until then).
    private DateOnly? _copiedFrom;

    // The "SavedAt" stamp of this Sunday's file as we last loaded or wrote it
    // (null = there was no file). If the file on disk has a different stamp when
    // we go to save, another computer has saved this Sunday in the meantime.
    private string? _diskStamp;

    // True while another computer's version is waiting for the pastor's choice.
    private bool _conflict;
    private SavedBulletin? _theirs;

    // Titles that were loaded from a saved Sunday, per title box. They are not
    // corrections, so they must not overwrite the hymn database.
    private readonly Dictionary<string, (int? Number, string Title)> _snapshot = new();

    public MainWindow(BulletinStore store, HymnCorrections corrections)
    {
        // Permanent per-user folder, so rebuilding or cleaning the project
        // can't erase corrected hymn titles. On Windows this is
        // %LocalAppData%\StMarkBulletin; on Linux, ~/.local/share/StMarkBulletin.
        var folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "StMarkBulletin");
        Directory.CreateDirectory(folder);

        _repo = new HymnRepository(Path.Combine(folder, "hymns.db"));
        _repo.ImportTsv(Path.Combine(AppContext.BaseDirectory, "Data", "hymns.tsv"));

        // The saved bulletins (one file per Sunday). The calendar window creates
        // the store and shares it with this form.
        _store = store;
        _corrections = corrections;

        InitializeComponent();

        // Start on Setting Three (what your current bulletin uses).
        SettingBox.SelectedIndex = 2;

        // Auto-save: a moment after the last edit.
        _saveTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1.5) };
        _saveTimer.Tick += (_, _) => SaveCurrent();
        HookChangeEvents();
    }

    // Any edit to the form marks the current Sunday as needing a save.
    private void HookChangeEvents()
    {
        var boxes = new[]
        {
            MinisterBox, OrganistBox, PreludeBox, ServiceTimeBox, SundayNameBox,
            OpeningHymnBox, OpeningTitle, DayHymnBox, DayTitle,
            ClosingHymnBox, ClosingTitle, PostludeBox, AnnouncementsBox,
        };
        foreach (var box in boxes)
            box.TextChanged += (_, _) => MarkDirty();

        SettingBox.SelectionChanged += (_, _) => MarkDirty();
        CreedBox.SelectionChanged += (_, _) => MarkDirty();
    }

    private void MarkDirty()
    {
        if (_loading || _currentDate == null) return;
        _dirty = true;
        _saveTimer.Stop();
        _saveTimer.Start();
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        FlushSave();
        base.OnClosing(e);
    }

    private static string Pretty(DateOnly d, string format) =>
        d.ToString(format, CultureInfo.InvariantCulture);

    // Asks the lectionary data for the church-year name of a Sunday.
    private string LookUpSundayName(DateOnly date)
    {
        var name = SundayNames.Get(date, out var problem);
        if (problem != null)
            StatusText.Text = $"Couldn't work out the Sunday name ({problem}). You can type it in by hand.";
        return name;
    }

    // Raised after this form saves a Sunday, so the calendar can mark it.
    public event Action? BulletinSaved;

    // Called by the calendar window: puts the chosen Sunday in the form. The
    // Sunday that was open is saved first if it had edits.
    public void ShowSunday(DateOnly date)
    {
        StatusText.Text = "";

        // Pick up hymn title corrections made on other computers.
        var problem = _corrections.Sync(_repo);
        if (problem != null)
            StatusText.Text = $"Couldn't update the shared hymn corrections ({problem}).";

        SwitchTo(date);
        SundayLabel.Text = Pretty(date, "dddd, MMMM d, yyyy");
        Title = "St. Mark Bulletin: " + Pretty(date, "MMM d, yyyy");
    }

    private string? SelectedSetting =>
        (SettingBox.SelectedItem as ComboBoxItem)?.Content?.ToString();

    private void SettingBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        var name = SelectedSetting;
        SettingPageLabel.Text =
            name != null && SettingPages.TryGetValue(name, out var page)
                ? $"LSB page {page}"
                : "";
    }

    // Shared by all three hymn boxes. Each box's Tag names its title label.
    private void HymnBox_TextChanged(object? sender, TextChangedEventArgs e)
    {
        var box = (TextBox)sender!;
        var title = this.FindControl<TextBox>((string)box.Tag!)!;
        var text = box.Text?.Trim() ?? "";

        if (text.Length == 0)
        {
            title.Text = "";
            title.Watermark = "";
        }
        else if (!int.TryParse(text, out var number)){
            title.Text = "";
            title.Watermark = "Enter a number";
        }
        else{
            var hymn = _repo.GetByNumber(number);
            title.Text = hymn?.Title ?? "";
            title.Watermark = hymn == null ? "Not in database; Type a title to add it." : "";
        }
    }

    /**
    If the user clicks out the title box
    */
    private void Title_LostFocus(object? sender, RoutedEventArgs e)
    {
        var titleBox = (TextBox)sender!;
        var numberBox = this.FindControl<TextBox>((string)titleBox.Tag!)!;
        SaveTitle(numberBox, titleBox);
    }

    private void SaveTitle(TextBox numberBox, TextBox titleBox)
    {
        if (!int.TryParse(numberBox.Text?.Trim(), out var number)) return;

        var title = titleBox.Text?.Trim() ?? "";
        if(title.Length == 0) return;
        var existing = _repo.GetByNumber(number);
        if(existing?.Title==title) return;

        // A title that was simply loaded from a saved Sunday isn't a correction:
        // an old bulletin must not overwrite a newer fix in the database. (If
        // the database doesn't know the hymn at all, adding it is still right.)
        if (existing != null
            && _snapshot.TryGetValue(titleBox.Name!, out var loaded)
            && loaded.Number == number && loaded.Title == title) return;

        _repo.Upsert(number, title);

        // Share the correction with the other computers.
        var problem = _corrections.Record(_repo, number, title);
        if (problem != null)
            StatusText.Text = $"The title is saved on this computer, but couldn't be shared yet ({problem}).";
    }

    // Blank is allowed (the hymn is left empty on the bulletin). A non-number
    // or a hymn that isn't in the database stops the PDF with a message.
    private bool TryReadHymn(string? text, string label, out int? number)
    {
        number = null;
        text = text?.Trim() ?? "";
        if (text.Length == 0) return true;

        if (!int.TryParse(text, out var n))
        {
            StatusText.Text = $"{label}: \"{text}\" isn't a number.";
            return false;
        }
        if (_repo.GetByNumber(n) == null)
        {
            StatusText.Text = $"{label}: #{n} isn't in the hymn database.";
            return false;
        }
        number = n;
        return true;
    }

    private void Generate_Click(object? sender, RoutedEventArgs e)
    {
        if (_currentDate is not DateOnly sunday)
        {
            StatusText.Text = "Pick a Sunday in the calendar first.";
            return;
        }

        SaveTitle(OpeningHymnBox, OpeningTitle);
        SaveTitle(DayHymnBox, DayTitle);
        SaveTitle(ClosingHymnBox, ClosingTitle);

        if (!TryReadHymn(OpeningHymnBox.Text, "Opening hymn", out var opening)) return;
        if (!TryReadHymn(DayHymnBox.Text, "Hymn of the Day", out var hymnOfDay)) return;
        if (!TryReadHymn(ClosingHymnBox.Text, "Closing hymn", out var closing)) return;

        // Generating the PDF keeps this Sunday, even if it was only copied.
        SaveCurrent();

        var setting = SelectedSetting ?? "";
        var creed = (CreedBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "";

        var bulletin = new Bulletin
        {
            Date = sunday,
            ServiceTime = ServiceTimeBox.Text?.Trim() ?? "",
            SundayName = SundayNameBox.Text?.Trim() ?? "",
            Minister = MinisterBox.Text?.Trim() ?? "",
            Organist = OrganistBox.Text?.Trim() ?? "",
            SettingName = setting,
            SettingPage = SettingPages.TryGetValue(setting, out var page) ? (int?)page : null,
            Prelude = PreludeBox.Text?.Trim() ?? "",
            OpeningHymn = opening,
            OpeningTitle = OpeningTitle.Text?.Trim() ?? "",
            Creed = creed,
            CreedPages = CreedPages.TryGetValue(creed, out var pages) ? pages : "",
            HymnOfTheDay = hymnOfDay,
            DayTitle = DayTitle.Text?.Trim() ?? "",
            ClosingHymn = closing,
            ClosingTitle = ClosingTitle.Text?.Trim() ?? "",
            Postlude = PostludeBox.Text?.Trim() ?? "",
            Announcements = AnnouncementsBox.Text ?? ""
        };

        try
        {
            var folder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "StMarkBulletins");
            Directory.CreateDirectory(folder);
            var path = Path.Combine(folder, $"bulletin_{DateTime.Now:yyyy-MM-dd_HHmm}.pdf");

            new BulletinDocument(bulletin, _repo).GeneratePdf(path);

            StatusText.Text = $"Saved: {path}";
            OpenFile(path);
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Couldn't create the PDF: {ex.Message}";
        }
    }

    // ───────────────────────── saving and loading Sundays ─────────────────────────

    // Moves the form to another Sunday: keeps the old one if it was edited,
    // then loads the new one.
    private void SwitchTo(DateOnly date)
    {
        if (_currentDate == date) return;
        FlushSave();
        LoadSunday(date);
    }

    private void FlushSave()
    {
        // Another computer's version is still waiting for a choice. Don't lose
        // the pastor's edits: keep them in a side file.
        if (_conflict && _currentDate is DateOnly date)
        {
            try { _store.SaveConflictCopy(date, ReadForm(date)); } catch { }
            _conflict = false;
            _theirs = null;
            return;
        }

        if (_dirty) SaveCurrent();
    }

    private void SaveCurrent()
    {
        _saveTimer.Stop();
        if (_currentDate is not DateOnly date) return;
        if (_conflict) return;      // waiting for the pastor to choose

        try
        {
            // Has another computer saved this Sunday since we loaded or last wrote it?
            var onDisk = _store.Load(date);
            if (onDisk != null && onDisk.SavedAt != _diskStamp)
            {
                _conflict = true;
                _theirs = onDisk;
                UpdateBanner();
                SaveNote.Text = "Not saved yet: choose which version to keep.";
                return;
            }

            var record = ReadForm(date);
            _store.Save(date, record);
            _diskStamp = record.SavedAt;
            _dirty = false;
            _copiedFrom = null;
            UpdateBanner();
            SaveNote.Text = $"Saved {DateTime.Now.ToString("h:mm tt", CultureInfo.InvariantCulture)}";
            BulletinSaved?.Invoke();
        }
        catch (Exception ex)
        {
            SaveNote.Text = $"Couldn't save: {ex.Message}";
        }
    }

    // Fills the form for a Sunday: its own saved bulletin if there is one,
    // otherwise a copy of the nearest earlier saved Sunday, otherwise blank.
    private void LoadSunday(DateOnly date)
    {
        _conflict = false;
        _theirs = null;
        _loading = true;
        try
        {
            var saved = LoadRecord(date);
            if (saved != null)
            {
                ApplyRecord(saved);
                _copiedFrom = null;
                _diskStamp = saved.SavedAt;
                SaveNote.Text = LastSavedText(saved);
            }
            else
            {
                var previous = _store.FindNearestEarlier(date);
                var source = previous is DateOnly p ? LoadRecord(p) : null;
                if (source != null)
                {
                    ApplyRecord(source);
                    _copiedFrom = previous;
                }
                else
                {
                    ResetForm();
                    _copiedFrom = null;
                }

                // These belong to the new Sunday, so they are never copied.
                SundayNameBox.Text = LookUpSundayName(date);
                _diskStamp = null;
                SaveNote.Text = "";
            }
        }
        finally
        {
            _loading = false;
        }

        _currentDate = date;
        _dirty = false;
        UpdateBanner();
    }

    // A file that can't be read is renamed out of the way (never overwritten).
    private SavedBulletin? LoadRecord(DateOnly date)
    {
        try
        {
            return _store.Load(date);
        }
        catch (Exception ex)
        {
            string aside;
            try { aside = _store.SetAside(date); }
            catch { aside = "(it could not be renamed)"; }

            StatusText.Text =
                $"The saved file for {Pretty(date, "MMMM d, yyyy")} couldn't be read ({ex.Message}). " +
                $"It was set aside as {aside}.";
            return null;
        }
    }

    private static string LastSavedText(SavedBulletin r)
    {
        if (DateTimeOffset.TryParse(r.SavedAt, CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind, out var when))
            return "Last saved " + when.ToLocalTime().ToString("MMM d, h:mm tt", CultureInfo.InvariantCulture);
        return "";
    }

    private void UpdateBanner()
    {
        ConflictButtons.IsVisible = _conflict;

        if (_conflict && _theirs != null)
        {
            var who = string.IsNullOrWhiteSpace(_theirs.SavedBy) ? "Another computer" : _theirs.SavedBy;
            BannerText.Text =
                $"{who} saved this Sunday {WhenText(_theirs)}. Your edits are not saved yet. " +
                "Keep your version or use theirs; the other one is kept in a side file either way.";
            BannerBorder.IsVisible = true;
        }
        else if (_copiedFrom is DateOnly from)
        {
            BannerText.Text =
                $"Copied from {Pretty(from, "MMMM d")}. Nothing is saved for this Sunday yet: " +
                "change anything, or generate the PDF, to keep it.";
            BannerBorder.IsVisible = true;
        }
        else
        {
            BannerBorder.IsVisible = false;
        }
    }

    private static string WhenText(SavedBulletin r)
    {
        if (DateTimeOffset.TryParse(r.SavedAt, CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind, out var when))
            return "at " + when.ToLocalTime().ToString("MMM d, h:mm tt", CultureInfo.InvariantCulture);
        return "a moment ago";
    }

    // "Use their version": the pastor's edits go to a side file, and the form
    // reloads what the other computer saved.
    private void UseTheirs_Click(object? sender, RoutedEventArgs e)
    {
        if (_currentDate is not DateOnly date) return;

        var kept = "";
        try { kept = _store.SaveConflictCopy(date, ReadForm(date)); } catch { }

        _conflict = false;
        _theirs = null;
        _dirty = false;
        _currentDate = null;          // forces a fresh load from disk
        LoadSunday(date);
        BulletinSaved?.Invoke();

        if (kept.Length > 0) StatusText.Text = $"Your edits were kept in the file {kept}.";
    }

    // "Keep mine": the other computer's version goes to a side file, and this
    // form's version is saved over the real one.
    private void KeepMine_Click(object? sender, RoutedEventArgs e)
    {
        if (_currentDate is not DateOnly date || _theirs == null) return;

        var kept = "";
        try { kept = _store.SaveConflictCopy(date, _theirs); } catch { }

        _diskStamp = _theirs.SavedAt;  // we have now seen their version
        _conflict = false;
        _theirs = null;
        SaveCurrent();

        if (kept.Length > 0) StatusText.Text = $"Their version was kept in the file {kept}.";
    }

    private SavedBulletin ReadForm(DateOnly date) => new()
    {
        Date = Pretty(date, "yyyy-MM-dd"),
        ServiceTime = ServiceTimeBox.Text?.Trim() ?? "",
        SundayName = SundayNameBox.Text?.Trim() ?? "",
        Minister = MinisterBox.Text?.Trim() ?? "",
        Organist = OrganistBox.Text?.Trim() ?? "",
        SettingName = SelectedSetting ?? "",
        Prelude = PreludeBox.Text?.Trim() ?? "",
        OpeningHymn = OpeningHymnBox.Text?.Trim() ?? "",
        OpeningTitle = OpeningTitle.Text?.Trim() ?? "",
        Creed = (CreedBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "",
        DayHymn = DayHymnBox.Text?.Trim() ?? "",
        DayTitle = DayTitle.Text?.Trim() ?? "",
        ClosingHymn = ClosingHymnBox.Text?.Trim() ?? "",
        ClosingTitle = ClosingTitle.Text?.Trim() ?? "",
        Postlude = PostludeBox.Text?.Trim() ?? "",
        Announcements = AnnouncementsBox.Text ?? "",
        SavedAt = DateTimeOffset.Now.ToString("o", CultureInfo.InvariantCulture),
        SavedBy = Environment.MachineName,
    };

    private void ApplyRecord(SavedBulletin r)
    {
        ServiceTimeBox.Text = r.ServiceTime ?? "";
        SundayNameBox.Text = r.SundayName ?? "";
        MinisterBox.Text = r.Minister ?? "";
        OrganistBox.Text = r.Organist ?? "";
        SelectByContent(SettingBox, r.SettingName, 2);
        PreludeBox.Text = r.Prelude ?? "";
        SetHymn(OpeningHymnBox, OpeningTitle, r.OpeningHymn, r.OpeningTitle);
        SelectByContent(CreedBox, r.Creed, 0);
        SetHymn(DayHymnBox, DayTitle, r.DayHymn, r.DayTitle);
        SetHymn(ClosingHymnBox, ClosingTitle, r.ClosingHymn, r.ClosingTitle);
        PostludeBox.Text = r.Postlude ?? "";
        AnnouncementsBox.Text = r.Announcements ?? "";
    }

    // Empty form with the usual defaults (the pastor's very first launch).
    private void ResetForm()
    {
        ServiceTimeBox.Text = "10 a.m.";
        SundayNameBox.Text = "";
        MinisterBox.Text = "";
        OrganistBox.Text = "";
        SettingBox.SelectedIndex = 2;
        PreludeBox.Text = "";
        SetHymn(OpeningHymnBox, OpeningTitle, "", "");
        CreedBox.SelectedIndex = 0;
        SetHymn(DayHymnBox, DayTitle, "", "");
        SetHymn(ClosingHymnBox, ClosingTitle, "", "");
        PostludeBox.Text = "";
        AnnouncementsBox.Text = "";
    }

    // Sets a hymn number and shows the title that was saved with it. If no
    // title was saved, the database's title is used.
    private void SetHymn(TextBox numberBox, TextBox titleBox, string? number, string? title)
    {
        number = number?.Trim() ?? "";
        title = title?.Trim() ?? "";
        int? parsed = int.TryParse(number, out var n) ? n : null;

        numberBox.Text = number;
        if (title.Length > 0)
            titleBox.Text = title;
        else
            titleBox.Text = parsed is int hymnNumber ? _repo.GetByNumber(hymnNumber)?.Title ?? "" : "";

        _snapshot[titleBox.Name!] = (parsed, title);
    }

    private static void SelectByContent(ComboBox box, string? text, int fallbackIndex)
    {
        for (int i = 0; i < box.Items.Count; i++)
        {
            if ((box.Items[i] as ComboBoxItem)?.Content?.ToString() == text)
            {
                box.SelectedIndex = i;
                return;
            }
        }
        box.SelectedIndex = fallbackIndex;
    }

    // Opens the PDF in the computer's default viewer.
    private static void OpenFile(string path)
    {
        if (OperatingSystem.IsWindows())
        {
            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
            return;
        }

        var psi = new ProcessStartInfo(OperatingSystem.IsMacOS() ? "open" : "xdg-open");
        psi.ArgumentList.Add(path);
        Process.Start(psi);
    }
}