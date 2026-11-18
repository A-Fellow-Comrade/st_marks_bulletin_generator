using Avalonia;
using Avalonia.Media;
using st_mark_bulletin_generator.Liturgical;

namespace st_mark_bulletin_generator.Views;

// Shared church-year brushes are defined in App.axaml and stay consistent across themes.
internal static class SeasonBrushes
{
    public static string Key(LiturgicalColor color) => color switch
    {
        LiturgicalColor.Green => "SeasonGreenBrush",
        LiturgicalColor.Violet => "SeasonVioletBrush",
        LiturgicalColor.Red => "SeasonRedBrush",
        _ => "SeasonWhiteBrush",
    };

    public static IBrush For(LiturgicalColor color) =>
        (IBrush)Application.Current!.Resources[Key(color)]!;
}