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
    [JsonPropertyName("introit")] public string? Introit { get; set; }
    [JsonPropertyName("introit_text")] public string? IntroitText { get; set; }
    [JsonPropertyName("gradual")] public string? Gradual { get; set; }
    [JsonPropertyName("source")] public string? Source { get; set; }

    // Three-year lectionary only: readings per series, with "all" as fallback.
    [JsonPropertyName("A")] public Readings? A { get; set; }
    [JsonPropertyName("B")] public Readings? B { get; set; }
    [JsonPropertyName("C")] public Readings? C { get; set; }
    [JsonPropertyName("all")] public Readings? All { get; set; }

    /// <summary>Readings for series "A", "B" or "C", falling back to "all".</summary>
    public Readings? ForSeries(string series)
    {
        var bySeries = series switch
        {
            "A" => A,
            "B" => B,
            "C" => C,
            _ => null,
        };
        return bySeries ?? All;
    }

    public SlotInfo Clone() => (SlotInfo)MemberwiseClone();
}

/// <summary>One-year lectionary propers (collect, introit, gradual, source).</summary>
public sealed class Propers
{
    [JsonPropertyName("collect")] public string? Collect { get; init; }
    [JsonPropertyName("introit")] public string? Introit { get; init; }
    [JsonPropertyName("gradual")] public string? Gradual { get; init; }
    [JsonPropertyName("source")] public string? Source { get; init; }
}

/// <summary>
/// Daily lectionary data. "fixed" is keyed by "MM-dd"; "movable" is indexed by
/// days since Ash Wednesday.
/// </summary>
public sealed class DailyLectionary
{
    [JsonPropertyName("fixed")]
    public Dictionary<string, Dictionary<string, string?>> Fixed { get; init; } = new();

    [JsonPropertyName("movable")]
    public List<Dictionary<string, string?>> Movable { get; init; } = new();
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
    /// Load from a directory containing sanctoral.json, three_year.json,
    /// one_year.json, one_year_propers.json and (optionally) daily_lectionary.json.
    /// Each slot file is an object keyed by slot name.
    /// </summary>
    public static LiturgicalData LoadFromDirectory(string directory)
    {
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
            return new Dictionary<string, string?>(Daily.Movable[idx]);
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
    public SlotInfo? GetSlotInfo(string slot, string series)
    {
        // Sanctoral feasts take priority for specific date-tied keys
        if (Sanctoral.TryGetValue(slot, out var sanctoral))
            return sanctoral.Clone();

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
            return new SlotInfo
            {
                Name = name,
                Season = "Pentecost",
                Color = "Green",
                Feast = false,
                Readings = null,
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
