using System.Net;
using System.Text.RegularExpressions;

namespace st_mark_bulletin_generator.Pdf;

// Small helpers for the rich-text (HTML) boxes on the bulletin form.
public static class HtmlText
{
    // True if the HTML holds any visible text. An empty rich-text box can still
    // give back markup such as "<p></p>", which must count as empty.
    public static bool HasContent(string? html)
    {
        if (string.IsNullOrWhiteSpace(html)) return false;

        var text = Regex.Replace(html, @"<[^>]*>", "");
        text = WebUtility.HtmlDecode(text);
        text = text.Replace('\u00A0', ' ').Replace("\u200B", "");
        return text.Trim().Length > 0;
    }

    // Makes every paragraph centered, whether or not it already has a style.
    public static string CenterParagraphs(string html)
    {
        return Regex.Replace(html, @"<(p)\b([^>]*)>", match =>
        {
            var tag = match.Groups[1].Value;
            var attributes = match.Groups[2].Value;

            // Already has an alignment: change it to center.
            if (Regex.IsMatch(attributes, @"text-align\s*:", RegexOptions.IgnoreCase))
            {
                var changed = Regex.Replace(attributes, @"text-align\s*:\s*[a-zA-Z]+",
                    "text-align:center", RegexOptions.IgnoreCase);
                return "<" + tag + changed + ">";
            }

            // Already has a style: put the alignment first.
            var style = Regex.Match(attributes, @"\bstyle\s*=\s*([""'])", RegexOptions.IgnoreCase);
            if (style.Success)
            {
                var at = style.Index + style.Length;
                return "<" + tag + attributes.Insert(at, "text-align:center;") + ">";
            }

            return "<" + tag + attributes + " style=\"text-align:center\">";
        }, RegexOptions.IgnoreCase);
    }
}