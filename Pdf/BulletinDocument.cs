using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using st_mark_bulletin_generator.Data;
using st_mark_bulletin_generator.Models;
using LiturgyTools;
using System;

namespace st_mark_bulletin_generator.Pdf;

public class BulletinDocument : IDocument
{
    private const string FontName = "Lato";
    private static readonly string CoverImage =
        Path.Combine(AppContext.BaseDirectory, "imgs", "cover.jpg");

    private readonly Bulletin _b;
    private readonly HymnRepository _hymns;

    public BulletinDocument(Bulletin bulletin, HymnRepository hymns)
    {
        _b = bulletin;
        _hymns = hymns;
    }

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    // Each PDF page is a landscape Letter sheet holding two 5.5" x 8.5" panels.
    private static IContainer Panel(IContainer c) => c.Padding(0.5f, Unit.Inch);

    // One hymn line: bold label + number, then the title from the database.
    private void HymnRow(ColumnDescriptor col, string label, int? number)
    {
        col.Item().Text(t =>
        {
            t.Span(number is int n ? $"{label} #{n} " : $"{label} ").Bold();
            t.Span(number is int m
                ? _hymns.GetByNumber(m)?.Title ?? "(title not found)"
                : "");
        });
    }

    public void Compose(IDocumentContainer container)
    {
        var announcements = _b.Announcements.Split('\n',
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

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

                    col.Item().PaddingTop(4).Text("Welcome to St. Mark Lutheran Church.").AlignCenter();
                    col.Item().Text(
                        "The order for this service is found in the Lutheran Service Book " +
                        $"(in the hymnal rack) on page {_b.SettingPage}, {_b.SettingName}.")
                        .AlignCenter();

                    col.Item().Height(10);

                    col.Item().Text(t =>
                    {
                        t.Span("ORGAN PRELUDE ").Bold();
                        t.Span(_b.Prelude);
                    });

                    HymnRow(col, "HYMN", _b.OpeningHymn);

                    col.Item().Text(t =>
                    {
                        t.Span($"{_b.Creed.ToUpperInvariant()} ").Bold();
                        t.Span($"LSB {_b.CreedPages}");
                    });

                    HymnRow(col, "HYMN OF THE DAY", _b.HymnOfTheDay);
                    HymnRow(col, "CLOSING HYMN", _b.ClosingHymn);

                    col.Item().Text(t =>
                    {
                        t.Span("ORGAN POSTLUDE ").Bold();
                        t.Span(_b.Postlude);
                    });

                    col.Item().Height(10);

                    col.Item().PaddingTop(8).Text("HOLY COMMUNION").AlignCenter().Bold();
                    col.Item().PaddingLeft(6).Text(
                        "In Holy Communion we receive the true body and blood of our Lord for the " +
                        "forgiveness of sins. It is for those who have been baptized. If you do not " +
                        "have a church home, your church is not a Missouri Synod Church, or you are " +
                        "unsure and would like to commune with us, please see Pastor before the " +
                        "service. You may, instead, come forward for a blessing; please cross your " +
                        "arms over your chest as you kneel at the rail.").AlignCenter();
                    col.Item().PaddingLeft(6).Text(
                        "We strongly encourage the use of the common chalice as our Lord instructed. " +
                        "See Pastor before the service if a gluten-free wafer is needed.").AlignCenter();
                });

