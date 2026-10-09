using Avalonia.Controls;
using Avalonia.Interactivity;
using st_mark_bulletin_generator.Data;

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
        // Set here, after the window is built, so the page label updates safely.
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
        var title = this.FindControl<TextBlock>((string)box.Tag!)!;
        var text = box.Text?.Trim() ?? "";

        if (text.Length == 0)
            title.Text = "";
        else if (!int.TryParse(text, out var number))
            title.Text = "Enter a number";
        else
            title.Text = _repo.GetByNumber(number)?.Title ?? "Not in database";
    }

    // Placeholder: proves the form values can be read. We'll connect
    // this to the PDF in the next step.
    private void Generate_Click(object? sender, RoutedEventArgs e)
    {
        var creed = (CreedBox.SelectedItem as ComboBoxItem)?.Content?.ToString();

        StatusText.Text =
            $"Minister: {MinisterBox.Text}\n" +
            $"Setting: {SelectedSetting} ({SettingPageLabel.Text})\n" +
            $"Opening hymn: {OpeningHymnBox.Text} {OpeningTitle.Text}\n" +
            $"Creed: {creed}\n" +
            $"Hymn of the Day: {DayHymnBox.Text} {DayTitle.Text}\n" +
            $"Closing hymn: {ClosingHymnBox.Text} {ClosingTitle.Text}\n" +
            $"Announcements:\n{AnnouncementsBox.Text}\n" +
            "(PDF not connected yet)";
    }
}