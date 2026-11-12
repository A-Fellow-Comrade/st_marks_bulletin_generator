using LiturgyTools;

namespace st_mark_bulletin_generator.Liturgical;

//three year vs one year choice and how it is saved in the bulletin
public static class LectionaryChoice
{
    public static string ToText(Lectionary lectionary) => lectionary == Lectionary.OneYear ? "OneYear" : "ThreeYear";

    //just to overwrite previous bulletin that i saved before editing the lectionary
    //blank means three year
    public static Lectionary FromText(string? text) => string.Equals(text?.Trim(), "OneYear", StringComparison.OrdinalIgnoreCase) ? Lectionary.OneYear : Lectionary.ThreeYear;

    public static string Describe(Lectionary lectionary) => lectionary == Lectionary.OneYear ? "one-year": "three-year";
}