                // ---- RIGHT: Psalm
                row.RelativeItem().Element(Panel).Column(col =>
                {
                    //TONY TODO: This gets the nearest sunday. Integrate with calendar UI.
                    int dayOfWeek = (int)DateTime.Today.DayOfWeek;
                    int daysToAdjust = dayOfWeek <= 3 ? -dayOfWeek : (7 - dayOfWeek);
                    DateTime nearestSunday = DateTime.Today.Date.AddDays(daysToAdjust);

                    //get the psalm reference
                    LiturgicalData myData = LiturgicalData.Load();
                    var calendar = new LiturgicalCalendar(2026, myData);
                    var result = calendar.Lookup(DateOnly.FromDateTime(nearestSunday));

                    var resultParsed = PsalmParser.ParseReading(result.Readings.Ps);


                    //NOTE: TONY TODO: UI ADD OPTION TO GET 
                    //OPTIONAL VERSES: resultParsed.OptionalVerses
                    //Also add option to change which verses are fetched in UI
                    var psalm = PsalmParser.GetPsalm(
                        resultParsed.Book,
                        int.Parse(resultParsed.Chapter),
                        resultParsed.RequiredVerses,
                        "ESV"
                    );

                    //its the whole chapter, omit this in title
                    string psalm_title;
                    if(resultParsed.RequiredVerses == "none")
                    {
                        psalm_title = resultParsed.Book + " " + resultParsed.Chapter;
                    } else
                    {
                        psalm_title = resultParsed.Book + " " + resultParsed.Chapter + " " + resultParsed.RequiredVerses;
                    }

                    col.Spacing(1.5f);
                    col.Item().PaddingBottom(4).Text(psalm_title).FontSize(12).Bold().AlignCenter();

                    int verseIndex = 0;

                    foreach (var (v, lines) in psalm)
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
                row.RelativeItem().Element(Panel).Column(col =>
                {
                    col.Spacing(4);

                    col.Item().AlignCenter().Text("Welcome to St. Mark's").Italic();
                    col.Item().Text(
                        "Today is the nineteenth Sunday after Pentecost. Please use the Record of " +
                        "Fellowship books located at the center end of your pew. Sign in then pass it " +
                        "on. If this is your first time at St. Mark, also sign our guest book in the " +
                        "narthex so we have a record of your visit.");

                    // Announcements from the form, one bullet per line.
                    if (announcements.Length > 0)
                    {
                        col.Item().PaddingTop(2).AlignCenter().Text("Item of Interest").Italic();
                        foreach (var a in announcements)
                            col.Item().Text($"* {a}");
                    }

                    // Contact box
                    col.Item().PaddingTop(16).Border(1).BorderColor(Colors.Black).Padding(4).Column(b =>
                    {
                        b.Spacing(1);
                        b.Item().AlignCenter().Text("Church office: Phone 585-334-4795").FontSize(9.5f);
                        b.Item().AlignCenter().Text("Office email: saintmarksoffice@icloud.com").FontSize(9.5f);
                        b.Item().AlignCenter().Text("OFFICE HOURS ARE BY APPOINTMENT").Italic().FontSize(9.5f);
                        b.Item().AlignCenter().Text("Feel free to contact John Butterazzi, elder at 585-615-3051").FontSize(9.5f);
                        b.Item().AlignCenter().Text("or Katie Stanton our deaconess at 585-334-2245").FontSize(9.5f);
                        b.Item().AlignCenter().Text("Prayer cards may be placed in the prayer box in the narthex or given to the presiding minister prior to the service.")
                            .Italic().FontSize(9).FontColor(Colors.Grey.Darken3);
                    });

                    // Thank-yous
                    col.Item().PaddingTop(40).AlignCenter().Text("Thank you Kathryn Dorr for preparing the altar this morning.").AlignCenter();
                    col.Item().AlignCenter().Text("Thank you Roger and Denise for setting up refreshments following the service this morning.").AlignCenter();

                    col.Item().PaddingTop(12).AlignCenter().Text("~More people are needed to help with these tasks.~").Italic();
                });

                // ---- RIGHT: Front cover ----
                row.RelativeItem().Element(Panel).Column(col =>
                {
                    col.Item().PaddingTop(12).AlignCenter().Text("St. Mark Evangelical-Lutheran Church").FontSize(15).AlignCenter();
                    col.Item().AlignCenter().Text("of West Henrietta, New York").FontSize(15);

                    col.Item().PaddingTop(4).AlignCenter().Text(t =>
                    {
                        t.DefaultTextStyle(s => s.Italic().FontSize(10));
                        t.Span("Our 125");
                        t.Span("th").Superscript();
                        t.Span(" year of Proclaiming the Gospel");
                    });

                    col.Item().PaddingTop(14).AlignCenter().Height(3.2f, Unit.Inch)
                       .Image(CoverImage).FitArea();

                    col.Item().PaddingTop(30).AlignCenter().Text("DIVINE SERVICE").FontSize(16);
                    col.Item().AlignCenter().Text("October 4, T 2026 at 10 a.m.").FontSize(11);

                    col.Item().PaddingTop(14).AlignCenter().Text("Lutheran Church-Missouri Synod").FontSize(11);
                    col.Item().AlignCenter().Text("saintmarkslutheran.org").FontSize(11);
                });
            });
        });
    }
}