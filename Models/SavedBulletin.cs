namespace st_mark_bulletin_generator.Models;

//one sunday's form as it is stored on disk
//Everything is in plain text, so it can be edited by hand exactly how it was left
//Setting and creed Page numbers are not stored, lookups are done instead
public class SavedBulletin
{
    public int Version { get; set; } = 1;
    public string Date { get; set; } = ""; //"yyyy-MM-dd"
    public string ServiceTime { get; set; } = ""; //"10 a.m."
    public string SundayName { get; set; } = ""; //"the nineteenth Sunday after Pentecost"
    public string Minister { get; set; } = "";
    public string Organist { get; set; } = "";
    public string SettingName { get; set; } = ""; //"Setting Three"
    public string Prelude { get; set; } = "";
    public int OpeningHymn { get; set; } = 0;
    public string OpeningTitle { get; set; } = ""; 
    public string Creed { get; set; } = ""; //"Nicene Creed"
    public string DayHymn { get; set; } = "";
    public string DayTitle { get; set; } = "";
    public int ClosingHymn { get; set; } = 0;
    public string ClosingTitle { get; set; } = "";
    public string Postlude { get; set; } = "";
    public string Announcements { get; set; } = "";
    public string SavedAt { get; set; } = ""; //"yyyy-MM-dd HH:mm:ss"
    public string SavedBy { get; set; } = ""; //computer name
}