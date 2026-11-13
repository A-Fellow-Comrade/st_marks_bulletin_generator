using System.Text.RegularExpressions;

namespace st_mark_bulletin_generator.Models;

// The wording that is the same on every bulletin and belongs to the church, not
// to one Sunday. Every value starts as the wording the bulletin has always used,
// so a church that never opens Settings gets exactly the same bulletin.
//
// A setting that is missing from the file uses the standard wording below.
// A setting that is saved blank prints nothing.
public class ChurchSettings
{
    public int Version { get; set; } = 1;

    // ── front cover ──
    public string ChurchName { get; set; } = "St. Mark Evangelical-Lutheran Church";
    public string ChurchTown { get; set; } = "of West Henrietta, New York";
    public int FoundedYear { get; set; } = 1901;
    // {year} becomes the number of the year, like 125th (the church's age this year).
    public string YearLine { get; set; } = "Our {year} year of Proclaiming the Gospel";
    public string Synod { get; set; } = "Lutheran Church-Missouri Synod";
    public string Website { get; set; } = "saintmarkslutheran.org";

    // ── inside, left page ──
    public string WelcomeLine { get; set; } = "Welcome to St. Mark Lutheran Church.";
    // {page} and {setting} become the page number and the name of the chosen setting.
    public string OrderOfServiceText { get; set; } =
        "The order for this service is found in the Lutheran Service Book " +
        "(in the hymnal rack) on page {page}, {setting}.";
    public string CommunionHeading { get; set; } = "HOLY COMMUNION";
    public string CommunionParagraph1 { get; set; } =
        "In Holy Communion we receive the true body and blood of our Lord for the " +
        "forgiveness of sins. It is for those who have been baptized. If you do not " +
        "have a church home, your church is not a Missouri Synod Church, or you are " +
        "unsure and would like to commune with us, please see Pastor before the " +
        "service. You may, instead, come forward for a blessing; please cross your " +
        "arms over your chest as you kneel at the rail.";
    public string CommunionParagraph2 { get; set; } =
        "We strongly encourage the use of the common chalice as our Lord instructed. " +
        "See Pastor before the service if a gluten-free wafer is needed.";

    // ── back cover ──
    public string BackWelcomeHeading { get; set; } = "Welcome to St. Mark's";
    // Printed after "Today is the ... Sunday." which the program fills in itself.
    public string BackWelcomeText { get; set; } =
        "Please use the Record of Fellowship books located at the center end of your pew. " +
        "Sign in then pass it on. If this is your first time at St. Mark, also sign our " +
        "guest book in the narthex so we have a record of your visit.";
    public string AnnouncementsHeading { get; set; } = "Item of Interest";
    // Printed under the Acknowledgements, only when there are some.
    public string MorePeopleLine { get; set; } = "~More people are needed to help with these tasks.~";

    // ── the contact box on the back cover (a blank line is left out) ──
    public string ContactPhone { get; set; } = "Church office: Phone 585-334-4795";
    public string ContactEmail { get; set; } = "Office email: saintmarksoffice@icloud.com";
    public string OfficeHours { get; set; } = "OFFICE HOURS ARE BY APPOINTMENT";
    public string ContactElder { get; set; } = "Feel free to contact John Butterazzi, elder at 585-615-3051";
    public string ContactDeaconess { get; set; } = "or Katie Stanton our deaconess at 585-334-2245";
    public string PrayerCardNote { get; set; } =
        "Prayer cards may be placed in the prayer box in the narthex or given to the presiding minister prior to the service.";


    // 1st, 2nd, 3rd, 4th ... 11th, 12th, 13th ... 21st, 101st, 111th
    public static string OrdinalSuffix(int n)
    {
        int lastTwo = Math.Abs(n) % 100;
        if (lastTwo is >= 11 and <= 13) return "th";
        return (Math.Abs(n) % 10) switch { 1 => "st", 2 => "nd", 3 => "rd", _ => "th" };
    }

    // Splits the year line around {year}, e.g. ("Our ", "125", "th", " year of ..."),
    // or null if the line has no {year} (then it is printed as it is).
    public (string Before, string Number, string Suffix, string After)? YearLineParts(int year)
    {
        int at = YearLine.IndexOf("{year}", StringComparison.OrdinalIgnoreCase);
        if (at < 0) return null;

        return (YearLine[..at], year.ToString(System.Globalization.CultureInfo.InvariantCulture),
                OrdinalSuffix(year), YearLine[(at + "{year}".Length)..]);
    }

    // The order-of-service sentence with the page number and setting name put in.
    public string OrderOfServiceFor(string page, string setting)
    {
        var text = Regex.Replace(OrderOfServiceText, @"\{page\}", _ => page, RegexOptions.IgnoreCase);
        return Regex.Replace(text, @"\{setting\}", _ => setting, RegexOptions.IgnoreCase);
    }
}