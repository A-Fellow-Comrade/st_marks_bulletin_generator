namespace st_mark_bulletin_generator.Liturgical;

// The colors of the church year.
public enum LiturgicalColor { White, Green, Violet, Red }

// What kind of day a date is, in the church year: its name and its color.
public readonly record struct ChurchYearDay(string Name, LiturgicalColor Color)
{
    // "white", "green", "violet" or "red", for putting in a sentence.
    public string ColorWord => Color.ToString().ToLowerInvariant();
}

// Works out the season and color for any date from the date of Easter and of Advent.
// It is meant for Sundays (that is what the calendar and the bulletin form use); the
// color of another day is just the color of the season it falls in.
//
// The colors follow the usual Lutheran Service Book practice:
//   Advent violet, Christmas white, Epiphany and Baptism of Our Lord white, the Sundays
//   after Epiphany green, Transfiguration white, Lent violet, Sunday of the Passion red,
//   Easter season white, Pentecost red, Holy Trinity white, the Sundays after Pentecost
//   green, Reformation Day red and All Saints' Day white when they fall on a Sunday, and
//   the Last Sunday of the Church Year white.
//
// Choices a church might do differently are the constants just below.
public static class ChurchYear
{
    // LSB allows blue or violet for Advent.
    public const LiturgicalColor AdventColor = LiturgicalColor.Violet;
    // LSB allows scarlet (red) or violet for the Sunday of the Passion.
    public const LiturgicalColor PalmSundayColor = LiturgicalColor.Red;

    // Easter Sunday of a year (the usual Gregorian computation).
    public static DateOnly Easter(int year)
    {
        int a = year % 19, b = year / 100, c = year % 100;
        int d = b / 4, e = b % 4, f = (b + 8) / 25, g = (b - f + 1) / 3;
        int h = (19 * a + b - d - g + 15) % 30;
        int i = c / 4, k = c % 4;
        int l = (32 + 2 * e + 2 * i - h - k) % 7;
        int m = (a + 11 * h + 22 * l) / 451;
        int month = (h + l - 7 * m + 114) / 31;
        int day = (h + l - 7 * m + 114) % 31 + 1;
        return new DateOnly(year, month, day);
    }

    // The First Sunday in Advent: the Sunday from November 27 to December 3.
    public static DateOnly Advent1(int year)
    {
        var nov27 = new DateOnly(year, 11, 27);
        int toSunday = ((int)DayOfWeek.Sunday - (int)nov27.DayOfWeek + 7) % 7;
        return nov27.AddDays(toSunday);
    }

    public static ChurchYearDay For(DateOnly date)
    {
        // The civil year in which this church year began (its Advent).
        int adventYear = date >= Advent1(date.Year) ? date.Year : date.Year - 1;
        var advent = Advent1(adventYear);
        var nextAdvent = Advent1(adventYear + 1);
        var easter = Easter(adventYear + 1);

        // Advent and Christmas
        if (date < new DateOnly(adventYear, 12, 25))
            return new("Advent", AdventColor);
        if (date <= new DateOnly(adventYear + 1, 1, 5))
            return new("Christmas", LiturgicalColor.White);

        var epiphany = new DateOnly(adventYear + 1, 1, 6);
        var ashWednesday = easter.AddDays(-46);

        // Epiphany season, up to Ash Wednesday
        if (date < ashWednesday)
        {
            if (date == epiphany)
                return new("The Epiphany of Our Lord", LiturgicalColor.White);
            if (date.DayOfWeek == DayOfWeek.Sunday && date == easter.AddDays(-49))
                return new("The Transfiguration of Our Lord", LiturgicalColor.White);
            if (date.DayOfWeek == DayOfWeek.Sunday && date > epiphany && date <= epiphany.AddDays(7))
                return new("The Baptism of Our Lord", LiturgicalColor.White);
            return new("Time after Epiphany", LiturgicalColor.Green);
        }

        // Lent and Holy Week
        var palmSunday = easter.AddDays(-7);
        if (date < palmSunday)
            return new("Lent", LiturgicalColor.Violet);
        if (date == palmSunday)
            return new("Sunday of the Passion", PalmSundayColor);
        if (date < easter)
            return new("Holy Week", LiturgicalColor.Red);

        // Easter season, Pentecost and Holy Trinity
        var pentecost = easter.AddDays(49);
        if (date == easter)
            return new("The Resurrection of Our Lord", LiturgicalColor.White);
        if (date < pentecost)
            return new("Easter", LiturgicalColor.White);
        if (date == pentecost)
            return new("Pentecost", LiturgicalColor.Red);
        if (date == pentecost.AddDays(7))
            return new("The Holy Trinity", LiturgicalColor.White);

        // The Sundays after Pentecost, and the festivals at the end of the church year
        if (date == nextAdvent.AddDays(-7))
            return new("Last Sunday of the Church Year", LiturgicalColor.White);
        if (date.DayOfWeek == DayOfWeek.Sunday && date.Month == 10 && date.Day == 31)
            return new("Reformation Day", LiturgicalColor.Red);
        if (date.DayOfWeek == DayOfWeek.Sunday && date.Month == 11 && date.Day == 1)
            return new("All Saints' Day", LiturgicalColor.White);

        return new("Time after Pentecost", LiturgicalColor.Green);
    }
}