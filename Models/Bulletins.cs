namespace st_mark_bulletin_generator.Models;

public class Bulletin
{
    public string Minister { get; set; } = "";
    public string Organist { get; set; } = "";
    public string SettingName { get; set; } = "";    // "Setting Three"
    public int? SettingPage { get; set; }            // 184
    public string Prelude { get; set; } = "";
    public int? OpeningHymn { get; set; }
    public string Creed { get; set; } = "";          // "Nicene Creed"
    public string CreedPages { get; set; } = "";     // "158"
    public int? HymnOfTheDay { get; set; }
    public int? ClosingHymn { get; set; }
    public string Postlude { get; set; } = "";
    public string Announcements { get; set; } = "";  // one per line
}