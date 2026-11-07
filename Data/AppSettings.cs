using System.Text.Json;

namespace st_mark_bulletin_generator.Data;

//Settings that belong to the user's computer, not shared.
//for now, folder holds the bulletins
public class AppSettings
{
    private static readonly JsonSerializerOptions JsonOptions = new(){ WriteIndented = true };
    //per-computer folder can also hold hymns.db
    public static string DataFolder => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "StMarkBulletin");
    public static string FilePath => Path.Combine(DataFolder, "settings.json");

    //The shared folder, like inside onedrive. Bulletins live in its bulletins subfolder, and hymn correction
    public string? SharedFolder { get; set; }
    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(FilePath))
                return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(FilePath)) ?? new AppSettings();
        }
        catch
        {
            //unreadable settings file, ask for folder again
        }
        return new AppSettings();
    }

    public void Save()
    {
        Directory.CreateDirectory(DataFolder);
        File.WriteAllText(FilePath, JsonSerializer.Serialize(this, JsonOptions));
    }
}