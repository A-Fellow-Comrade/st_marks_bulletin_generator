using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;

namespace st_mark_bulletin_generator.Views;

/// <summary>
/// Modal dialog that shows all images in the imgs folder as clickable thumbnails.
/// The user can also upload new images to the library or delete existing ones.
/// Returns the chosen file path, or null for "random".
/// </summary>
public partial class CoverImagePickerWindow : Window
{
    // null  → random (default); non-null → chosen absolute path
    public string? SelectedImagePath { get; private set; }

    private readonly string _imgsFolder;

    // Currently highlighted border (RandomBorder or a thumb border)
    private Border? _activeBorder;

    // thumb border → absolute file path
    private readonly Dictionary<Border, string> _borderPaths = new();

    public CoverImagePickerWindow(string imgsFolder, string? currentPath)
    {
        _imgsFolder = imgsFolder;
        InitializeComponent();
        RebuildThumbnails(currentPath);
    }

    // ── thumbnail building ─────────────────────────────────────────────────────

    /// <summary>
    /// Clears and rebuilds the thumbnail strip, then restores <paramref name="selectPath"/>.
    /// </summary>
    private void RebuildThumbnails(string? selectPath)
    {
        ThumbPanel.Children.Clear();
        _borderPaths.Clear();
        _activeBorder = null;

        if (Directory.Exists(_imgsFolder))
        {
            var files = Directory.GetFiles(_imgsFolder, "*.jpg")
                                 .Concat(Directory.GetFiles(_imgsFolder, "*.jpeg"))
                                 .Concat(Directory.GetFiles(_imgsFolder, "*.png"))
                                 .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
                                 .ToArray();

            foreach (var file in files)
                ThumbPanel.Children.Add(MakeThumbCard(file));
        }

        // Restore selection
        if (selectPath != null)
        {
            var match = _borderPaths.FirstOrDefault(kv => kv.Value == selectPath);
            if (match.Key != null)
                Select(match.Key);
            else
                SelectRandom();     // path gone – fall back to random
        }
        else
        {
            SelectRandom();
        }
    }

    /// <summary>Builds one thumbnail card: image + ✕ delete button overlay.</summary>
    private Border MakeThumbCard(string file)
    {
        // The outer border is the click target for selection.
        var border = new Border
        {
            Classes = { "thumb" },
            Width   = 120,
            Height  = 108,
        };

        // Inner Grid lets us layer the image and the delete button.
        var grid = new Grid();

        // Image (or fallback text)
        try
        {
            var bmp = new Bitmap(file);
            grid.Children.Add(new Image
            {
                Source              = bmp,
                Width               = 110,
                Height              = 96,
                Stretch             = Stretch.UniformToFill,
                VerticalAlignment   = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
            });
        }
        catch
        {
            grid.Children.Add(new TextBlock
            {
                Text                = Path.GetFileName(file),
                TextWrapping        = TextWrapping.Wrap,
                VerticalAlignment   = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin              = new Avalonia.Thickness(4),
            });
        }

        // ✕ delete button – sits in the top-right corner
        var delBtn = new Button
        {
            Classes = { "del" },
            Content = "✕",
            Margin  = new Avalonia.Thickness(0, 2, 2, 0),
        };
        ToolTip.SetTip(delBtn, $"Delete {Path.GetFileName(file)} from library");
        delBtn.Click += (_, _) => DeleteImage(border, file);
        grid.Children.Add(delBtn);

        border.Child = grid;

        ToolTip.SetTip(border, Path.GetFileName(file));
        _borderPaths[border] = file;
        border.PointerPressed += ThumbBorder_PointerPressed;

        return border;
    }

    // ── selection helpers ──────────────────────────────────────────────────────

    private void SelectRandom()
    {
        if (_activeBorder != null)
            _activeBorder.Classes.Remove("selected");

        _activeBorder = RandomBorder;
        RandomBorder.Classes.Add("selected");
        SelectedImagePath = null;
    }

    private void Select(Border border)
    {
        if (_activeBorder != null)
            _activeBorder.Classes.Remove("selected");

        _activeBorder = border;
        border.Classes.Add("selected");
        SelectedImagePath = _borderPaths.TryGetValue(border, out var path) ? path : null;
    }

    // ── delete ─────────────────────────────────────────────────────────────────

