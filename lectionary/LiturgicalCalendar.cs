using System;
using System.Collections.Generic;
using System.Linq;

namespace LiturgyTools;

/// <summary>Date arithmetic helpers (Easter, Sundays, propers).</summary>
public static class CalendarMath
{
    /// <summary>ISO weekday: 1 = Monday … 7 = Sunday.</summary>
    public static int IsoWeekday(DateOnly d) =>
        d.DayOfWeek == DayOfWeek.Sunday ? 7 : (int)d.DayOfWeek;

    /// <summary>Easter Sunday (Anonymous Gregorian algorithm).</summary>
    public static DateOnly CalcEaster(int year)
    {
        int a = year % 19;
        int b = year / 100;
        int c = year % 100;
        int d = (19 * a + b - b / 4 - ((b - (b + 8) / 25 + 1) / 3) + 15) % 30;
        int e = (32 + 2 * (b % 4) + 2 * (c / 4) - d - (c % 4)) % 7;
        int f = d + e - 7 * ((a + 11 * d + 22 * e) / 451) + 114;
        int month = f / 31;
        int day = f % 31 + 1;
        return new DateOnly(year, month, day);
    }

    public static DateOnly FirstSundayOnOrAfter(DateOnly d)
    {
        int dow = IsoWeekday(d);
        return dow == 7 ? d : d.AddDays(7 - dow);
    }

    /// <summary>First Sunday of Advent for the church year that BEGINS in adventYear.</summary>
    public static DateOnly Advent1ForYear(int adventYear) =>
        FirstSundayOnOrAfter(new DateOnly(adventYear, 11, 27));

    /// <summary>"A", "B" or "C" for the three-year lectionary series.</summary>
    public static string GetSeries(int adventYear) =>
        new[] { "A", "B", "C" }[adventYear % 3];

    /// <summary>
    /// Proper number (3–29) for a Sunday in Ordinary Time.
    /// Proper 3 = Sunday closest to May 25; each subsequent proper is 7 days later.
    /// </summary>
    public static int GetProper(DateOnly sunday)
    {
        var baseDate = new DateOnly(sunday.Year, 5, 25); // Proper 3 anchor
        int delta = sunday.DayNumber - baseDate.DayNumber;
        int proper = 3 + (int)Math.Round(delta / 7.0);
        return Math.Max(3, Math.Min(29, proper));
    }

    /// <summary>Dates from start up to (but not including) end.</summary>
    public static IEnumerable<DateOnly> DateRange(DateOnly start, DateOnly end)
    {
        for (var d = start; d < end; d = d.AddDays(1))
            yield return d;
    }

    /// <summary>Sundays from start up to (but not including) end.</summary>
    public static IEnumerable<DateOnly> SundayRange(DateOnly start, DateOnly end)
    {
        for (var d = FirstSundayOnOrAfter(start); d < end; d = d.AddDays(7))
            yield return d;
    }

    /// <summary>The Sunday nearest to d (ties go backward, as in the original).</summary>
    public static DateOnly NearestSunday(DateOnly d)
    {
        int dow = IsoWeekday(d);
        if (dow == 7) return d;
        int back = dow;      // days back to previous Sunday
        int fwd = 7 - dow;   // days forward to next Sunday
        return back <= fwd ? d.AddDays(-back) : d.AddDays(fwd);
    }

    /// <summary>The n-th Thursday of the given month/year.</summary>
    public static DateOnly NthThursday(int year, int month, int n)
    {
        var first = new DateOnly(year, month, 1);
        int offset = (((4 - IsoWeekday(first)) % 7) + 7) % 7; // Thu = 4
        return first.AddDays(offset + 7 * (n - 1));
    }
}

/// <summary>One entry in the output of <see cref="LiturgicalCalendar.AllEvents"/>.</summary>
public sealed class LiturgicalEvent
{
    public DateOnly Date { get; set; }
    public string Slot { get; set; } = "";
    public string Name { get; set; } = "";
    public string Season { get; set; } = "";
    public string Color { get; set; } = "";
    public string Series { get; set; } = "";
    public bool IsSunday { get; set; }
    public bool IsFeast { get; set; }
    public Readings? Readings { get; set; }
    public int? Proper { get; set; }
    public int? Ordinal { get; set; }
    public bool Minor { get; set; }
    public string? Collect { get; set; }
    public Introit? Introit { get; set; }
    public string? IntroitText { get; set; }
    public string? Gradual { get; set; }
    public string? Source { get; set; }

