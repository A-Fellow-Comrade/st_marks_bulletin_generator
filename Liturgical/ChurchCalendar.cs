namespace st_mark_bulletin_generator.Liturgical;
/**
    finds the church-year name of a Sunday by the date.
    Using date of Easter and Christmas as reference
    Name is returned as a phrase that completes the "Today is the ___"
*/
public static class ChurchCalendar
{
    private static readonly string[] Numbers =
    {
        "", "first", "second", "third", "fourth", "fifth", "sixth", "seventh",
        "eighth", "ninth", "tenth", "eleventh", "twelfth", "thirteenth",
        "fourteenth", "fifteenth", "sixteenth", "seventeenth", "eighteenth",
        "nineteenth", "twentieth", "twenty-first", "twenty-second", "twenty-third",
        "twenty-fourth", "twenty-fifth", "twenty-sixth", "twenty-seventh",
        "twenty-eighth", "twenty-ninth", "thirtieth",
    };

    private static string Num(int n) => n>= 1 && n < Numbers.Length ? Numbers[n]: $"{n}th";

    //easter sunday 
        public static DateOnly Easter(int year)
    {
        int a = year % 19;
        int b = year / 100;
        int c = year % 100;
        int d = b / 4;
        int e = b % 4;
        int f = (b + 8) / 25;
        int g = (b - f + 1) / 3;
        int h = (19 * a + b - d - g + 15) % 30;
        int i = c / 4;
        int k = c % 4;
        int l = (32 + 2 * e + 2 * i - h - k) % 7;
        int m = (a + 11 * h + 22 * l) / 451;
        int month = (h + l - 7 * m + 114) / 31;
        int day = ((h + l - 7 * m + 114) % 31) + 1;
        return new DateOnly(year, month, day);
    }

    //The sunday that is between Nov 27 and Dec 3, first Sunday of Advent
    public static DateOnly FirstSundayofAdvent(int year)
    {
        var dec24 = new DateOnly(year, 12, 24);
        var fourthAdvent = dec24.AddDays(-(int)dec24.DayOfWeek); //sunday before/on Dec 24
        return fourthAdvent.AddDays(-21);
    }

    //the first sunday on or after the given date
    public static DateOnly NextSunday(DateOnly from) => from.AddDays((7 - (int)from.DayOfWeek) % 7);

    //closest sunday to the given date, mon-wed go back, thu-sat go forwrd
    public static DateOnly NearestSunday(DateOnly date)
    {
        int DayOfWeek = (int)date.DayOfWeek;
        return DayOfWeek <= 3 ? date.AddDays(-DayOfWeek) : date.AddDays(7 - DayOfWeek);
    }

    //returns "" if the date is not a Sunday
    public static string SundayName(DateOnly date)
    {
        if (date.DayOfWeek != DayOfWeek.Sunday) return "";

        int year = date.Year;
        //festivals that land on this Sunday
        //keep reformation and all saints on a nearby sunday
        if (date.Month == 12 && date.Day == 25) return "the Nativity of Our Lord";
        if (date.Month == 1 && date.Day == 6) return "the Ephiphany of Our Lord";
        if (date.Month == 10 && date.Day == 31) return "Reformation Day";
        if (date.Month == 11 && date.Day == 1) return "All Saints' Day";

        //sundays after christmas
        if ((date.Month == 12 && date.Day >= 25) || (date.Month == 1 && date.Day <= 5))
        {
            var christmas = new DateOnly(date.Month == 12 ? year : year - 1, 12, 25);
            int n = (date.DayNumber - christmas.DayNumber - 1) / 7 + 1;
            return $"the {Num(n)} Sunday after Christmas";
        }

        //Easter, pentecost, advent1
        var easter = Easter(year);
        var pentecost = easter.AddDays(49);
        var advent1 = FirstSundayofAdvent(year);

        //advent
        if (date >= advent1)
        {
            int n = (date.DayNumber - advent1.DayNumber) / 7 + 1;
            return $"the {Num(n)} Sunday of Advent";
        }


        //Ephiphany season; sundays after 1/6 ending with the Transfiguration
        //sunday before Ash Wednesday
        var ephiphany = new DateOnly(year, 1, 6);
        var transfiguration = easter.AddDays(-49);
        if (date > ephiphany && date < transfiguration)
        {
            if (date == transfiguration) return "the Transfiguration of Our Lord";
            var firstAfter = ephiphany.AddDays(7 - (int)ephiphany.DayOfWeek); //first sunday after 1/6
            int n = (date.DayNumber - firstAfter.DayNumber) / 7 + 1;
            return n == 1 ? "The Baptism of Our Lord" : $"the {Num(n)} Sunday after the Ephiphany";
        }

        // Lent : five sundays then palm sunday
        if (date > transfiguration && date < easter)
        {
            int n = 7 - (easter.DayNumber - date.DayNumber) / 7;
            return n == 6 ? "Palm Sunday" : $"the {Num(n)} Sunday in Lent";
        }

        if (date == easter) return "the Ressurection of Our Lord";

        //easter season, with easter being the first sunday of Easter
        if (date > easter && date < pentecost)
        {
            int k = (date.DayNumber - easter.DayNumber) / 7;
            return $"the {Num(k+1)} Sunday of Easter";
        }

        if (date == pentecost) return "the Day of Pentecost";

        //last three sundays of the church year (sundays before advent)
        if (date == advent1.AddDays(-7)) return "the last Sunday of the Church Year";
        if(date == advent1.AddDays(-14)) return "the second-to-last Sunday of the Church Year";
        if(date == advent1.AddDays(-21)) return "the third-to-last Sunday of the Church Year";

        //sundays after pentecost. Holy Trinity is first/
        if (date > pentecost)
        {
            int k = (date.DayNumber - pentecost.DayNumber) / 7;
            return k == 1 ? "Holy Trinity Sunday" : $"the {Num(k)} Sunday after Pentecost";
        
        }
        return "";
    }
}
