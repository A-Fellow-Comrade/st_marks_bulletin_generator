using System.Text.RegularExpressions;
using LiturgyTools;

namespace st_mark_bulletin_generator.Liturgical;

// The one place where the bulletin form talks to the lectionary library
// (the LiturgyTools code in the "lectionary" folder). It turns that library's
// name for a Sunday into a phrase that completes the back-cover sentence
// "Today is ____."
//
//   "Nineteenth Sunday after Pentecost"            -> "the nineteenth Sunday after Pentecost"
//   "The Epiphany of Our Lord"                     -> "the Epiphany of Our Lord"
//   "The Resurrection of Our Lord (Easter Day)"    -> "the Resurrection of Our Lord"
//   "Sunday of the Passion (Palm Sunday)"          -> "Palm Sunday"
//   "Last Sunday of the Church Year"               -> "the last Sunday of the Church Year"
//   anything else (e.g. "All Saints' Day")         -> unchanged
//
// The pastor can always edit the result in the form.
public static class SundayNames
{
    private static LiturgicalCalendar? _calendar;

    // Names that read badly after "Today is" get their own wording here.
    // Add a line if another one turns out to need it (left side = the library's name).
    private static readonly Dictionary<string, string> Overrides = new()
    {
        ["The Holy Trinity"] = "Holy Trinity Sunday",
    };

    private static readonly HashSet<string> OrdinalWords =
        new(Enumerable.Range(1, 27).Select(Utils.Ordinal), StringComparer.OrdinalIgnoreCase);

    // Returns "" (and explains why in `problem`) if the name can't be worked out.
    public static string Get(DateOnly date, out string? problem)
    {
        problem = null;
        if (date.DayOfWeek != DayOfWeek.Sunday) return "";

        try
        {
            // Loading reads the JSON files once; after that the calendar is reused
            // (Lookup builds the right church year for any date by itself).
            _calendar ??= new LiturgicalCalendar(AdventYearFor(date), LiturgicalData.Load());

            var result = _calendar.Lookup(date);
            if (result is null)
            {
                problem = "the lectionary data has no entry for that date";
                return "";
            }
            return ToPhrase(result.Name);
        }
        catch (Exception ex)
        {
            problem = ex.Message;
            return "";
        }
    }

    // The civil year in which this date's church year began (Advent).
    private static int AdventYearFor(DateOnly date) =>
        date >= CalendarMath.Advent1ForYear(date.Year) ? date.Year : date.Year - 1;

    internal static string ToPhrase(string? name)
    {
        var n = (name ?? "").Trim();
        if (n.Length == 0) return "";

        if (Overrides.TryGetValue(n, out var special)) return special;

        // A trailing note such as " (Invocabit)" or " (Easter Day)" is dropped, except
        // when it is the everyday name of the Sunday: "Sunday of the Passion (Palm Sunday)".
        var note = Regex.Match(n, @"\s*\(([^)]*)\)\s*$");
        if (note.Success)
        {
            var inside = note.Groups[1].Value.Trim();
            if (inside.EndsWith(" Sunday", StringComparison.Ordinal)) return inside;
            n = n[..note.Index];
            if (n.Length == 0) return "";
        }

        if (n.StartsWith("The ", StringComparison.Ordinal))
            return "the " + n[4..];

        var firstWord = n.Split(' ', 2)[0];
        if (OrdinalWords.Contains(firstWord) || firstWord == "Last")
            return "the " + char.ToLowerInvariant(n[0]) + n[1..];

        if (firstWord == "Sunday")
            return "the " + n;

        return n;
    }
}