    // Set when Reformation / All Saints falls on this Sunday
    public string? AltName { get; set; }
    public Readings? AltReadings { get; set; }
}

/// <summary>Result of <see cref="LiturgicalCalendar.Lookup"/>.</summary>
public sealed class LookupResult
{
    public DateOnly Date { get; init; }
    public string Slot { get; init; } = "";
    public string ChurchYear { get; init; } = "";
    public string Series { get; init; } = "";
    public bool IsWeekday { get; init; }
    public DateOnly? GoverningDate { get; init; }
    public SanctoralFeast? MinorFeast { get; init; }

    /// <summary>Name, season, color, readings, propers, etc. for the slot.</summary>
    public SlotInfo Info { get; init; } = new();

    public string Name => Info.Name;
    public string Season => Info.Season;
    public string Color => Info.Color;
    public Readings? Readings => Info.Readings;
    public string? Collect => Info.Collect;
    public Introit? Introit => Info.Introit;
    public string? IntroitText => Info.IntroitText ?? Info.Introit?.Text;
    public string? Gradual => Info.Gradual;
    public string? Source => Info.Source;
}

/// <summary>
/// Computes all liturgically significant dates for a single church year
/// (LCMS / Lutheran Service Book).
/// </summary>
public sealed class LiturgicalCalendar
{
    public const int MinAdventYear = 1583;
    public const int MaxAdventYear = 2299;

    private readonly LiturgicalData _data;

    private readonly List<DateOnly> _christmasSundays;
    private readonly List<DateOnly> _epiphanySundays;
    private readonly List<DateOnly> _epiphanySundays1Yr;
    private readonly List<DateOnly> _pentecostSundays;

