using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace LiturgyTools;

/// <summary>A single parsed scripture reading.</summary>
public record Reading(
    string Label,
    string Ref,
    List<string> Alts,
    string Url,
    List<string> AltUrls);

/// <summary>Utility helpers for display, BibleGateway links, and file naming.</summary>
public static class Utils
{
    private static readonly string[] Ordinals =
    {
        "", "First", "Second", "Third", "Fourth", "Fifth",
        "Sixth", "Seventh", "Eighth", "Ninth", "Tenth",
        "Eleventh", "Twelfth", "Thirteenth", "Fourteenth", "Fifteenth",
        "Sixteenth", "Seventeenth", "Eighteenth", "Nineteenth", "Twentieth",
        "Twenty-first", "Twenty-second", "Twenty-third", "Twenty-fourth",
        "Twenty-fifth", "Twenty-sixth", "Twenty-seventh",
    };

    private static readonly Dictionary<string, string> CanticleMapping = new()
    {
        ["the song of moses and israel"] = "Exodus 15:1-18",
        ["the song of moses"] = "Exodus 15:1-18",
    };

    private static readonly Dictionary<string, string> SeasonColors = new()
    {
        ["Blue"] = "season-blue",
        ["White"] = "season-white",
        ["Red"] = "season-red",
        ["Green"] = "season-green",
        ["Purple"] = "season-purple",
        ["Black"] = "season-black",
        ["Scarlet"] = "season-scarlet",
    };

    private static readonly (string Key, string Label)[] LabelMap =
    {
        ("ot", "First Reading"),
        ("ps", "Psalm"),
        ("ep", "Epistle"),
        ("go", "Gospel"),
    };

    public static string Ordinal(int n) =>
        n > 0 && n < Ordinals.Length ? Ordinals[n] : n.ToString(CultureInfo.InvariantCulture);

    public static string CleanPassageForBibleGateway(string? reference)
    {
        if (string.IsNullOrEmpty(reference))
            return "";

        string refText = reference;

        // Map specific canticles/non-standard references to actual scripture books/ranges
        string lower = refText.Trim().ToLowerInvariant();
        if (CanticleMapping.TryGetValue(lower, out var mapped))
            return mapped;

        // Remove antiphon annotations (e.g. "(antiphon: v. 7)")
        refText = Regex.Replace(refText, @"\s*\(\s*antiphon:[^)]*\)", "", RegexOptions.IgnoreCase);

        // Remove other non-scripture parenthesized notes like "(Palm Sunday Procession)"
        refText = Regex.Replace(refText, @"\s*\(([^)]+)\)", match =>
        {
            string inside = match.Groups[1].Value;
            if (Regex.IsMatch(inside, @"[e-uw-z]{2,}", RegexOptions.IgnoreCase) ||
                Regex.IsMatch(inside, @"[a-z]{4,}", RegexOptions.IgnoreCase))
            {
                return "";
            }
            return match.Value;
        });

        // Handle parentheses around verse ranges
        refText = Regex.Replace(refText, @":\s*\(\s*([^)]+)\)\s*", ":$1, ");
        refText = Regex.Replace(refText, @"\s+\(\s*([^)]+)\)", ", $1");
        refText = Regex.Replace(refText, @"\(([^)]+)\)", ", $1");

        // Handle multiple alternatives split by 'or' or '|'
        refText = refText.Replace(" | ", "; ");
        refText = Regex.Replace(refText, @"\s+or\s+", "; ", RegexOptions.IgnoreCase);

        // Convert en-dash / em-dash to standard hyphen
        refText = refText.Replace('–', '-').Replace('—', '-');

        // Remove verse letters (a, b, etc.) at the end of numbers in ranges
        refText = Regex.Replace(refText, @"(\d+)[a-z]\b", "$1", RegexOptions.IgnoreCase);

        // Clean up whitespace and punctuation
        refText = Regex.Replace(refText, @"\s+", " ");
        refText = Regex.Replace(refText, @",\s*,", ",");
        refText = Regex.Replace(refText, @":\s*,", ":");
        refText = Regex.Replace(refText, @";\s*;", ";");
        refText = refText.Trim().Trim(',').Trim(';').Trim();

        return refText;
    }

    /// <summary>Return a BibleGateway URL for a scripture reference.</summary>
    public static string BgUrl(string? reference, string version = "ESV")
    {
        if (string.IsNullOrEmpty(reference))
            return "#";

        string refClean = CleanPassageForBibleGateway(reference);
        // Python's quote() leaves '/' unescaped by default; mirror that here.
        string encoded = Uri.EscapeDataString(refClean).Replace("%2F", "/");
        return $"https://www.biblegateway.com/passage/?search={encoded}&version={version}";
    }

    /// <summary>
    /// Convert scripture readings into a list of Reading records.
    /// </summary>
    public static List<Reading> ParseReadings(Readings? readings) =>
        ParseReadings(readings?.ToDictionary());

    /// <summary>
    /// Convert a readings dict {ot, ps, ep, go} into a list of Reading records,
    /// handling ' | ' alternatives.
    /// </summary>
    public static List<Reading> ParseReadings(IDictionary<string, string?>? readings)
    {
        var result = new List<Reading>();
        if (readings is null || readings.Count == 0)
            return result;

        foreach (var (key, label) in LabelMap)
        {
            if (!readings.TryGetValue(key, out var val) || string.IsNullOrEmpty(val))
                continue;

            // Handle alternative readings split by ' | '
            var parts = new List<string>();
            foreach (var p in val.Split(" | "))
                parts.Add(p.Trim());

            string primary = parts[0];
            var alts = parts.GetRange(1, parts.Count - 1);

            result.Add(new Reading(
                label,
                primary,
                alts,
                BgUrl(primary),
                alts.ConvertAll(a => BgUrl(a))));
        }
        return result;
    }

    /// <summary>Make a string safe for use in a filename.</summary>
    public static string SafeFilename(string text)
    {
        text = text.Replace("/", "-").Replace("\\", "-");
        text = Regex.Replace(text, "[<>:\"|?*]", "");
        return text.Trim();
    }

    /// <summary>Return a filename-safe label, e.g. "2026-06-07 Second Sunday after Pentecost".</summary>
    public static string FileLabel(DateTime d, string name) =>
        SafeFilename($"{d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)} {name}");

    public static string FileLabel(DateOnly d, string name) =>
        SafeFilename($"{d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)} {name}");

    /// <summary>Map liturgical color name to a CSS class.</summary>
    public static string SeasonColorClass(string? color) =>
        color is not null && SeasonColors.TryGetValue(color, out var cls) ? cls : "season-green";
}
