using Avalonia;
using Avalonia.Media;
using st_mark_bulletin_generator.Liturgical;

namespace st_mark_bulletin_generator.Views;

// The brush for a color of the church year, in the current light or dark theme.
// The four colors are defined in App.axaml (SeasonWhiteBrush and so on) and swap with the theme.
// Code that sets one of these brushes by hand must set it again when the theme changes.
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