    /// <param name="adventYear">
    /// Civil year in which Advent begins (e.g. 2025 for the 2025–2026 church year).
    /// Valid range: 1583–2299.
    /// </param>
    /// <param name="data">Lectionary data tables.</param>
    public LiturgicalCalendar(int adventYear, LiturgicalData data)
    {
        if (adventYear < MinAdventYear || adventYear > MaxAdventYear)
            throw new ArgumentOutOfRangeException(
                nameof(adventYear), $"advent_year must be {MinAdventYear}–{MaxAdventYear}");

        _data = data;
        AdventYear = adventYear;
        CivilYear = adventYear + 1;
        Series = CalendarMath.GetSeries(adventYear);

        int y = AdventYear;
        int cy = CivilYear;

        // --- Advent ---
        Advent1 = CalendarMath.Advent1ForYear(y);
        Advent2 = Advent1.AddDays(7);
        Advent3 = Advent1.AddDays(14);
        Advent4 = Advent1.AddDays(21);

        // --- Christmas & New Year ---
        Christmas = new DateOnly(y, 12, 25);
        HolyInnocents = new DateOnly(y, 12, 28);
        NewYearsEve = new DateOnly(y, 12, 31);
        NewYearsDay = new DateOnly(cy, 1, 1);

        // Sundays between Christmas and Epiphany
        _christmasSundays = CalendarMath
            .DateRange(Christmas.AddDays(1), new DateOnly(cy, 1, 6))
            .Where(d => CalendarMath.IsoWeekday(d) == 7)
            .ToList();

        // --- Epiphany ---
        Epiphany = new DateOnly(cy, 1, 6);
        // Baptism of Our Lord = first Sunday AFTER Jan 6
        BaptismOfLord = CalendarMath.FirstSundayOnOrAfter(new DateOnly(cy, 1, 7));

        // --- Easter & dependent dates ---
        Easter = CalendarMath.CalcEaster(cy);
        AshWednesday = Easter.AddDays(-46);
        // Transfiguration (Three-Year) = Sunday immediately before Ash Wednesday
        Transfiguration = AshWednesday.AddDays(-3);

        // --- Pre-Lent (One-Year series only) ---
        Septuagesima = Easter.AddDays(-63);
        Sexagesima = Easter.AddDays(-56);
        Quinquagesima = Easter.AddDays(-49);

        // Transfiguration (One-Year) = Sunday before Septuagesima
        Transfiguration1Yr = Septuagesima.AddDays(-7);

        // Sundays after Epiphany (2nd … last before Transfiguration)
        _epiphanySundays = CalendarMath
            .SundayRange(BaptismOfLord.AddDays(7), Transfiguration).ToList();
        _epiphanySundays1Yr = CalendarMath
            .SundayRange(BaptismOfLord.AddDays(7), Transfiguration1Yr).ToList();

        // --- Lent ---
        Lent1 = AshWednesday.AddDays(4);
        Lent2 = Lent1.AddDays(7);
        Lent3 = Lent1.AddDays(14);
        Lent4 = Lent1.AddDays(21);
        Lent5 = Lent1.AddDays(28);
        PalmSunday = Easter.AddDays(-7);
        MaundyThursday = Easter.AddDays(-3);
        GoodFriday = Easter.AddDays(-2);

        // --- Easter Season ---
        Easter2 = Easter.AddDays(7);
        Easter3 = Easter.AddDays(14);
        Easter4 = Easter.AddDays(21);
        Easter5 = Easter.AddDays(28);
        Easter6 = Easter.AddDays(35);
        Ascension = Easter.AddDays(39); // always Thursday
        Easter7 = Easter.AddDays(42);
        Pentecost = Easter.AddDays(49);

        // --- Season after Pentecost ---
        HolyTrinity = Pentecost.AddDays(7);
        NextAdvent1 = CalendarMath.Advent1ForYear(cy);
        LastSunday = NextAdvent1.AddDays(-7);

        // Sundays after Pentecost (from 2nd Sunday after Pentecost onward)
        _pentecostSundays = CalendarMath
            .SundayRange(HolyTrinity.AddDays(7), LastSunday).ToList();

        // --- Fixed observances ---
        ReformationDay = new DateOnly(cy, 10, 31);
        AllSaintsDay = new DateOnly(cy, 11, 1);
        Thanksgiving = CalendarMath.NthThursday(cy, 11, 4);

        // Observed Sundays for moveable observances
        ReformationObserved = CalendarMath.NearestSunday(ReformationDay);
        AllSaintsObserved = CalendarMath.NearestSunday(AllSaintsDay);
    }

    // ------------------------------------------------------------------
    // Properties
    // ------------------------------------------------------------------

    public int AdventYear { get; }
    public int CivilYear { get; }
    public string Series { get; }

    public DateOnly Advent1 { get; }
    public DateOnly Advent2 { get; }
    public DateOnly Advent3 { get; }
    public DateOnly Advent4 { get; }

    public DateOnly Christmas { get; }
    public DateOnly HolyInnocents { get; }
    public DateOnly NewYearsEve { get; }
    public DateOnly NewYearsDay { get; }

    public DateOnly Epiphany { get; }
    public DateOnly BaptismOfLord { get; }

    public DateOnly Easter { get; }
    public DateOnly AshWednesday { get; }
    public DateOnly Transfiguration { get; }
    public DateOnly Septuagesima { get; }
    public DateOnly Sexagesima { get; }
    public DateOnly Quinquagesima { get; }
    public DateOnly Transfiguration1Yr { get; }

    public DateOnly Lent1 { get; }
    public DateOnly Lent2 { get; }
    public DateOnly Lent3 { get; }
    public DateOnly Lent4 { get; }
    public DateOnly Lent5 { get; }
    public DateOnly PalmSunday { get; }
    public DateOnly MaundyThursday { get; }
    public DateOnly GoodFriday { get; }

    public DateOnly Easter2 { get; }
    public DateOnly Easter3 { get; }
    public DateOnly Easter4 { get; }
    public DateOnly Easter5 { get; }
    public DateOnly Easter6 { get; }
    public DateOnly Ascension { get; }
    public DateOnly Easter7 { get; }
    public DateOnly Pentecost { get; }

    public DateOnly HolyTrinity { get; }
    public DateOnly NextAdvent1 { get; }
    public DateOnly LastSunday { get; }

