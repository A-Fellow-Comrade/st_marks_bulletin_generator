using System.Reflection;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.VisualTree;

namespace st_mark_bulletin_generator.Views;

// Shared set-up for the rich-text boxes on the bulletin form (Announcements and
// Acknowledgements), so both look and behave the same: a small toolbar (bold,
// italic, underline, color), the app's light/dark colors, a "Type here..." note
// that shows while the box is empty, and Backspace taking a bullet away.
internal static class RichBox
{
    // Call once, right after InitializeComponent().
    public static void Prepare(AvaloniaRichEditor.Controls.RichEditorView box, Control placeholder, Action onEdited)
    {
        box.Toolbar.ToolbarLevel = AvaloniaRichEditor.Controls.ToolbarLevel.Normal;
        box.Toolbar.PropertyChanged += (_, e) =>
        {
            if (e.Property == ContentControl.ContentProperty)
            {
                ApplyTheme(box);
                ConfigureToolbar(box);
            }
        };

        ApplyTheme(box);
        ConfigureToolbar(box);
        UpdatePlaceholder(box, placeholder);

        box.Editor.TextChanged += (_, _) =>
        {
            UpdatePlaceholder(box, placeholder);
            onEdited();
        };

        // Tunnel = this runs before the editor's own key handling, so it can take over Backspace.
        box.Editor.AddHandler(InputElement.KeyDownEvent,
            (_, e) => RemoveBulletOnBackspace(box.Editor, e), RoutingStrategies.Tunnel);
    }

    // The editor turns "* " (or "- ") at the start of a line into a bullet, but Backspace at the
    // start of a bullet only joins it to the line above, so the first bullet could not be removed.
    // This makes Backspace at the very start of a bullet take the bullet away and keep the text,
    // as Word does. Anything else (a selection, a caret in the middle of the text, Ctrl+Backspace)
    // is left to the editor.
    private static void RemoveBulletOnBackspace(AvaloniaRichEditor.Controls.RichEditor editor, KeyEventArgs e)
    {
        if (e.Handled || e.Key != Key.Back || e.KeyModifiers != KeyModifiers.None) return;
        if (editor.IsReadOnly || HasSelection(editor)) return;
        if (editor.GetCaretFormat().List == AvaloniaRichEditor.Documents.ListKind.None) return;
        if (editor.GetStatus().col != 1) return; // not at the very start of the bullet's text

        editor.RemoveList();
        e.Handled = true;
    }

    // The editor has no public "is anything selected" question, but it keeps one internally. It is
    // read once by name; if a later version of the editor renames it, this safely answers "no".
    private static readonly PropertyInfo? HasSelectionProperty =
        typeof(AvaloniaRichEditor.Controls.RichEditor).GetProperty(
            "HasTextOrCellSelection", BindingFlags.Instance | BindingFlags.NonPublic);

    private static bool HasSelection(AvaloniaRichEditor.Controls.RichEditor editor)
    {
        try { return HasSelectionProperty?.GetValue(editor) is true; }
        catch { return false; }
    }

    // Colors the editor and its toolbar for the current light or dark theme.
    public static void ApplyTheme(AvaloniaRichEditor.Controls.RichEditorView box)
    {
        var resources = ((App)Avalonia.Application.Current!).Resources;
        var foreground = (IBrush)resources["TextBrush"]!;

        box.Editor.DefaultForeground = foreground;
        box.Editor.CaretBrush = foreground;

        if (box.Toolbar.Content is Border toolbarBorder)
            toolbarBorder.Background = (IBrush)resources["CardBrush"]!;

        foreach (var control in box.Toolbar.GetVisualDescendants())
        {
            if (control is Border { Width: 1, Height: 22 } separator)
                separator.Background = (IBrush)resources["BorderBrush"]!;
            else if (control is ComboBox comboBox)
                comboBox.BorderBrush = (IBrush)resources["InputBorderBrush"]!;
        }
    }

    // Keeps only the Bold, Italic, Underline and Text color buttons.
    public static void ConfigureToolbar(AvaloniaRichEditor.Controls.RichEditorView box)
    {
        if (box.Toolbar.Content is not Border { Child: WrapPanel wrap })
            return;

        string bold = AvaloniaRichEditor.RichEditorLocalization.GetString("Bold");
        string italic = AvaloniaRichEditor.RichEditorLocalization.GetString("Italic");
        string underline = AvaloniaRichEditor.RichEditorLocalization.GetString("Underline");
        string textColor = AvaloniaRichEditor.RichEditorLocalization.GetString("TextColor");

        foreach (var item in wrap.Children)
        {
            bool keep = item is Button button
                && ToolTip.GetTip(button)?.ToString() is string tip
                && (tip.StartsWith(bold + " (", StringComparison.Ordinal)
                    || tip.StartsWith(italic + " (", StringComparison.Ordinal)
                    || tip.StartsWith(underline + " (", StringComparison.Ordinal)
                    || string.Equals(tip, textColor, StringComparison.Ordinal));
            item.IsVisible = keep;
        }
    }

    // Shows the "Type here..." note only while the box is empty.
    public static void UpdatePlaceholder(AvaloniaRichEditor.Controls.RichEditorView box, Control placeholder)
    {
        placeholder.IsVisible =
            string.IsNullOrWhiteSpace(box.Editor.GetPlainText())
            && box.Editor.GetImageCount() == 0;
    }
}