using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LiturgyTools;

/// <summary>Which lectionary series to use.</summary>
public enum Lectionary
{
    ThreeYear,
    OneYear,
}

/// <summary>Scripture readings for a slot: {ot, ps, ep, go}.</summary>
public sealed class Readings
{
    [JsonPropertyName("ot")] public string? Ot { get; init; }
    [JsonPropertyName("ps")] public string? Ps { get; init; }
    [JsonPropertyName("ep")] public string? Ep { get; init; }
    [JsonPropertyName("go")] public string? Go { get; init; }

    /// <summary>Shape expected by <see cref="Utils.ParseReadings"/>.</summary>
    public Dictionary<string, string?> ToDictionary() => new()
    {
        ["ot"] = Ot,
        ["ps"] = Ps,
        ["ep"] = Ep,
        ["go"] = Go,
    };
}

/// <summary>Represents an Introit proper (incipit name, scripture reference, and text).</summary>
[JsonConverter(typeof(IntroitConverter))]
public sealed class Introit
{
    [JsonPropertyName("name")] public string? Name { get; init; }
    [JsonPropertyName("ref")] public string? Ref { get; init; }
    [JsonPropertyName("text")] public string? Text { get; init; }

    public override string ToString() => Text ?? Name ?? "";
}

/// <summary>
/// Custom converter supporting Introit as either a JSON string, a JSON object, or null.
/// </summary>
public sealed class IntroitConverter : JsonConverter<Introit>
{
    public override Introit? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;

        if (reader.TokenType == JsonTokenType.String)
        {
            return new Introit { Text = reader.GetString() };
        }

        if (reader.TokenType == JsonTokenType.StartObject)
        {
            string? name = null;
            string? reference = null;
            string? text = null;

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                    break;

                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    string propName = reader.GetString()!;
                    reader.Read();
                    if (string.Equals(propName, "name", StringComparison.OrdinalIgnoreCase))
                        name = reader.GetString();
                    else if (string.Equals(propName, "ref", StringComparison.OrdinalIgnoreCase))
                        reference = reader.GetString();
                    else if (string.Equals(propName, "text", StringComparison.OrdinalIgnoreCase))
                        text = reader.GetString();
                    else
                        reader.Skip();
                }
            }

            return new Introit { Name = name, Ref = reference, Text = text };
        }

        throw new JsonException($"Unexpected token {reader.TokenType} when parsing Introit.");
    }

    public override void Write(Utf8JsonWriter writer, Introit value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        if (value.Name is not null) writer.WriteString("name", value.Name);
        if (value.Ref is not null) writer.WriteString("ref", value.Ref);
        if (value.Text is not null) writer.WriteString("text", value.Text);
        writer.WriteEndObject();
    }
}

/// <summary>
/// One entry from the sanctoral / three-year / one-year slot tables.
/// Property names map 1:1 to the keys used in the Python dicts, so the JSON
/// files can keep the same shape.
/// </summary>
public class SlotInfo
{
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("date_str")] public string? DateStr { get; set; }
    [JsonPropertyName("season")] public string Season { get; set; } = "";
    [JsonPropertyName("color")] public string Color { get; set; } = "";
    [JsonPropertyName("feast")] public bool Feast { get; set; }
    [JsonPropertyName("minor")] public bool Minor { get; set; }
    [JsonPropertyName("proper")] public int? Proper { get; set; }
    [JsonPropertyName("ordinal")] public int? Ordinal { get; set; }

    [JsonPropertyName("readings")] public Readings? Readings { get; set; }

    [JsonPropertyName("collect")] public string? Collect { get; set; }
    [JsonPropertyName("introit")] public Introit? Introit { get; set; }
    [JsonPropertyName("introit_text")] public string? IntroitText { get; set; }
    [JsonPropertyName("gradual")] public string? Gradual { get; set; }
    [JsonPropertyName("source")] public string? Source { get; set; }

    // Three-year lectionary only: readings per series, with "all" as fallback.
    [JsonPropertyName("A")] public Readings? A { get; set; }
    [JsonPropertyName("B")] public Readings? B { get; set; }
    [JsonPropertyName("C")] public Readings? C { get; set; }
    [JsonPropertyName("all")] public Readings? All { get; set; }

    /// <summary>Readings for series "A", "B" or "C", falling back to "all" then Readings.</summary>
    public Readings? ForSeries(string series)
    {
        var bySeries = series switch
        {
            "A" => A,
            "B" => B,
            "C" => C,
            _ => null,
        };
        return bySeries ?? All ?? Readings;
    }

    public SlotInfo Clone() => (SlotInfo)MemberwiseClone();
}