    public DateOnly ReformationDay { get; }
    public DateOnly AllSaintsDay { get; }
    public DateOnly Thanksgiving { get; }
    public DateOnly ReformationObserved { get; }
    public DateOnly AllSaintsObserved { get; }

    // ------------------------------------------------------------------
    // Public API
    // ------------------------------------------------------------------

    /// <summary>
    /// Liturgical slot key for a date (e.g. "advent_1", "proper_15", "easter"),
    /// or null if the date is outside this church year or has no slot.
    /// </summary>
    public string? DateToSlot(DateOnly d, Lectionary lectionary = Lectionary.ThreeYear)
    {
        if (d < Advent1 || d >= NextAdvent1)
            return null;

        // Fixed-date feasts first
        if (d == Christmas) return "christmas_day";
        if (d == HolyInnocents) return "holy_innocents";
        if (d == NewYearsEve) return "new_years_eve";
        if (d == NewYearsDay) return "new_years_day";
        if (d == Epiphany) return "epiphany";
        if (d == AshWednesday) return "ash_wednesday";
        if (d == MaundyThursday) return "maundy_thursday";
        if (d == GoodFriday) return "good_friday";
        if (d == Ascension) return "ascension";
        if (d == Thanksgiving) return "thanksgiving";

        // Sundays
        if (CalendarMath.IsoWeekday(d) != 7)
            return null; // non-Sunday, non-feast: no slot

        if (d == Advent1) return "advent_1";
        if (d == Advent2) return "advent_2";
        if (d == Advent3) return "advent_3";
        if (d == Advent4) return "advent_4";

        int christmasIdx = _christmasSundays.IndexOf(d);
        if (christmasIdx >= 0)
            return $"christmas_sunday_{christmasIdx + 1}";

        if (d == BaptismOfLord) return "baptism_of_lord";

        if (lectionary == Lectionary.OneYear)
        {
            int idx = _epiphanySundays1Yr.IndexOf(d);
            if (idx >= 0) return $"epiphany_{idx + 2}";
            if (d == Transfiguration1Yr) return "transfiguration";
            if (d == Septuagesima) return "septuagesima";
            if (d == Sexagesima) return "sexagesima";
            if (d == Quinquagesima) return "quinquagesima";
        }
        else
        {
            int idx = _epiphanySundays.IndexOf(d);
            if (idx >= 0) return $"epiphany_{idx + 2}"; // 2nd, 3rd, …
            if (d == Transfiguration) return "transfiguration";
        }

        if (d == Lent1) return "lent_1";
        if (d == Lent2) return "lent_2";
        if (d == Lent3) return "lent_3";
        if (d == Lent4) return "lent_4";
        if (d == Lent5) return "lent_5";
        if (d == PalmSunday) return "palm_sunday";
        if (d == Easter) return "easter";
        if (d == Easter2) return "easter_2";
        if (d == Easter3) return "easter_3";
        if (d == Easter4) return "easter_4";
        if (d == Easter5) return "easter_5";
        if (d == Easter6) return "easter_6";
        if (d == Easter7) return "easter_7";
        if (d == Pentecost) return "pentecost";
        if (d == HolyTrinity) return "holy_trinity";
        if (d == LastSunday) return "last_sunday";

        // Reformation / All Saints (observed Sunday)
        if (d == ReformationObserved) return "reformation";
        if (d == AllSaintsObserved) return "all_saints";

        // St. Michael and All Angels (Sept 29 → observed Sunday)
        if (d == CalendarMath.NearestSunday(new DateOnly(CivilYear, 9, 29)))
            return "st_michael";

        // Season after Pentecost
        if (_pentecostSundays.Contains(d))
        {
            if (lectionary == Lectionary.OneYear)
            {
                int n = (d.DayNumber - HolyTrinity.DayNumber) / 7;
                return $"trinity_{n}";
            }
            return $"proper_{CalendarMath.GetProper(d)}";
        }

        return null; // unknown / unassigned
    }

