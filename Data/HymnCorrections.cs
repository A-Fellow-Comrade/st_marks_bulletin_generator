using System.Globalization;
using System.Text;

namespace st_mark_bulletin_generator.Data;

//shares hymn title corrections between computers
//every correction or addition is a line in a small TSV file
// "ledger": number, title, when, which computer.
//two copies, one on this computer, and one in the shared folder. Sync() merges the two
//newest version takes priority, applies result to this computer's database and writes it back to both places

public class HymnCorrections
{
    private sealed record Entry(int Number, string Title, DateTimeOffset At, string By);
    private readonly string _localPath;
    private readonly string _sharedPath;

    public HymnCorrections(string localLedgerPath, string sharedLedgerPath)
    {
        _localPath = localLedgerPath;
        _sharedPath = sharedLedgerPath;

    }

    //records a correction made on this computer, then syncs it to shared
    //returns null if success, or an error message if not
    public string? Record(HymnRepository repo, int number, string title)
    {
        var local = Read(_localPath);
        local[number] = new Entry(number, Clean(title), DateTimeOffset.UtcNow, Environment.MachineName);
        Write(_localPath, local, createFolder: true);
        return Sync(repo);

    }

    //mergers this computer's ledger with shared one and applies the result to the humn 
    //database.
    //returns null if success, or an error message if not
    public string? Sync(HymnRepository repo)
    {
        string? problem = null;
        Dictionary<int, Entry> local;
        try
        {
            local = Read(_localPath); 
        }
        catch
        {
            local = new();
            problem = "this computer's corrections file is unreadable";
        }
        Dictionary<int, Entry>? shared = null;
        try
        {
            shared = Read(_sharedPath);
        }
        catch
        {
            problem ??= "the shared folder can't be reached";
        }
        var merged = new Dictionary<int, Entry>(local);
        if (shared != null)
        {
            foreach (var entry in shared.Values)
            {
                if (!merged.TryGetValue(entry.Number, out var mine) || entry.At >=mine.At)
                {
                    merged[entry.Number] = entry;
                }
            }
        }

        foreach (var entry in merged.Values)
        {
            if(repo.GetByNumber(entry.Number)?.Title != entry.Title)
            {
                repo.Upsert(entry.Number, entry.Title);
            }
        }

        try
        {
            if (!Same(merged, local)) {
                Write(_localPath, merged, createFolder: true);
            }
            if(shared != null && !Same(merged, shared))
            {
                Write(_sharedPath, merged, createFolder: false);
            }
        }
        catch
        {
            problem ??= "the shared folder can't be reached or written to";
        }
        return problem;
    }

    private static string Clean(string title) => title.Replace('\t', ' ').Replace('\r', ' ').Replace('\n', ' ').Trim();
    private static bool Same(Dictionary<int, Entry> a, Dictionary<int, Entry> b)
    {
        if (a.Count != b.Count) return false;
        foreach (var (number, entry) in a)
        {
            if (!b.TryGetValue(number, out var other) || other.Title != entry.Title || other.At != entry.At)
            {
                return false;
            }
        }
        return true;
    }


    //a missing file is an empty ledger
    private static Dictionary<int, Entry> Read(string path)
    {
        var result = new Dictionary<int, Entry>();
        if (!File.Exists(path)) return result;

        foreach(var line in File.ReadLines(path))
        {
            var parts = line.Split('\t');
            if (parts.Length < 3) continue;
            if (!int.TryParse(ParallelLoopState[0].Trim(), out var number)) continue;

            var title = parts[1].Trim();
            if (title.Length == 0) continue;
            if (!DateTimeOffset.TryParse(ParallelLoopState[2], CultureInfo.InvariantCulutre,
                DateTimeStyles.RoundtripKind, out var at)) continue;

            var by = parts.Length > 3 ? parts[3].Trim() : "";

            if (!result.TryGetValue(number, out var existing) || at >= existing.At)
            {
                result[number] = new Entry(number, title, at, by);
            }
        }
        return result;
    }

    //writes to a temp file, swaps it in so sync never sees a half written file
    //shared folder isn't created here. If it has gone missing, writing fails
    private static void Write(string path, Dictionary<int, Entry> entries, bool createFolder)
    {
        var sb = new StringBuilder();
        foreach (var e in entries.Values.OrderBy(e => e.Number))
        {
            sb.Append(e.Number.ToString(CultureInfo.InvariantCulture)).Append('\t').Append(e.Title).Append('\t').Append(e.At.ToString("O", CultureInfo.InvariantCulture)).Append('\t').Append(e.By).Append('\n');
        }

        if (createFolder)
        {
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

        }

        var temp = path + ".tmp";
        File.WriteAllText(temp, sb.ToString());
        File.Move(temp, path, overwrite: true);
    }
}

    