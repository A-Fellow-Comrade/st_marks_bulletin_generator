using System.Globalization;
using System.Reflection;
using System.Text.Json;
using st_mark_bulletin_generator.Models;

namespace st_mark_bulletin_generator.Data;

// Keeps the church's own wording in one file, church_settings.json, in the shared
// folder, so every computer prints the same bulletin. It can be edited by hand.
public class ChurchSettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    private readonly string _path;

    public ChurchSettingsStore(string path) => _path = path;

    public string FilePath => _path;

    // Never throws. No file (or a missing setting in it) gives the standard
    // wording. A file that can't be understood is renamed out of the way, never
    // overwritten, and the standard wording is used; `problem` then says so.
    public ChurchSettings Load(out string? problem)
    {
        problem = null;
        try
        {
            if (!File.Exists(_path)) return new ChurchSettings();

            var loaded = JsonSerializer.Deserialize<ChurchSettings>(File.ReadAllText(_path), JsonOptions)
                ?? throw new InvalidDataException("The file is empty");
            FillNulls(loaded);
            return loaded;
        }
        catch (Exception ex) when (ex is JsonException or InvalidDataException)
        {
            string aside;
            try { aside = SetAside(); }
            catch { aside = "(it could not be renamed)"; }

            problem = $"The church settings file couldn't be read ({ex.Message}). " +
                      $"It was set aside as {aside}, and the standard wording is used.";
            return new ChurchSettings();
        }
        catch (Exception ex)
        {
            // Probably the folder or drive isn't reachable right now: leave the file alone.
            problem = $"The church settings couldn't be opened ({ex.Message}). The standard wording is used this time.";
            return new ChurchSettings();
        }
    }

    // Writes to a temp file and then swaps it in, so a half-written file is never left.
    public void Save(ChurchSettings settings)
    {
        var folder = Path.GetDirectoryName(_path);
        if (!string.IsNullOrEmpty(folder)) Directory.CreateDirectory(folder);

        var temp = _path + ".tmp";
        File.WriteAllText(temp, JsonSerializer.Serialize(settings, JsonOptions));
        File.Move(temp, _path, overwrite: true);
    }

    private string SetAside()
    {
        var aside = _path + ".unreadable-" + DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
        File.Move(_path, aside);
        return Path.GetFileName(aside);
    }

    // A setting written as null in a hand-edited file means "not set": standard wording.
    // (A blank "" is different: that prints nothing.)
    private static void FillNulls(ChurchSettings loaded)
    {
        var standard = new ChurchSettings();
        foreach (var p in typeof(ChurchSettings).GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (p.PropertyType == typeof(string) && p.CanWrite && p.GetValue(loaded) == null)
                p.SetValue(loaded, p.GetValue(standard));
        }
    }
}