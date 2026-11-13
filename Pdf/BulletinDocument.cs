using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using st_mark_bulletin_generator.Data;
using st_mark_bulletin_generator.Models;
using LiturgyTools;
using System;
using HTMLQuestPDF.Extensions;
using System.Text.RegularExpressions;

namespace st_mark_bulletin_generator.Pdf;

public class BulletinDocument : IDocument
{
    private const string FontName = "Lato";
    private static string RandomCoverImage =>
        Path.Combine(AppContext.BaseDirectory, "imgs", $"{Random.Shared.Next(1, 7)}.jpg");

    // Returns the bulletin's chosen image, or a random one if none was chosen.
    private string CoverImage =>
        (!string.IsNullOrEmpty(_b.CoverImagePath) && File.Exists(_b.CoverImagePath))
            ? _b.CoverImagePath
            : RandomCoverImage;

    private readonly Bulletin _b;
    private readonly HymnRepository _hymns;
    private readonly ChurchSettings _church;

    // `church` is the wording that is the same every week (see ChurchSettings).
    // Without it, the standard wording is used.
    public BulletinDocument(Bulletin bulletin, HymnRepository hymns, ChurchSettings? church = null)
    {
        _b = bulletin;
        _hymns = hymns;
        _church = church ?? new ChurchSettings();
    }

    // A setting that was saved blank is left off the page.
    private static bool Has(string? text) => !string.IsNullOrWhiteSpace(text);

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    // Front cover line, e.g. "October 18, 2026 at 10 a.m."
    private string DateLine()
    {
        if (_b.Date is not DateOnly d) return "";
        var text = d.ToString("MMMM d, yyyy", System.Globalization.CultureInfo.InvariantCulture);
        var time = _b.ServiceTime.Trim();
        return time.Length > 0 ? $"{text} at {time}" : text;
    }

    // Each PDF page is a landscape Letter sheet holding two 5.5" x 8.5" panels.
    private static IContainer Panel(IContainer c) => c.Padding(0.5f, Unit.Inch);

    // One hymn line: bold label + number, then the title shown in the form, or
    //if blank, then the database
    private void HymnRow(ColumnDescriptor col, string label, int? number, string title)
    {
        col.Item().Text(t =>
        {
            t.Span(number is int n ? $"{label} #{n} " : $"{label} ").Bold();
            t.Span(number is int m
                ? (!string.IsNullOrWhiteSpace(title)
                ? title:
                _hymns.GetByNumber(m)?.Title ?? "(title not found)")
                : "");
        });
    }