/// <summary>One-year lectionary propers (collect, introit, gradual, source).</summary>
public sealed class Propers
{
    [JsonPropertyName("collect")] public string? Collect { get; init; }
    [JsonPropertyName("introit")] public Introit? Introit { get; init; }
    [JsonPropertyName("gradual")] public string? Gradual { get; init; }
    [JsonPropertyName("source")] public string? Source { get; init; }
}

/// <summary>
/// Daily lectionary data. "DAILY_FIXED" is keyed by "MM-dd"; "DAILY_MOVABLE" is indexed by
/// days since Ash Wednesday.
/// </summary>
public sealed class DailyLectionary
{
    [JsonPropertyName("DAILY_FIXED")]
    public Dictionary<string, Dictionary<string, string?>> DailyFixed { get; init; } = new();

    [JsonPropertyName("DAILY_MOVABLE")]
    public List<Dictionary<string, string?>> DailyMovable { get; init; } = new();

    [JsonPropertyName("fixed")]
    public Dictionary<string, Dictionary<string, string?>>? FixedAlias { get; init; }

    [JsonPropertyName("movable")]
    public List<Dictionary<string, string?>>? MovableAlias { get; init; }

    [JsonIgnore]
    public Dictionary<string, Dictionary<string, string?>> Fixed =>
        DailyFixed.Count > 0 ? DailyFixed : (FixedAlias ?? DailyFixed);

    [JsonIgnore]
    public List<Dictionary<string, string?>> Movable =>
        DailyMovable.Count > 0 ? DailyMovable : (MovableAlias ?? DailyMovable);
}

/// <summary>A sanctoral observance found by calendar date.</summary>
public sealed record SanctoralFeast(string Slot, SlotInfo Info);

/// <summary>All lectionary data tables plus the slot-info dispatcher.</summary>
public sealed class LiturgicalData
{
    private static readonly string[] EpiphanyOrdinals =
    {
        "", "First", "Second", "Third", "Fourth",
        "Fifth", "Sixth", "Seventh", "Eighth",
    };

    // Number of entries in the ordinal list used for Sundays after Trinity
    // (index 0 is the empty string, so 27 Sundays are named).
    private const int TrinityOrdinalCount = 28;

    private static readonly Dictionary<string, int> MonthAbbr = new()
    {
        ["Jan"] = 1, ["Feb"] = 2, ["Mar"] = 3, ["Apr"] = 4, ["May"] = 5, ["Jun"] = 6,
        ["Jul"] = 7, ["Aug"] = 8, ["Sep"] = 9, ["Oct"] = 10, ["Nov"] = 11, ["Dec"] = 12,
    };

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    private readonly Lazy<Dictionary<(int Month, int Day), string>> _sanctoralDateMap;

    public IReadOnlyDictionary<string, SlotInfo> Sanctoral { get; }
    public IReadOnlyDictionary<string, SlotInfo> ThreeYear { get; }
    public IReadOnlyDictionary<string, SlotInfo> OneYear { get; }
    public IReadOnlyDictionary<string, Propers> OneYearPropers { get; }
    public DailyLectionary? Daily { get; }

    public LiturgicalData(
        IReadOnlyDictionary<string, SlotInfo> sanctoral,
        IReadOnlyDictionary<string, SlotInfo> threeYear,
        IReadOnlyDictionary<string, SlotInfo> oneYear,
        IReadOnlyDictionary<string, Propers> oneYearPropers,
        DailyLectionary? daily = null)
    {
        Sanctoral = sanctoral;
        ThreeYear = threeYear;
        OneYear = oneYear;
        OneYearPropers = oneYearPropers;
        Daily = daily;
        _sanctoralDateMap = new Lazy<Dictionary<(int, int), string>>(BuildSanctoralDateMap);
    }

