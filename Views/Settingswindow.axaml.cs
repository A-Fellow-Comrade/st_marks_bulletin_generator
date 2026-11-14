using Avalonia.Controls;
using Avalonia.Interactivity;
using st_mark_bulletin_generator.Data;
using st_mark_bulletin_generator.Models;

namespace st_mark_bulletin_generator.Views;

// Lets the church edit the wording that is printed on every bulletin (see
// ChurchSettings). The settings are kept in the shared folder, so every computer
// prints the same. The list of boxes comes from ChurchSettingsFields.
public partial class SettingsWindow : Window
{
    // Set to true to show every setting but let nobody change it (Save and
    // Restore are then hidden). To lock single settings, see
    // ChurchSettingsFields.ReadOnlyKeys.
    private const bool AllReadOnly = false;

    private readonly ChurchSettingsStore _store;
    private readonly Dictionary<string, TextBox> _boxes = new();

    // Only here so Avalonia's XAML loader and previewer can find the window.
    public SettingsWindow() => throw new InvalidOperationException(
        "SettingsWindow needs the settings store: use SettingsWindow(ChurchSettingsStore).");

    public SettingsWindow(ChurchSettingsStore store)
    {
        _store = store;
        InitializeComponent();

        BuildForm();

        // A file that can't be read is set aside by Load; the form then shows the
        // standard wording, and the pastor is told why.
        var current = _store.Load(out var problem);
        foreach (var field in ChurchSettingsFields.All)
            _boxes[field.Key].Text = field.Get(current);

        if (problem != null)
        {
            ProblemText.Text = problem;
            ProblemBorder.IsVisible = true;
        }

        SaveButton.IsVisible = !AllReadOnly;
        RestoreButton.IsVisible = !AllReadOnly;
        CancelButton.Content = AllReadOnly ? "Close" : "Cancel";
    }

    // One card per section, one labelled box per setting.
    private void BuildForm()
    {
        string? section = null;
        StackPanel? card = null;

        foreach (var field in ChurchSettingsFields.All)
        {
            if (field.Section != section)
            {
                section = field.Section;
                card = AddCard(section);
            }

            var label = new TextBlock { Text = field.Label };
            label.Classes.Add("field");
            card!.Children.Add(label);

            if (!string.IsNullOrEmpty(field.Hint))
            {
                var hint = new TextBlock { Text = field.Hint };
                hint.Classes.Add("hint");
                card.Children.Add(hint);
            }

            bool multiline = field.Rows > 1;
            bool locked = AllReadOnly || ChurchSettingsFields.IsReadOnly(field);
            var box = new TextBox
            {
                AcceptsReturn = multiline,
                TextWrapping = multiline ? Avalonia.Media.TextWrapping.Wrap : Avalonia.Media.TextWrapping.NoWrap,
                MinHeight = multiline ? field.Rows * 22 + 14 : 0,
                IsReadOnly = locked,
                Opacity = locked ? 0.7 : 1.0,
            };
            card.Children.Add(box);
            _boxes[field.Key] = box;
        }
    }

    private StackPanel AddCard(string title)
    {
        var heading = new TextBlock { Text = title };
        heading.Classes.Add("section-title");

        var rule = new Border();
        rule.Classes.Add("rule");

        var stack = new StackPanel();
        stack.Children.Add(heading);
        stack.Children.Add(rule);

        var border = new Border { Child = stack };
        border.Classes.Add("card");
        FieldsPanel.Children.Add(border);
        return stack;
    }

    // The message beside the buttons: a note, or a problem (shown in red).
    private void ShowMessage(string text, bool isProblem)
    {
        StatusText.Text = text;
        if (isProblem) StatusText.Classes.Add("error");
        else StatusText.Classes.Remove("error");
    }

    private void Save_Click(object? sender, RoutedEventArgs e)
    {
        ShowMessage("", isProblem: false);

        // Start from the standard settings so anything this window doesn't show
        // (such as the file's version number) keeps a sensible value.
        var result = new ChurchSettings();
        foreach (var field in ChurchSettingsFields.All)
        {
            var box = _boxes[field.Key];
            var message = field.Set(result, (box.Text ?? "").Trim());
            if (message != null)
            {
                ShowMessage(message, isProblem: true);
                box.Focus();
                return;
            }
        }

        try
        {
            _store.Save(result);
            Close(true);
        }
        catch (Exception ex)
        {
            ShowMessage($"Couldn't save the settings: {ex.Message}", isProblem: true);
        }
    }

    // Puts the standard wording in the boxes. Nothing is kept until Save is pressed.
    private void Restore_Click(object? sender, RoutedEventArgs e)
    {
        var standard = new ChurchSettings();
        foreach (var field in ChurchSettingsFields.All)
        {
            if (!ChurchSettingsFields.IsReadOnly(field))
                _boxes[field.Key].Text = field.Get(standard);
        }
        ShowMessage("Standard wording is back in the boxes. Press Save to keep it.", isProblem: false);
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e) => Close(false);
}