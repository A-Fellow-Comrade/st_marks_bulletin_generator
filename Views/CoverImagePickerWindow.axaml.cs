using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;

namespace st_mark_bulletin_generator.Views;

/// <summary>
/// A modal dialog that shows all .jpg files in the imgs folder as clickable
/// thumbnails. Returns the chosen file path, or null if the user wants random.
/// </summary>
public partial class CoverImagePickerWindow : Window
{
    // null  → random (default)
    // non-null → the chosen absolute path
    public string? SelectedImagePath { get; private set; }

    // The border that is currently highlighted (either RandomBorder or a thumb border)
    private Border? _activeBorder;

    // Maps each thumb border → its file path
    private readonly Dictionary<Border, string> _borderPaths = new();

    public CoverImagePickerWindow(string imgsFolder, string? currentPath)
    {
        InitializeComponent();
        BuildThumbnails(imgsFolder);

        // Restore the previous selection, if any.
        if (currentPath != null)
        {
            var match = _borderPaths.FirstOrDefault(kv => kv.Value == currentPath);
            if (match.Key != null)
                Select(match.Key);
            else
                Select(RandomBorder);       // path no longer exists – fall back to random
        }
        else
        {
            Select(RandomBorder);
        }
    }

    private void BuildThumbnails(string folder)
    {
        if (!Directory.Exists(folder)) return;

        var files = Directory.GetFiles(folder, "*.jpg")
                             .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
                             .ToArray();

        foreach (var file in files)
        {
            var border = new Border
            {
                Classes = { "thumb" },
                Width   = 120,
                Height  = 100,
            };

            try
            {
                var bmp   = new Bitmap(file);
                var image = new Image
                {
                    Source  = bmp,
                    Width   = 108,
                    Height  = 88,
                    Stretch = Avalonia.Media.Stretch.UniformToFill,
                };
                border.Child = image;
            }
            catch
            {
                // If the image can't be loaded, just show the filename.
                border.Child = new TextBlock
                {
                    Text                = Path.GetFileName(file),
                    TextWrapping        = Avalonia.Media.TextWrapping.Wrap,
                    VerticalAlignment   = Avalonia.Layout.VerticalAlignment.Center,
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                };
            }

            var tooltip = new ToolTip { Content = Path.GetFileName(file) };
            ToolTip.SetTip(border, tooltip);

            _borderPaths[border] = file;
            border.PointerPressed += ThumbBorder_PointerPressed;
            ThumbPanel.Children.Add(border);
        }
    }

    // ── selection helpers ──────────────────────────────────────────────────────

    private void Select(Border border)
    {
        // Remove highlight from previous selection.
        if (_activeBorder != null)
            _activeBorder.Classes.Remove("selected");

        _activeBorder = border;
        border.Classes.Add("selected");

        SelectedImagePath = _borderPaths.TryGetValue(border, out var path) ? path : null;
    }

    // ── event handlers ─────────────────────────────────────────────────────────

    private void RandomBorder_PointerPressed(object? sender, PointerPressedEventArgs e)
        => Select(RandomBorder);

    private void ThumbBorder_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Border b) Select(b);
    }

    private void Ok_Click(object? sender, RoutedEventArgs e)
        => Close(true);

    private void Cancel_Click(object? sender, RoutedEventArgs e)
        => Close(false);
}
