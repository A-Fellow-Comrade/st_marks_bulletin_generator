using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Interactivity;
using QuestPDF.Fluent;
using st_mark_bulletin_generator.Data;
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

    public MainWindow()
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

        InitializeComponent();

        // Start on Setting Three (what your current bulletin uses).
        SettingBox.SelectedIndex = 2;
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
        if(_repo.GetByNumber(number)?.Title==title) return;

        _repo.Upsert(number, title);
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
        SaveTitle(OpeningHymnBox, OpeningTitle);
        SaveTitle(DayHymnBox, DayTitle);
        SaveTitle(ClosingHymnBox, ClosingTitle);

        if (!TryReadHymn(OpeningHymnBox.Text, "Opening hymn", out var opening)) return;
        if (!TryReadHymn(DayHymnBox.Text, "Hymn of the Day", out var hymnOfDay)) return;
        if (!TryReadHymn(ClosingHymnBox.Text, "Closing hymn", out var closing)) return;

        var setting = SelectedSetting ?? "";
        var creed = (CreedBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "";

        var bulletin = new Bulletin
        {
            Minister = MinisterBox.Text?.Trim() ?? "",
            Organist = OrganistBox.Text?.Trim() ?? "",
            SettingName = setting,
            SettingPage = SettingPages.TryGetValue(setting, out var page) ? (int?)page : null,
            Prelude = PreludeBox.Text?.Trim() ?? "",
            OpeningHymn = opening,
            Creed = creed,
            CreedPages = CreedPages.TryGetValue(creed, out var pages) ? pages : "",
            HymnOfTheDay = hymnOfDay,
            ClosingHymn = closing,
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