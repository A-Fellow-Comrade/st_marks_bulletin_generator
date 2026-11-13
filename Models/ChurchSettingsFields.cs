using System.Globalization;

namespace st_mark_bulletin_generator.Models;

// One editable setting as the Settings window shows it.
public sealed class SettingField
{
    public required string Key { get; init; }       // the ChurchSettings property it edits
    public required string Section { get; init; }   // the card it sits in
    public required string Label { get; init; }
    public string? Hint { get; init; }              // small note under the label
    public int Rows { get; init; } = 1;             // 1 = one line, more = a taller box
    public required Func<ChurchSettings, string> Get { get; init; }
    // Puts the typed text into the settings. Returns a message if the text isn't acceptable.
    public required Func<ChurchSettings, string, string?> Set { get; init; }
}


// The one list of everything the Settings window can edit, in the order shown.
public static class ChurchSettingsFields
{
    // To make a setting read-only (shown, but not editable), add its Key here,
    // for example: "ChurchName", "Website". To lock the whole window, see
    // SettingsWindow.AllReadOnly.
    public static readonly HashSet<string> ReadOnlyKeys = new()
    {
    };

    public static bool IsReadOnly(SettingField field) => ReadOnlyKeys.Contains(field.Key);


    private static SettingField Text(string key, string section, string label, string? hint, int rows,
        Func<ChurchSettings, string> get, Action<ChurchSettings, string> set) => new()
    {
        Key = key, Section = section, Label = label, Hint = hint, Rows = rows,
        Get = get,
        Set = (s, v) => { set(s, v); return null; },
    };


    public static readonly IReadOnlyList<SettingField> All = new[]
    {
        // ── front cover ──
        Text("ChurchName", "FRONT COVER", "Church name", null, 1,
            s => s.ChurchName, (s, v) => s.ChurchName = v),
        Text("ChurchTown", "FRONT COVER", "Town line", null, 1,
            s => s.ChurchTown, (s, v) => s.ChurchTown = v),
        Text("YearLine", "FRONT COVER", "Years of the church", "{year} is replaced by the church's age with its ending, like 125th.", 1,
            s => s.YearLine, (s, v) => s.YearLine = v),
        new SettingField
        {
            Key = "FoundedYear", Section = "FRONT COVER", Label = "Year founded",
            Hint = "Used to work out the church's age for the line above.",
            Get = s => s.FoundedYear.ToString(CultureInfo.InvariantCulture),
            Set = (s, v) =>
            {
                if (!int.TryParse(v.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out var year)
                    || year < 1000 || year > DateTime.Now.Year)
                    return "Year founded must be a year such as 1901.";
                s.FoundedYear = year;
                return null;
            },
        },
        Text("Synod", "FRONT COVER", "Synod", null, 1,
            s => s.Synod, (s, v) => s.Synod = v),
        Text("Website", "FRONT COVER", "Website", null, 1,
            s => s.Website, (s, v) => s.Website = v),

        // ── inside, left page ──
        Text("WelcomeLine", "INSIDE LEFT PAGE", "Welcome line", null, 1,
            s => s.WelcomeLine, (s, v) => s.WelcomeLine = v),
        Text("OrderOfServiceText", "INSIDE LEFT PAGE", "Order of service", "{page} and {setting} are replaced by the page number and setting chosen on each bulletin.", 3,
            s => s.OrderOfServiceText, (s, v) => s.OrderOfServiceText = v),
        Text("CommunionHeading", "INSIDE LEFT PAGE", "Holy Communion heading", null, 1,
            s => s.CommunionHeading, (s, v) => s.CommunionHeading = v),
        Text("CommunionParagraph1", "INSIDE LEFT PAGE", "Holy Communion, first paragraph", null, 6,
            s => s.CommunionParagraph1, (s, v) => s.CommunionParagraph1 = v),
        Text("CommunionParagraph2", "INSIDE LEFT PAGE", "Holy Communion, second paragraph", null, 3,
            s => s.CommunionParagraph2, (s, v) => s.CommunionParagraph2 = v),

        // ── back cover ──
        Text("BackWelcomeHeading", "BACK COVER", "Welcome heading", null, 1,
            s => s.BackWelcomeHeading, (s, v) => s.BackWelcomeHeading = v),
        Text("BackWelcomeText", "BACK COVER", "Welcome text", "Printed after \u201CToday is the \u2026 Sunday.\u201D, which the program fills in for you.", 4,
            s => s.BackWelcomeText, (s, v) => s.BackWelcomeText = v),
        Text("AnnouncementsHeading", "BACK COVER", "Announcements heading", null, 1,
            s => s.AnnouncementsHeading, (s, v) => s.AnnouncementsHeading = v),
        Text("MorePeopleLine", "BACK COVER", "Line under the acknowledgements", "Printed only when there are acknowledgements.", 1,
            s => s.MorePeopleLine, (s, v) => s.MorePeopleLine = v),

        // ── contact box ──
        Text("ContactPhone", "CONTACT BOX", "Phone line", "Leave a line empty to leave it out. If every line is empty, the box disappears.", 1,
            s => s.ContactPhone, (s, v) => s.ContactPhone = v),
        Text("ContactEmail", "CONTACT BOX", "Email line", null, 1,
            s => s.ContactEmail, (s, v) => s.ContactEmail = v),
        Text("OfficeHours", "CONTACT BOX", "Office hours line", null, 1,
            s => s.OfficeHours, (s, v) => s.OfficeHours = v),
        Text("ContactElder", "CONTACT BOX", "Elder line", null, 1,
            s => s.ContactElder, (s, v) => s.ContactElder = v),
        Text("ContactDeaconess", "CONTACT BOX", "Deaconess line", null, 1,
            s => s.ContactDeaconess, (s, v) => s.ContactDeaconess = v),
        Text("PrayerCardNote", "CONTACT BOX", "Prayer card note", null, 3,
            s => s.PrayerCardNote, (s, v) => s.PrayerCardNote = v),
    };
}