    /// <summary>Ordered list of all liturgical events for this church year.</summary>
    public List<LiturgicalEvent> AllEvents(
        bool includeMinor = true, Lectionary lectionary = Lectionary.ThreeYear)
    {
        var events = new List<LiturgicalEvent>();

        void Add(DateOnly d, string slot)
        {
            var info = _data.GetSlotInfo(slot, Series, lectionary);
            if (info is null) return;
            if (!includeMinor && info.Minor) return;

            // Merge one-year propers (collect + introit + gradual) for shared slots
            string? collect = info.Collect;
            Introit? introit = info.Introit;
            string? introitText = info.IntroitText ?? info.Introit?.Text;
            string? gradual = info.Gradual;
            string? source = info.Source;
            if (lectionary == Lectionary.OneYear && string.IsNullOrEmpty(collect))
            {
                _data.OneYearPropers.TryGetValue(slot, out var propers);
                collect = propers?.Collect;
                introit = propers?.Introit;
                introitText = propers?.Introit?.Text;
                gradual = propers?.Gradual;
                source = propers?.Source;
            }

            events.Add(new LiturgicalEvent
            {
                Date = d,
                Slot = slot,
                Name = info.Name,
                Season = info.Season,
                Color = info.Color,
                Series = Series,
                IsSunday = CalendarMath.IsoWeekday(d) == 7,
                IsFeast = info.Feast,
                Readings = info.Readings,
                Proper = info.Proper,
                Ordinal = info.Ordinal,
                Minor = info.Minor,
                Collect = collect,
                Introit = introit,
                IntroitText = introitText,
                Gradual = gradual,
                Source = source,
            });
        }

        // Walk the calendar in order
        Add(Advent1, "advent_1");
        Add(Advent2, "advent_2");
        Add(Advent3, "advent_3");
        Add(Advent4, "advent_4");

        // Christmas Eve (Dec 24 evening service)
        Add(new DateOnly(AdventYear, 12, 24), "christmas_eve");
        Add(Christmas, "christmas_day");
        Add(HolyInnocents, "holy_innocents");

        for (int i = 0; i < _christmasSundays.Count; i++)
            Add(_christmasSundays[i], $"christmas_sunday_{i + 1}");

        Add(NewYearsEve, "new_years_eve");
        Add(NewYearsDay, "new_years_day");
        Add(Epiphany, "epiphany");
        Add(BaptismOfLord, "baptism_of_lord");

        if (lectionary == Lectionary.OneYear)
        {
            for (int i = 0; i < _epiphanySundays1Yr.Count; i++)
                Add(_epiphanySundays1Yr[i], $"epiphany_{i + 2}");
            Add(Transfiguration1Yr, "transfiguration");
            Add(Septuagesima, "septuagesima");
            Add(Sexagesima, "sexagesima");
            Add(Quinquagesima, "quinquagesima");
        }
        else
        {
            for (int i = 0; i < _epiphanySundays.Count; i++)
                Add(_epiphanySundays[i], $"epiphany_{i + 2}");
            Add(Transfiguration, "transfiguration");
        }

        Add(AshWednesday, "ash_wednesday");
        Add(Lent1, "lent_1");
        Add(Lent2, "lent_2");
        Add(Lent3, "lent_3");
        Add(Lent4, "lent_4");
        Add(Lent5, "lent_5");
        Add(PalmSunday, "palm_sunday");
        Add(MaundyThursday, "maundy_thursday");
        Add(GoodFriday, "good_friday");
        Add(Easter, "easter");
        Add(Easter2, "easter_2");
        Add(Easter3, "easter_3");
        Add(Easter4, "easter_4");
        Add(Easter5, "easter_5");
        Add(Easter6, "easter_6");
        Add(Ascension, "ascension");
        Add(Easter7, "easter_7");
        Add(Pentecost, "pentecost");
        Add(HolyTrinity, "holy_trinity");

        // Season after Pentecost
        foreach (var s in _pentecostSundays)
        {
            int nAfter = (s.DayNumber - HolyTrinity.DayNumber) / 7; // trinity_1 = 1st Sunday after Trinity

            if (lectionary == Lectionary.OneYear)
            {
                string slot = $"trinity_{nAfter}";
                var info = _data.GetSlotInfo(slot, Series, lectionary);
                if (info is null) continue;
                if (!includeMinor && info.Minor) continue;

                _data.OneYearPropers.TryGetValue(slot, out var propers);
                var propersIntroit = propers?.Introit ?? info.Introit;
                events.Add(new LiturgicalEvent
                {
                    Date = s,
                    Slot = slot,
                    Name = TrinityOrdinalName(s),
                    Season = "Trinity",
                    Color = info.Color,
                    Series = Series,
                    IsSunday = true,
                    IsFeast = false,
                    Readings = info.Readings,
                    Ordinal = nAfter,
                    Minor = false,
                    Collect = propers?.Collect ?? info.Collect,
                    Introit = propersIntroit,
                    IntroitText = propersIntroit?.Text ?? info.IntroitText,
                    Gradual = propers?.Gradual ?? info.Gradual,
                    Source = propers?.Source ?? info.Source,
                });
            }
            else
            {
                int p = CalendarMath.GetProper(s);
                string slot = $"proper_{p}";
                var info = _data.GetSlotInfo(slot, Series, lectionary);
                if (info is null) continue;
                if (!includeMinor && info.Minor) continue;

                events.Add(new LiturgicalEvent
                {
                    Date = s,
                    Slot = slot,
                    Name = PentecostOrdinalName(s),
                    Season = info.Season,
                    Color = info.Color,
                    Series = Series,
                    IsSunday = true,
                    IsFeast = false,
                    Readings = info.Readings,
                    Proper = p,
                    Ordinal = nAfter,
                    Minor = false,
                });
            }

            // Possibly overlay Reformation / All Saints
            if (includeMinor)
            {
                if (s == ReformationObserved)
                {
                    var rInfo = _data.GetSlotInfo("reformation", Series, lectionary);
                    if (rInfo is not null)
                    {
                        events[^1].AltName = rInfo.Name;
                        events[^1].AltReadings = rInfo.Readings;
                    }
                }
                if (s == AllSaintsObserved)
                {
                    var aInfo = _data.GetSlotInfo("all_saints", Series, lectionary);
                    if (aInfo is not null)
                    {
                        events[^1].AltName = aInfo.Name;
                        events[^1].AltReadings = aInfo.Readings;
                    }
                }
            }
        }

        Add(LastSunday, "last_sunday");

        // Thanksgiving (Thursday)
        Add(Thanksgiving, "thanksgiving");

        // OrderBy is stable (List.Sort is not), matching Python's sort.
        return events.OrderBy(e => e.Date).ToList();
    }