    /// <summary>
    /// Find the lectionary Data directory by checking common locations.
    /// </summary>
    public static string FindDataDirectory(string? preferredDirectory = null)
    {
        if (!string.IsNullOrWhiteSpace(preferredDirectory) && Directory.Exists(preferredDirectory))
            return preferredDirectory;

        string[] candidates =
        {
            Path.Combine(AppContext.BaseDirectory, "lectionary", "Data"),
            Path.Combine(AppContext.BaseDirectory, "Data", "lectionary"),
            Path.Combine(AppContext.BaseDirectory, "Data"),
            Path.Combine(Directory.GetCurrentDirectory(), "lectionary", "Data"),
            Path.Combine(Directory.GetCurrentDirectory(), "Data", "lectionary"),
            Path.Combine(Directory.GetCurrentDirectory(), "Data"),
        };

        foreach (var candidate in candidates)
        {
            if (Directory.Exists(candidate) && File.Exists(Path.Combine(candidate, "three_year.json")))
                return candidate;
        }

        for (var dir = new DirectoryInfo(Directory.GetCurrentDirectory()); dir is not null; dir = dir.Parent)
        {
            string check1 = Path.Combine(dir.FullName, "lectionary", "Data");
            if (Directory.Exists(check1) && File.Exists(Path.Combine(check1, "three_year.json")))
                return check1;
            string check2 = Path.Combine(dir.FullName, "Data");
            if (Directory.Exists(check2) && File.Exists(Path.Combine(check2, "three_year.json")))
                return check2;
        }

        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            string check1 = Path.Combine(dir.FullName, "lectionary", "Data");
            if (Directory.Exists(check1) && File.Exists(Path.Combine(check1, "three_year.json")))
                return check1;
            string check2 = Path.Combine(dir.FullName, "Data");
            if (Directory.Exists(check2) && File.Exists(Path.Combine(check2, "three_year.json")))
                return check2;
        }

        if (!string.IsNullOrWhiteSpace(preferredDirectory))
            return preferredDirectory;

