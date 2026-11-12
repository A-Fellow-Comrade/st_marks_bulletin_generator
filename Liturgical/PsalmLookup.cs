using System.Text.RegularExpressions;
using LiturgyTools;

namespace st_mark_bulletin_generator.Liturgical;

// The psalm appointed for a Sunday, as read from the lectionary.
public sealed class AppointedPsalm
{
    public string Display { get; init; } = "";      // "Psalm 89:1-5 (19-29)", antiphon note removed
    public string Chapter { get; init; } = "";      // "89" ("" if the reading isn't a psalm)
    public string Required { get; init; } = "";     // "1-5" ("" = the whole psalm)
    public string Optional { get; init; } = "";     // "19-29" ("" = none)
    public string Alternates { get; init; } = "";   // other readings offered, e.g. "Psalm 31"
    public string? Problem { get; init; }           // why there is nothing usable, if so

    // The verses to print: the required ones, plus the optional ones if asked for.
    public string VersesFor(bool includeOptional)
    {
        if (Required.Length == 0) return "";                 // whole psalm
        if (!includeOptional || Optional.Length == 0) return Required;

        PsalmLookup.TryNormalizeVerses(Required + "," + Optional, out var joined, out _);
        return joined;
    }
}

// What the PDF prints for the psalm, or the reason it couldn't be found.
public sealed class PsalmResult
{
    public string Title { get; init; } = "";                              // "Psalm 118:1-9"
    public (string V, string[] Lines)[] Verses { get; init; } = Array.Empty<(string V, string[] Lines)>();
    public string? Problem { get; init; }

    public int TotalCharacters => Verses.Sum(v => v.Lines.Sum(l => l.Length));
}

// The one place where the bulletin form talks to the lectionary and psalm code
// (the LiturgyTools code in the "lectionary" folder).
public static class PsalmLookup
{
    private static LiturgicalData? _data;
    private static LiturgicalCalendar? _calendar;

    // ───────────────────────────── versions ─────────────────────────────