    /// <summary>
    /// Liturgical info for any date, or null if outside the valid range.
    /// Sundays/feasts return that day's info; weekdays return the governing
    /// Sunday's info with IsWeekday = true, plus MinorFeast if a sanctoral
    /// observance falls on that calendar date.
    /// </summary>
    public LookupResult? Lookup(DateOnly d, Lectionary lectionary = Lectionary.ThreeYear)
    {
        int ay = d >= CalendarMath.Advent1ForYear(d.Year) ? d.Year : d.Year - 1;
        if (d < new DateOnly(MinAdventYear, 11, 27) || d > new DateOnly(MaxAdventYear + 1, 11, 26))
            return null;
        var cal = ay != AdventYear ? new LiturgicalCalendar(ay, _data) : this;

        // Sanctoral feast on this exact calendar date (by month/day)
        var minorFeast = _data.SanctoralFeastForDate(d.Month, d.Day);

        string? slot = cal.DateToSlot(d, lectionary);

        if (slot is not null)
        {
            var info = _data.GetSlotInfo(slot, cal.Series, lectionary);
            if (info is null) return null;

            // Human-readable ordinal name for season-after-Pentecost Sundays
            if (slot.StartsWith("proper_", StringComparison.Ordinal) && cal._pentecostSundays.Contains(d))
            {
                info.Name = cal.PentecostOrdinalName(d);
            }
            else if (slot.StartsWith("trinity_", StringComparison.Ordinal) && cal._pentecostSundays.Contains(d))
            {
                info.Name = cal.TrinityOrdinalName(d);
                info.Season = "Trinity";
            }

            // Merge one-year propers for any slot when using one-year
            if (lectionary == Lectionary.OneYear && string.IsNullOrEmpty(info.Collect))
            {
                if (_data.OneYearPropers.TryGetValue(slot, out var propers))
                {
                    info.Collect = propers.Collect;
                    info.Introit = propers.Introit;
                    info.Gradual = propers.Gradual;
                    info.Source = propers.Source;
                }
            }

            // If the slot IS a sanctoral feast, don't double-report it as minor_feast
            if (minorFeast is not null && minorFeast.Info.Name == info.Name)
                minorFeast = null;

            return new LookupResult
            {
                Date = d,
                Slot = slot,
                ChurchYear = $"{ay}–{ay + 1}",
                Series = cal.Series,
                IsWeekday = false,
                MinorFeast = minorFeast,
                Info = info,
            };
        }

        // Weekday with no direct slot: find governing Sunday
        DateOnly? governingDate = null;
        string? govSlot = null;
        SlotInfo? govInfo = null;
        for (int delta = 1; delta <= 7; delta++)
        {
            var prev = d.AddDays(-delta);
            if (prev < cal.Advent1) break;
            string? prevSlot = cal.DateToSlot(prev, lectionary);
            if (prevSlot is not null)
            {
                govInfo = _data.GetSlotInfo(prevSlot, cal.Series, lectionary);
                if (govInfo is not null)
                {
                    governingDate = prev;
                    govSlot = prevSlot;
                }
                break;
            }
        }

        if (governingDate is null || govSlot is null || govInfo is null)
            return null;

        var govDate = governingDate.Value;
        if (govSlot.StartsWith("proper_", StringComparison.Ordinal) && cal._pentecostSundays.Contains(govDate))
        {
            govInfo.Name = cal.PentecostOrdinalName(govDate);
        }
        else if (govSlot.StartsWith("trinity_", StringComparison.Ordinal) && cal._pentecostSundays.Contains(govDate))
        {
            govInfo.Name = cal.TrinityOrdinalName(govDate);
            govInfo.Season = "Trinity";
        }

        if (lectionary == Lectionary.OneYear && string.IsNullOrEmpty(govInfo.Collect))
        {
            if (_data.OneYearPropers.TryGetValue(govSlot, out var propers))
            {
                govInfo.Collect = propers.Collect;
                govInfo.Introit = propers.Introit;
                govInfo.Gradual = propers.Gradual;
                govInfo.Source = propers.Source;
            }
        }

        return new LookupResult
        {
            Date = d,
            Slot = govSlot,
            ChurchYear = $"{ay}–{ay + 1}",
            Series = cal.Series,
            IsWeekday = true,
            GoverningDate = govDate,
            MinorFeast = minorFeast,
            Info = govInfo,
        };
    }