        throw new DirectoryNotFoundException("Could not locate lectionary Data directory containing JSON files.");
    }

    /// <summary>Load lectionary data using automatic data directory discovery.</summary>
    public static LiturgicalData Load(string? directory = null) =>
        LoadFromDirectory(FindDataDirectory(directory));

    /// <summary>
    /// Load from a directory containing sanctoral.json, three_year.json,
    /// one_year.json, one_year_propers.json and (optionally) daily_lectionary.json.
    /// Each slot file is an object keyed by slot name.
    /// </summary>
    public static LiturgicalData LoadFromDirectory(string directory)
    {
        directory = FindDataDirectory(directory);
        var sanctoral = Read<Dictionary<string, SlotInfo>>(Path.Combine(directory, "sanctoral.json"));
        var threeYear = Read<Dictionary<string, SlotInfo>>(Path.Combine(directory, "three_year.json"));
        var oneYear = Read<Dictionary<string, SlotInfo>>(Path.Combine(directory, "one_year.json"));
        var propers = Read<Dictionary<string, Propers>>(Path.Combine(directory, "one_year_propers.json"));

        string dailyPath = Path.Combine(directory, "daily_lectionary.json");
        DailyLectionary? daily = File.Exists(dailyPath) ? Read<DailyLectionary>(dailyPath) : null;

        return new LiturgicalData(sanctoral, threeYear, oneYear, propers, daily);
    }

    private static T Read<T>(string path)
    {
        using var stream = File.OpenRead(path);
        return JsonSerializer.Deserialize<T>(stream, JsonOptions)
               ?? throw new InvalidDataException($"{path} is empty or null.");
    }

    // ------------------------------------------------------------------
    // Daily lectionary
    // ------------------------------------------------------------------

    /// <summary>
    /// LSB Daily Lectionary readings (OT + NT) for any date.
    /// The window from Ash Wednesday through Holy Trinity is keyed to the
    /// movable Easter cycle and takes precedence over fixed dates.
    /// </summary>
    public Dictionary<string, string?>? DailyReadings(DateOnly d)
    {
        if (Daily is null)
            throw new InvalidOperationException("Daily lectionary data is not available.");

        var easter = CalendarMath.CalcEaster(d.Year);
        var ashWed = easter.AddDays(-46);
        var trinity = easter.AddDays(56);

        if (ashWed <= d && d <= trinity)
        {
            int idx = d.DayNumber - ashWed.DayNumber;
            if (idx >= 0 && idx < Daily.Movable.Count)
                return new Dictionary<string, string?>(Daily.Movable[idx]);
            return null;
        }

        string key = $"{d.Month:D2}-{d.Day:D2}";
        return Daily.Fixed.TryGetValue(key, out var entry)
            ? new Dictionary<string, string?>(entry)
            : null;
    }

    // ------------------------------------------------------------------
    // Sanctoral lookup by calendar date
    // ------------------------------------------------------------------

    private Dictionary<(int Month, int Day), string> BuildSanctoralDateMap()
    {
        var result = new Dictionary<(int, int), string>();
        foreach (var (key, info) in Sanctoral)
        {
            var parts = (info.DateStr ?? "").Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2) continue;
            if (!MonthAbbr.TryGetValue(parts[0], out int month)) continue;
            if (!int.TryParse(parts[1], out int day)) continue;
            result[(month, day)] = key;
        }
        return result;
    }

    /// <summary>Return sanctoral slot info for a given month/day, or null.</summary>
    public SanctoralFeast? SanctoralFeastForDate(int month, int day)
    {
        if (!_sanctoralDateMap.Value.TryGetValue((month, day), out var slotKey))
            return null;
        return Sanctoral.TryGetValue(slotKey, out var info)
            ? new SanctoralFeast(slotKey, info.Clone())
            : null;
    }

    // ------------------------------------------------------------------
    // Slot info dispatcher
    // ------------------------------------------------------------------

    /// <summary>
    /// Resolve a slot key to its info (readings picked for the given series).
    /// Always returns a copy that callers may modify. Null if the slot is unknown.
    /// </summary>
    public SlotInfo? GetSlotInfo(string slot, string series, Lectionary lectionary = Lectionary.ThreeYear)
    {
        // Sanctoral feasts take priority for specific date-tied keys
        if (Sanctoral.TryGetValue(slot, out var sanctoral))
            return sanctoral.Clone();

        if (lectionary == Lectionary.OneYear && OneYear.TryGetValue(slot, out var oneYearSlot))
        {
            var result = oneYearSlot.Clone();
            OneYearPropers.TryGetValue(slot, out var propers);
            result.Collect = propers?.Collect;
            result.Introit = propers?.Introit;
            result.Gradual = propers?.Gradual;
            result.Source = propers?.Source;
            return result;
        }

        if (ThreeYear.TryGetValue(slot, out var threeYear))
        {
            var result = threeYear.Clone();
            result.Readings = threeYear.ForSeries(series);
            result.A = result.B = result.C = result.All = null;
            return result;
        }

        if (OneYear.TryGetValue(slot, out var oneYear))
        {
            var result = oneYear.Clone();
            OneYearPropers.TryGetValue(slot, out var propers);
            result.Collect = propers?.Collect;
            result.Introit = propers?.Introit;
            result.Gradual = propers?.Gradual;
            result.Source = propers?.Source;
            return result;
        }

        // Trinity Sundays beyond what is pre-keyed: generate dynamically
        if (slot.StartsWith("trinity_", StringComparison.Ordinal))
        {
            if (!int.TryParse(slot.Split('_')[1], out int n)) return null;
            string name = n < TrinityOrdinalCount
                ? $"{Utils.Ordinal(n)} Sunday after Trinity"
                : $"Sunday {n} after Trinity";
            OneYearPropers.TryGetValue(slot, out var propers);
            return new SlotInfo
            {
                Name = name,
                Season = "Pentecost",
                Color = "Green",
                Feast = false,
                Readings = null,
                Collect = propers?.Collect,
                Introit = propers?.Introit,
                Gradual = propers?.Gradual,
                Source = propers?.Source,
            };
        }

        // Epiphany Sundays beyond what is pre-keyed: generate dynamically
        if (slot.StartsWith("epiphany_", StringComparison.Ordinal))
        {
            if (!int.TryParse(slot.Split('_')[1], out int n)) return null;
            string name = n < EpiphanyOrdinals.Length
                ? $"{EpiphanyOrdinals[n]} Sunday after the Epiphany"
                : $"Sunday {n} after the Epiphany";
            string baseKey = ThreeYear.ContainsKey(slot) ? slot : $"epiphany_{Math.Min(n, 8)}";
            ThreeYear.TryGetValue(baseKey, out var baseInfo);
            return new SlotInfo
            {
                Name = name,
                Season = "Epiphany",
                Color = "Green",
                Feast = false,
                Readings = baseInfo?.ForSeries(series),
            };
        }

        // Christmas Sundays
        if (slot.StartsWith("christmas_sunday_", StringComparison.Ordinal))
        {
            if (!int.TryParse(slot.Split('_')[^1], out int n)) return null;
            string[] names = { "", "First Sunday after Christmas", "Second Sunday after Christmas" };
            ThreeYear.TryGetValue(slot, out var baseInfo);
            return new SlotInfo
            {
                Name = n < names.Length ? names[n] : $"Sunday after Christmas {n}",
                Season = "Christmas",
                Color = "White",
                Feast = false,
                Readings = baseInfo?.ForSeries(series),
            };
        }

        return null;
    }
}