    // Every Psalm_XXX.csv in the lectionary data folder gives one version, so a
    // new version only needs its file added. ESV first, then the rest A-Z.
    public static List<string> Versions()
    {
        var versions = new List<string>();
        try
        {
            foreach (var file in Directory.EnumerateFiles(LiturgicalData.FindDataDirectory(), "Psalm_*.csv"))
            {
                var name = Path.GetFileNameWithoutExtension(file);
                var version = name["Psalm_".Length..];
                if (version.Length > 0 && !versions.Contains(version, StringComparer.OrdinalIgnoreCase))
                    versions.Add(version);
            }
        }
        catch
        {
            // Fall through to the default below.
        }

        if (versions.Count == 0) versions.Add("ESV");
        return versions
            .OrderBy(v => v.Equals("ESV", StringComparison.OrdinalIgnoreCase) ? 0 : 1)
            .ThenBy(v => v, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    // ───────────────────────────── the appointed psalm ─────────────────────────────

    public static AppointedPsalm FindAppointed(DateOnly date, Lectionary lectionary = Lectionary.ThreeYear)
    {
        try
        {
            _data ??= LiturgicalData.Load();
            var adventYear = date >= CalendarMath.Advent1ForYear(date.Year)
                ? date.Year : date.Year - 1;

            _calendar ??= new LiturgicalCalendar(adventYear, _data);
            var result = _calendar.Lookup(date, lectionary);

            if (result is null) return new AppointedPsalm{Problem = "The lectionary data has no entry for that date."};

            var ps = result.Readings?.Ps;

            if (string.IsNullOrWhiteSpace(ps)) return new AppointedPsalm{ Problem = $"The {LectionaryChoice.Describe(lectionary)} lectionary lists no psalm for this Sunday. " +
                "Type a psalm number if you would like one.",};

            return ParseAppointed(ps);
        }
        catch (Exception ex)
        {
            return new AppointedPsalm{ Problem = $"Couldn't look up the appointed psalm ({ex.Message})."};
        }
    }
    // Matches "Psalm 89", "Psalm 89:1-5 (19-29)", "Psalm 115:(1-8) 9-18".
    private static readonly Regex ReferencePattern = new(
        @"^(?<book>[A-Za-z]+(?: [A-Za-z]+)*?)\s+(?<chapter>\d+)(?:\s*:\s*(?<rest>.*))?$",
        RegexOptions.Compiled);

    // Turns the lectionary's text into chapter, required verses and optional verses.
    // In the lectionary, verses in parentheses are optional, whether they come
    // after the required ones ("1-9 (10-14)") or before ("(1-8) 9-18").
    public static AppointedPsalm ParseAppointed(string ps)
    {
        var parts = ps.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0)
            return new AppointedPsalm { Problem = "The lectionary lists no psalm for this Sunday." };

        var first = CleanReference(parts[0]);
        var alternates = string.Join("; ", parts.Skip(1).Select(CleanReference).Where(p => p.Length > 0));

        var m = ReferencePattern.Match(first);
        if (!m.Success || !m.Groups["book"].Value.Equals("Psalm", StringComparison.OrdinalIgnoreCase))
        {
            return new AppointedPsalm
            {
                Display = first,
                Alternates = alternates,
                Problem = $"The lectionary appoints \"{first}\", which isn't a psalm number. Type the psalm number yourself.",
            };
        }

        var chapter = m.Groups["chapter"].Value;
        var rest = m.Groups["rest"].Success ? m.Groups["rest"].Value : "";

        var optionalParts = new List<string>();
        var requiredText = Regex.Replace(rest, @"\(([^)]*)\)", match =>
        {
            optionalParts.Add(match.Groups[1].Value);
            return " ";
        }).Trim();

        if (!TryNormalizeVerses(requiredText, out var required, out _) ||
            !TryNormalizeVerses(string.Join(",", optionalParts), out var optional, out _))
        {
            return new AppointedPsalm
            {
                Display = first,
                Chapter = chapter,
                Alternates = alternates,
                Problem = $"Couldn't read the verses in \"{first}\". Type the verses yourself.",
            };
        }

        // Only parentheses: nothing is required, so the whole psalm is printed.
        if (required.Length == 0) optional = "";

        return new AppointedPsalm
        {
            Display = first,
            Chapter = chapter,
            Required = required,
            Optional = optional,
            Alternates = alternates,
        };
    }

    // Drops the "(antiphon: v. 7)" note, which isn't part of the verses, and tidies dashes.
    private static string CleanReference(string text)
    {
        text = Regex.Replace(text, @"\s*\(\s*antiphon\s*:[^)]*\)", "", RegexOptions.IgnoreCase);
        text = text.Replace('\u2013', '-').Replace('\u2014', '-');
        return Regex.Replace(text, @"\s+", " ").Trim();
    }

    // ───────────────────────────── verse lists ─────────────────────────────

    // Reads a verse list such as "1-9", "1-4, 23-26" or "7a-10" and writes it back
    // sorted, merged and in one style ("1-4, 23-26"). Blank means the whole psalm.
    public static bool TryNormalizeVerses(string? text, out string normalized, out string? problem)
    {
        normalized = "";
        problem = null;

        text = (text ?? "").Replace('\u2013', '-').Replace('\u2014', '-');
        text = Regex.Replace(text, @"(?<=\d)[a-cA-C](?![A-Za-z])", "");   // "7a" means verse 7
        text = text.Trim();
        if (text.Length == 0) return true;

        const string Example = "Verses should look like 1-9 or 1-5, 8 (leave blank for the whole psalm).";

        if (!Regex.IsMatch(text, @"^[0-9,\-\s]+$"))
        {
            problem = Example;
            return false;
        }

        var verses = new SortedSet<int>();
        foreach (var segment in text.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var bits = segment.Split('-', StringSplitOptions.TrimEntries);
            int start, end;

            if (bits.Length == 1 && int.TryParse(bits[0], out start))
                end = start;
            else if (bits.Length == 2 && int.TryParse(bits[0], out start) && int.TryParse(bits[1], out end))
            {
                // fine
            }
            else
            {
                problem = Example;
                return false;
            }

            if (start < 1 || end < start || end > 200)
            {
                problem = Example;
                return false;
            }
            for (int v = start; v <= end; v++) verses.Add(v);
        }

        normalized = FormatVerses(verses);
        return true;
    }

    private static string FormatVerses(SortedSet<int> verses)
    {
        var ranges = new List<string>();
        int? start = null, previous = null;

        void Close()
        {
            if (start is null || previous is null) return;
            ranges.Add(start == previous ? $"{start}" : $"{start}-{previous}");
        }

        foreach (var v in verses)
        {
            if (previous is int p && v == p + 1)
            {
                previous = v;
                continue;
            }
            Close();
            start = previous = v;
        }
        Close();

        return string.Join(", ", ranges);
    }

    // ───────────────────────────── fetching the text ─────────────────────────────

    // Fetches a psalm for the PDF. Never throws: if something is wrong, Problem says what.
    public static PsalmResult Load(string? version, string? number, string? verses)
    {
        version = (version ?? "").Trim();
        number = (number ?? "").Trim();

        if (!int.TryParse(number, out var chapter) || chapter < 1 || chapter > 150)
            return Fail("Enter a psalm number from 1 to 150.");
        if (version.Length == 0)
            return Fail("Choose a psalm version.");
        if (!TryNormalizeVerses(verses, out var normalized, out var problem))
            return Fail(problem ?? "Couldn't read the verses.");

        try
        {
            var raw = PsalmParser.GetPsalm("Psalm", chapter, normalized, version);
            if (raw.Length == 0)
                return Fail($"Psalm {chapter} isn't in the {version} text.");

            var tidy = raw
                .Select(v => (V: v.V, Lines: v.Lines.Select(Tidy).Where(l => l.Length > 0).ToArray()))
                .ToArray();

            return new PsalmResult
            {
                Title = normalized.Length == 0 ? $"Psalm {chapter}" : $"Psalm {chapter}:{normalized}",
                Verses = tidy,
            };
        }
        catch (FileNotFoundException)
        {
            return Fail($"The {version} psalm text file wasn't found.");
        }
        catch (KeyNotFoundException ex)
        {
            var m = Regex.Match(ex.Message, @":(\d+):(\d+)");
            return Fail(m.Success
                ? $"Psalm {m.Groups[1].Value} doesn't have a verse {m.Groups[2].Value} in the {version} text."
                : ex.Message);
        }
        catch (Exception ex)
        {
            return Fail(ex.Message);
        }
    }

    private static PsalmResult Fail(string problem) => new() { Problem = problem };

    // The text files have stray spaces: "the Lord , and" or "\u201C There".
    private static string Tidy(string line)
    {
        line = Regex.Replace(line, @"\s+([,.;:!?])", "$1");
        line = Regex.Replace(line, @"([\u201C\u2018])\s+", "$1");
        return Regex.Replace(line, @"\s{2,}", " ").Trim();
    }
}