    public void Compose(IDocumentContainer container)
    {
        var announcementsHtml = _b.Announcements ?? "";
        var acknowledgementsHtml = _b.Acknowledgements ?? "";

        // "Today is the nineteenth Sunday after Pentecost. " (left out if the name box is empty)
        var sundayName = _b.SundayName.Trim().TrimEnd('.');
        var todayIs = sundayName.Length > 0 ? $"Today is {sundayName}. " : "";

        // ───────────────────────── PAGE 1 : INSIDE SPREAD ─────────────────────────
        container.Page(page =>
        {
            page.Size(PageSizes.Letter.Landscape());
            page.Margin(0);
            page.DefaultTextStyle(t => t.FontFamily(FontName).FontSize(10.5f));

            page.Content().Row(row =>
            {
                // ---- LEFT: Order of service ----
                row.RelativeItem().Element(Panel).Column(col =>
                {
                    col.Spacing(5);

                    col.Item().Text("DIVINE SERVICE").AlignCenter().FontSize(14).Bold();
                    if (!string.IsNullOrWhiteSpace(_b.Minister))
                        col.Item().Text($"{_b.Minister}, presiding minister").AlignCenter();
                    if (!string.IsNullOrWhiteSpace(_b.Organist))
                        col.Item().Text($"{_b.Organist}, organist").AlignCenter();

                    col.Item().LineHorizontal(1);

                    if (Has(_church.WelcomeLine))
                        col.Item().PaddingTop(4).Text(_church.WelcomeLine).AlignCenter();
                    if (Has(_church.OrderOfServiceText))
                        col.Item().Text(_church.OrderOfServiceFor(_b.SettingPage?.ToString() ?? "", _b.SettingName))
                            .AlignCenter();

                    col.Item().Height(10);

                    col.Item().Text(t =>
                    {
                        t.Span("ORGAN PRELUDE ").Bold();
                        t.Span(_b.Prelude);
                    });

                    HymnRow(col, "HYMN", _b.OpeningHymn, _b.OpeningTitle);

                    col.Item().Text(t =>
                    {
                        t.Span($"{_b.Creed.ToUpperInvariant()} ").Bold();
                        t.Span($"LSB {_b.CreedPages}");
                    });

                    HymnRow(col, "HYMN OF THE DAY", _b.HymnOfTheDay, _b.DayTitle);
                    HymnRow(col, "CLOSING HYMN", _b.ClosingHymn, _b.ClosingTitle);

                    col.Item().Text(t =>
                    {
                        t.Span("ORGAN POSTLUDE ").Bold();
                        t.Span(_b.Postlude);
                    });

                    col.Item().Height(10);

                    if (Has(_church.CommunionHeading))
                        col.Item().PaddingTop(8).Text(_church.CommunionHeading).AlignCenter().Bold();
                    if (Has(_church.CommunionParagraph1))
                        col.Item().PaddingLeft(6).Text(_church.CommunionParagraph1).AlignCenter();
                    if (Has(_church.CommunionParagraph2))
                        col.Item().PaddingLeft(6).Text(_church.CommunionParagraph2).AlignCenter();
                });

                // ---- RIGHT: Psalm
                // ---- RIGHT: Psalm ----
// The title and verses come from the bulletin form.
row.RelativeItem().Element(Panel).ScaleToFit().Column(col =>
{
    col.Spacing(1.5f);
    col.Item().PaddingBottom(4).Text(_b.PsalmTitle).FontSize(12).Bold().AlignCenter();

                    int verseIndex = 0;

                    foreach (var (v, lines) in _b.Psalm)
                    {
                        col.Item().Row(r =>
                        {
                            if(verseIndex%2 == 0){
                                r.ConstantItem(20).Text(v);
                            } else
                            {
                                r.ConstantItem(20).Text(v).Bold();
                            }
                            r.RelativeItem().Column(c =>
                            {
                                for (int i = 0; i < lines.Length; i++)
                                    if(verseIndex%2 == 0){
                                        c.Item().PaddingLeft(i == 0 ? 0 : 10).Text(lines[i]).AlignCenter();
                                    } else
                                    {
                                        c.Item().PaddingLeft(i == 0 ? 0 : 10).Text(lines[i]).Bold().AlignCenter();
                                    }
                            });
                        });
                        verseIndex++;
                    }

                    // Gloria Patri
                    col.Item().PaddingTop(8).Row(r =>
                    {
                        r.ConstantItem(20).Text("C").Bold();
                        r.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Glory be to the Father and to the Son").Bold().AlignCenter();
                            c.Item().PaddingLeft(10).Text("and to the Holy Spirit;").Bold().AlignCenter();
                            c.Item().PaddingLeft(10).Text("as it was in the beginning,").Bold().AlignCenter();
                            c.Item().PaddingLeft(10).Text("is now, and will be forever. Amen").Bold().AlignCenter();
                        });
                    });
                });
            });
        });

        // ───────────────────────── PAGE 2 : OUTSIDE SPREAD ─────────────────────────
        container.Page(page =>
        {
            page.Size(PageSizes.Letter.Landscape());
            page.Margin(0);
            page.DefaultTextStyle(t => t.FontFamily(FontName).FontSize(10.5f));

            page.Content().Row(row =>
            {
                // ---- LEFT: Back cover ----
                // ScaleToFit: if announcements and thank-yous are long, the type shrinks
                // a little instead of running off the panel.
                row.RelativeItem().Element(Panel).ScaleToFit().Column(col =>
                {
                    col.Spacing(4);

                    if (Has(_church.BackWelcomeHeading))
                        col.Item().AlignCenter().Text(_church.BackWelcomeHeading).Italic();

                    var backWelcome = (todayIs + (_church.BackWelcomeText ?? "")).Trim();
                    if (backWelcome.Length > 0)
                        col.Item().Text(backWelcome);

                    // Announcements from the form, one bullet per line.
                    if (!string.IsNullOrWhiteSpace(announcementsHtml))
                    {
                        if (Has(_church.AnnouncementsHeading))
                            col.Item().PaddingTop(2).AlignCenter().Text(_church.AnnouncementsHeading).Italic();

                        var htmlWithAsterisks = Regex.Replace(
                            announcementsHtml,
                            @"(<p\b[^>]*>)",
                            "$1* ",
                            RegexOptions.IgnoreCase
                        );

                        col.Item().HTML(handler =>
                        {
                            handler.SetContainerStyleForHtmlElement(
                                "p", container => container.PaddingVertical(1)
                            );
                            handler.SetHtml(htmlWithAsterisks);
                        });
                    }

                    // Contact box (a line that is blank in Settings is left out; so is the whole box if all are)
                    var contactLines = new[]
                    {
                        _church.ContactPhone, _church.ContactEmail, _church.OfficeHours,
                        _church.ContactElder, _church.ContactDeaconess, _church.PrayerCardNote,
                    };
                    if (contactLines.Any(Has))
                    {
                        col.Item().PaddingTop(16).Border(1).BorderColor(Colors.Black).Padding(4).Column(b =>
                        {
                            b.Spacing(1);
                            if (Has(_church.ContactPhone))
                                b.Item().AlignCenter().Text(_church.ContactPhone).FontSize(9.5f);
                            if (Has(_church.ContactEmail))
                                b.Item().AlignCenter().Text(_church.ContactEmail).FontSize(9.5f);
                            if (Has(_church.OfficeHours))
                                b.Item().AlignCenter().Text(_church.OfficeHours).Italic().FontSize(9.5f);
                            if (Has(_church.ContactElder))
                                b.Item().AlignCenter().Text(_church.ContactElder).FontSize(9.5f);
                            if (Has(_church.ContactDeaconess))
                                b.Item().AlignCenter().Text(_church.ContactDeaconess).FontSize(9.5f);
                            if (Has(_church.PrayerCardNote))
                                b.Item().AlignCenter().Text(_church.PrayerCardNote)
                                    .Italic().FontSize(9).FontColor(Colors.Grey.Darken3);
                        });
                    }

                    // Thank-yous: the Acknowledgements box from the form, centered.
                    // Nothing prints (and no "more people" line) when the box is empty.
                    if (HtmlText.HasContent(acknowledgementsHtml))
                    {
                        col.Item().PaddingTop(40).HTML(handler =>
                        {
                            handler.SetContainerStyleForHtmlElement(
                                "p", c => c.PaddingVertical(1)
                            );
                            handler.SetHtml(HtmlText.CenterParagraphs(acknowledgementsHtml));
                        });

                        if (Has(_church.MorePeopleLine))
                            col.Item().PaddingTop(12).AlignCenter().Text(_church.MorePeopleLine).Italic();
                    }
                });

                // ---- RIGHT: Front cover ----
                row.RelativeItem().Element(Panel).Column(col =>
                {
                    if (Has(_church.ChurchName))
                        col.Item().PaddingTop(12).AlignCenter().Text(_church.ChurchName).FontSize(15).AlignCenter();
                    if (Has(_church.ChurchTown))
                        col.Item().AlignCenter().Text(_church.ChurchTown).FontSize(15);

                    if (Has(_church.YearLine))
                    {
                        col.Item().PaddingTop(4).AlignCenter().Text(t =>
                        {
                            t.DefaultTextStyle(s => s.Italic().FontSize(10));
                            var parts = _church.YearLineParts(DateTime.Now.Year - _church.FoundedYear);
                            if (parts != null)
                            {
                                t.Span(parts.Value.Before + parts.Value.Number);
                                t.Span(parts.Value.Suffix).Superscript();
                                t.Span(parts.Value.After);
                            }
                            else
                            {
                                t.Span(_church.YearLine);
                            }
                        });
                    }

                    col.Item().PaddingTop(14).AlignCenter().Height(3.2f, Unit.Inch)
                       .Image(CoverImage).FitArea();

                    col.Item().PaddingTop(30).AlignCenter().Text("DIVINE SERVICE").FontSize(16);
                    col.Item().AlignCenter().Text(DateLine()).FontSize(11);

                    if (Has(_church.Synod))
                        col.Item().PaddingTop(14).AlignCenter().Text(_church.Synod).FontSize(11);
                    if (Has(_church.Website))
                        col.Item().AlignCenter().Text(_church.Website).FontSize(11);
                });
            });
        });
    }
}