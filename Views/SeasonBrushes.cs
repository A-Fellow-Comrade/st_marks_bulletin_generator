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

    public static IBrush MarkerFor(LiturgicalColor color) =>
        (IBrush)Application.Current!.Resources[Key(color).Replace("Brush", "MarkBrush")]!;

    public static IBrush CalendarFor(LiturgicalColor color) =>
        (IBrush)Application.Current!.Resources[Key(color).Replace("Season", "CalendarSeason")]!;

    public static IBrush CalendarMarkerFor(LiturgicalColor color) =>
        (IBrush)Application.Current!.Resources[
            Key(color).Replace("Season", "CalendarSeason").Replace("Brush", "MarkBrush")]!;

    public static IBrush CalendarForeground() =>
        (IBrush)Application.Current!.Resources["CalendarSeasonForegroundBrush"]!;

    public static IBrush HoverFor(LiturgicalColor color) =>
        (IBrush)Application.Current!.Resources[Key(color).Replace("Brush", "HoverBrush")]!;

    public static IBrush ForegroundFor(LiturgicalColor color) =>
        color == LiturgicalColor.White
            ? (IBrush)Application.Current!.Resources["SeasonDarkTextBrush"]!
            : (IBrush)Application.Current!.Resources["SeasonForegroundBrush"]!;
}