    private async void DeleteImage(Border thumb, string file)
    {
        var dlg = new Window
        {
            Title                   = "Delete image",
            Width                   = 340,
            SizeToContent           = SizeToContent.Height,
            WindowStartupLocation   = WindowStartupLocation.CenterOwner,
            CanResize               = false,
        };

        var yesBtn = new Button { Content = "Delete", Width = 80 };
        var noBtn  = new Button { Content = "Cancel", Width = 80 };
        bool confirmed = false;
        yesBtn.Click += (_, _) => { confirmed = true;  dlg.Close(); };
        noBtn.Click  += (_, _) => { confirmed = false; dlg.Close(); };

        dlg.Content = new StackPanel
        {
            Margin  = new Avalonia.Thickness(16),
            Spacing = 12,
            Children =
            {
                new TextBlock
                {
                    Text        = $"Delete \"{Path.GetFileName(file)}\" from the library?\nThis cannot be undone.",
                    TextWrapping = TextWrapping.Wrap,
                },
                new StackPanel
                {
                    Orientation         = Orientation.Horizontal,
                    Spacing             = 8,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Children            = { yesBtn, noBtn },
                },
            },
        };

        await dlg.ShowDialog(this);
        if (!confirmed) return;

        // If the deleted image was selected, switch to random.
        if (SelectedImagePath == file)
        {
            SelectedImagePath = null;
            SelectRandom();
        }

        try
        {
            File.Delete(file);
        }
        catch (Exception ex)
        {
            await ShowError($"Could not delete the file: {ex.Message}");
            return;
        }

        // Cleanly remove the thumbnail without rebuilding everything.
        _borderPaths.Remove(thumb);
        ThumbPanel.Children.Remove(thumb);
    }

    // ── upload ─────────────────────────────────────────────────────────────────

    private async void Upload_Click(object? sender, RoutedEventArgs e)
    {
        var topLevel = GetTopLevel(this);
        if (topLevel == null) return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title         = "Choose an image to add to the library",
            AllowMultiple = true,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("Images")
                {
                    Patterns = new[] { "*.jpg", "*.jpeg", "*.png" },
                    MimeTypes = new[] { "image/jpeg", "image/png" },
                },
            },
        });

        if (files.Count == 0) return;

        Directory.CreateDirectory(_imgsFolder);

        string? lastCopied = null;
        var errors = new List<string>();

        foreach (var file in files)
        {
            var dest = UniquePath(_imgsFolder, file.Name);
            try
            {
                await using var src  = await file.OpenReadAsync();
                await using var dst  = File.Create(dest);
                await src.CopyToAsync(dst);
                lastCopied = dest;
            }
            catch (Exception ex)
            {
                errors.Add($"{file.Name}: {ex.Message}");
            }
        }

        if (errors.Count > 0)
            await ShowError("Some files couldn't be copied:\n" + string.Join("\n", errors));

        // Rebuild thumbnails and select the last uploaded image.
        RebuildThumbnails(lastCopied ?? SelectedImagePath);
    }

    /// <summary>
    /// Returns a path in <paramref name="folder"/> for <paramref name="fileName"/> that
    /// doesn't collide with an existing file (appends _2, _3, … as needed).
    /// </summary>
    private static string UniquePath(string folder, string fileName)
    {
        var dest = Path.Combine(folder, fileName);
        if (!File.Exists(dest)) return dest;

        var name = Path.GetFileNameWithoutExtension(fileName);
        var ext  = Path.GetExtension(fileName);
        for (int i = 2; ; i++)
        {
            dest = Path.Combine(folder, $"{name}_{i}{ext}");
            if (!File.Exists(dest)) return dest;
        }
    }

    // ── small helpers ──────────────────────────────────────────────────────────

    private async Task ShowError(string message)
    {
        var dlg = new Window
        {
            Title                 = "Error",
            Width                 = 360,
            SizeToContent         = SizeToContent.Height,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize             = false,
        };
        var ok = new Button { Content = "OK", Width = 80 };
        ok.Click += (_, _) => dlg.Close();
        dlg.Content = new StackPanel
        {
            Margin   = new Avalonia.Thickness(16),
            Spacing  = 12,
            Children =
            {
                new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap },
                new StackPanel
                {
                    Orientation         = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Children            = { ok },
                },
            },
        };
        await dlg.ShowDialog(this);
    }

    // ── event handlers ─────────────────────────────────────────────────────────

    private void RandomBorder_PointerPressed(object? sender, PointerPressedEventArgs e)
        => SelectRandom();

    private void ThumbBorder_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Border b) Select(b);
    }

    private void Ok_Click(object? sender, RoutedEventArgs e)     => Close(true);
    private void Cancel_Click(object? sender, RoutedEventArgs e) => Close(false);
}