    // ------------------------------------------------------------------
    // Names / file labels
    // ------------------------------------------------------------------

    private string PentecostOrdinalName(DateOnly d)
    {
        int n = (d.DayNumber - HolyTrinity.DayNumber) / 7 + 1;
        return $"{Utils.Ordinal(n)} Sunday after Pentecost";
    }

    private string TrinityOrdinalName(DateOnly d)
    {
        int n = (d.DayNumber - HolyTrinity.DayNumber) / 7; // 1st Sunday after Trinity = 7 days after Holy Trinity
        return $"{Utils.Ordinal(n)} Sunday after Trinity";
    }

    /// <summary>
    /// Filename-safe label, e.g. "2026-06-07 Second Sunday after Pentecost"
    /// (three-year) or "2026-06-07 Second Sunday after Trinity" (one-year).
    /// </summary>
    public string FileLabel(DateOnly d, Lectionary lectionary = Lectionary.ThreeYear)
    {
        string? slot = DateToSlot(d, lectionary);
        string dateText = d.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
        if (slot is null)
            return dateText;

        string name;
        if (slot.StartsWith("proper_", StringComparison.Ordinal) && _pentecostSundays.Contains(d))
        {
            name = PentecostOrdinalName(d);
        }
        else if (slot.StartsWith("trinity_", StringComparison.Ordinal) && _pentecostSundays.Contains(d))
        {
            name = TrinityOrdinalName(d);
        }
        else
        {
            var info = _data.GetSlotInfo(slot, Series, lectionary);
            name = info?.Name ?? slot;
        }
        return Utils.SafeFilename($"{dateText} {name}");
    }
}
