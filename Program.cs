using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using QuestPDF.Companion;

//lectionary import
using LiturgyTools;

// importing hymns / test
using st_mark_bulletin_generator.Data;
var repo = new HymnRepository(Path.Combine(AppContext.BaseDirectory, "hymns.db"));
var added = repo.ImportTsv(Path.Combine(AppContext.BaseDirectory, "Data", "hymns.tsv"));


QuestPDF.Settings.License = LicenseType.Community;

const string FontName = "Lato"; 
const string CoverImage = "cover.jpg";
const string OutputFile = "bulletin.pdf";

// Each PDF page is a landscape Letter sheet holding two 5.5" x 8.5" panels.
static IContainer Panel(IContainer c) => c.Padding(0.5f, Unit.Inch);

var document = Document.Create(container =>
{
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
                col.Item().Text("Rev. Mark Williamson, presiding minister").AlignCenter();
                col.Item().Text("Susan Pritchard, organist").AlignCenter();

                col.Item().LineHorizontal(1);

                col.Item().PaddingTop(4).Text("Welcome to St. Mark Lutheran Church.").AlignCenter();
                col.Item().Text("The order for this service is found in the Lutheran Service Book (in the hymnal rack) on page 184, Setting Three.").AlignCenter();

                col.Item().Height(10);

                col.Item().Text(t =>
                {
                    t.Span("ORGAN PRELUDE ").Bold();
                    t.Span("Holy Manna (Dale Wood)");
                });

                col.Item().Text(t =>
                {
                    t.Span("HYMN #540 ").Bold();
                    t.Span("Christ, the Word of God Incarnate");
                });

                col.Item().Text(t =>
                {
                    t.Span("NICENE OR APOSTLES' CREED ").Bold();
                    t.Span("LSB 158-159");
                });

                col.Item().Text(t =>
                {
                    t.Span("HYMN OF THE DAY #544 ").Bold();
                    t.Span("O Love, How Deep");
                });

                col.Item().Text(t =>
                {
                    t.Span("CLOSING HYMN #680 ").Bold();
                    t.Span("Thine the Amen, Thine the Praise");
                });

                col.Item().Text(t =>
                {
                    t.Span("ORGAN POSTLUDE ").Bold();
                    t.Span("Deo Gracias (Healey Willan)");
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

            // ---- RIGHT: Psalm 80 ----
            row.RelativeItem().Element(Panel).Column(col =>
            {
                col.Spacing(1.5f);
                col.Item().PaddingBottom(4).Text("Psalm 80: 7-19").FontSize(12).Bold();

                // (verse number, text lines)
                var psalm = new (string V, string[] Lines)[]
                {
                    ("7",  new[] { "Restore us, O God of hosts;", "let your face shine, that we may be saved!" }),
                    ("8",  new[] { "You brought a vine out of Egypt;", "you drove out the nations and planted it." }),
                    ("9",  new[] { "You cleared the ground for it;", "it took deep root and filled the land." }),
                    ("10", new[] { "The mountains were covered with its shade,", "the mighty cedars with its branches." }),
                    ("11", new[] { "It sent out its branches to the sea", "and its shoots to the River." }),
                    ("12", new[] { "Why then have you broken down its walls,", "so that all who pass along the way pluck its fruit?" }),
                    ("13", new[] { "The boar from the forest ravages it,", "and all that move in the field feed on it." }),
                    ("14", new[] { "Turn again, O God of hosts!", "Look down from heaven, and see;", "have regard for this vine," }),
                    ("15", new[] { "the stock that your right hand planted,", "and for the son whom you made strong for yourself." }),
                    ("16", new[] { "They have burned it with fire; they have cut it down;", "may they perish at the rebuke of your face!" }),
                    ("17", new[] { "But let your hand be on the man of your right hand,", "the son of man whom you have made strong for yourself!" }),
                    ("18", new[] { "Then we shall not turn back from you;", "give us life, and we will call upon your name!" }),
                    ("19", new[] { "Restore us, O LORD God of hosts!", "Let your face shine, that we may be saved!" }),
                };

                foreach (var (v, lines) in psalm)
                {
                    col.Item().Row(r =>
                    {
                        r.ConstantItem(20).Text(v);
                        r.RelativeItem().Column(c =>
                        {
                            for (int i = 0; i < lines.Length; i++)
                                c.Item().PaddingLeft(i == 0 ? 0 : 10).Text(lines[i]);
                        });
                    });
                }

                // Gloria Patri
                col.Item().PaddingTop(8).Row(r =>
                {
                    r.ConstantItem(20).Text("C").Bold();
                    r.RelativeItem().Column(c =>
                    {
                        c.Item().Text("Glory be to the Father and to the Son");
                        c.Item().PaddingLeft(10).Text("and to the Holy Spirit;");
                        c.Item().PaddingLeft(10).Text("as it was in the beginning,");
                        c.Item().PaddingLeft(10).Text("is now, and will be forever. Amen");
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

                col.Item().PaddingTop(2).AlignCenter().Text("Item of Interest").Italic();
                col.Item().Text("* $610 has been collected for the Goehrings from the 1-1-1 Missions Jar during 2026!");
                col.Item().Text("* St. Mark no longer has cleaning students from RH transitions program to help us.");

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
});

//document.ShowInCompanion();

Console.WriteLine($"Created {OutputFile}");

//psalm proof of concept
LiturgicalData myData = LiturgicalData.Load();
var calendar = new LiturgicalCalendar(2025, myData);
var date = new DateOnly(2026, 10, 25);
var result = calendar.Lookup(date, Lectionary.ThreeYear);
if (result != null && result.Readings != null)
{
    Console.WriteLine($"Readings for {result.Name}:");
    Console.WriteLine($"Old Testament: {result.Readings.Ot}");
    Console.WriteLine($"Psalm:         {result.Readings.Ps}");
    Console.WriteLine($"Epistle:       {result.Readings.Ep}");
    Console.WriteLine($"Gospel:        {result.Readings.Go}");
}
else
{
    Console.WriteLine("No readings found for this date.");
}