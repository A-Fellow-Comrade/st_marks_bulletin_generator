using System.Globalization;
using System.Text.Json;
using st_mark_bulletin_generator.Models;

namespace st_mark_bulletin_generator.Data;

//keeps a JSON file of the data for every sunday, named like YYYY-MM-DD.json
public class BulletinStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented= true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,

    };

    private readonly string _folder;
    public BulletinStore(string folder)
    {
        _folder= folder;
        Directory.CreateDirectory(_folder);

    }

    public string folder => _folder;
    private string PathFor(DateOnly date) => Path.Combine(_folder, date.ToString("yyyy-MMM-dd", CultureInfo.InvariantCulture) + ".json");

    //lists every sunday with a saved file, oldest first
    public List<DateOnly> ListDates()
    {
        var dates = new List<DateOnly>();
        foreach(var file in Directory.EnumerateFiles(_folder, "*.json"))
        {
            if (DateOnly.TryParseExact(Path.GetFileNameWithoutExtension(file), "yyyy-MM-dd",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
                dates.Add(d);
        }
        dates.Sort();
        return dates;
    }

    //the closest saved Sunday before this one, or null if none exists
    public DateOnly? FindNearestEarlier(DateOnly date)
    {
        var earlier = ListDates().Where(d => d < date).ToList();
        return earlier.Count == 0 ? null : earlier.Max();
    }

    //returns null if nothing is saved for that sunday. 
    //Will throw an error if the sunday can't be read
    public SavedBulletin? Load(DateOnly date)
    {
        var path = PathFor(date);
        if( !File.Exists(path)) return null;
        var record = JsonSerializer.Deserialize<SavedBulletin>(File.ReadAllText(path));
        return record ?? throw new InvalidDataException("The file is empty");
    }

    //writes the bulletin to a temp file and then swaps it in
    public void Save(DateOnly date, SavedBulletin records)
    {
        var path = PathFor(date);
        var temp = path + ".tmp";
        File.WriteAllText(temp, JsonSerializer.Serialize(records, JsonOptions));
        File.Move(temp, path, overwrite: true);
    }

    //keeps a version of a sunday that had a conflict so nothing is lost. Calendar will ignore it
    //returns the file name
    public string SaveConflictCopy(DateOnly date, SavedBulletin record)
    {
        var path = PathFor(date) + ".conflict-" + DateTime.Now.ToString("yyyyMMff-HHmmss-fff", CultureInfo.InvariantCulture);
        File.WriteAllText(path, JsonSerializer.Serialize(record, JsonOptions));
        return Path.GetFileName(path);
    }

    //For a file that can't be read, rename it so it is never overwritten
    public string SetAside(DateOnly date)
    {
        var path = PathFor(date);
        var aside = path + ".unreadable-" + DateTime.Now.ToString("yyyyMMff-HHmmss", CultureInfo.InvariantCulture);
        File.Move(path, aside);
        return Path.GetFileName(aside);
    }
}