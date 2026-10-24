using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace LiturgyTools;

public class PsalmParser
{
    public static string GetPsalm(string book, int chapter, string verseRange, string translation, string directory = ".")
    {

        // Parse the verse range (handles both "4-6" and "4")
        int startVerse, endVerse;
        if (verseRange.Contains("-"))
        {
            var parts = verseRange.Split('-');
            if (!int.TryParse(parts[0], out startVerse) || !int.TryParse(parts[1], out endVerse))
            {
                return "Error: Invalid verse range format.";
            }
        }
        else
        {
            if (!int.TryParse(verseRange, out startVerse))
            {
                return "Error: Invalid verse range format.";
            }
            endVerse = startVerse;
        }

        // Construct the file path
        string fileName = Path.Combine(directory, $"{book}_{translation}.csv");
        if (!File.Exists(fileName))
        {
            return $"Error: File '{fileName}' not found.";
        }

        // Read and parse the CSV (uses a custom parser to handle commas and newlines inside quoted strings)
        var verseData = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        string csvContent = File.ReadAllText(fileName);
        var parsedCsv = ParseCsv(csvContent);

        // Assuming column 0 is the Key (PSALM:1:1) and column 1 is the text
        foreach (var row in parsedCsv)
        {
            if (row.Count >= 2)
            {
                verseData[row[0].Trim()] = row[1];
            }
        }

        // Build the output string
        var sb = new StringBuilder();
        sb.AppendLine("var psalm = new (string V, string[] Lines)[]");
        sb.AppendLine("                    {");

        for (int v = startVerse; v <= endVerse; v++)
        {
            string key = $"{book.ToUpper()}:{chapter}:{v}";
            
            if (!verseData.TryGetValue(key, out string text))
            {
                return $"Error: Verse {key} does not exist in the file.";
            }

            // Split the verse text by newlines to create the string array for poetic lines
            var verseLines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            
            // Format each line with quotes, escaping any existing quotes inside the text
            string formattedLines = string.Join(", ", verseLines.Select(l => $"\"{l.Replace("\"", "\\\"").Trim()}\""));

            sb.AppendLine($"                        (\"{v}\",  new[] {{ {formattedLines} }}),");
        }

        sb.AppendLine("                    };");

        return sb.ToString();
    }

    /// <summary>
    /// A robust CSV parser that correctly handles commas and newlines inside quoted fields.
    /// </summary>
    private static List<List<string>> ParseCsv(string text)
    {
        var result = new List<List<string>>();
        var currentRow = new List<string>();
        var currentValue = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            if (inQuotes)
            {
                if (c == '\"')
                {
                    // Handle escaped quotes ("")
                    if (i + 1 < text.Length && text[i + 1] == '\"')
                    {
                        currentValue.Append('\"');
                        i++; 
                    }
                    else
                    {
                        inQuotes = false;
                    }
                }
                else
                {
                    currentValue.Append(c);
                }
            }
            else
            {
                if (c == '\"')
                {
                    inQuotes = true;
                }
                else if (c == ',')
                {
                    currentRow.Add(currentValue.ToString());
                    currentValue.Clear();
                }
                else if (c == '\r')
                {
                    // Skip carriage returns outside quotes to handle \r\n cleanly
                }
                else if (c == '\n')
                {
                    currentRow.Add(currentValue.ToString());
                    result.Add(currentRow);
                    currentRow = new List<string>();
                    currentValue.Clear();
                }
                else
                {
                    currentValue.Append(c);
                }
            }
        }

        // Add the final row if the file doesn't end with a newline
        if (currentValue.Length > 0 || currentRow.Count > 0)
        {
            currentRow.Add(currentValue.ToString());
            result.Add(currentRow);
        }

        return result;
    }

    public record ReadingDetails(string Book, string Chapter, string RequiredVerses, string OptionalVerses);

    public static ReadingDetails ParseReading(string reading)
    {
        if (string.IsNullOrWhiteSpace(reading))
            return new ReadingDetails("None", "None", "None", "none");

        // Regex breakdown:
        // (?<book>...)     Captures the book name, including optional leading numbers (e.g., "1 John", "Psalm")
        // (?<chapter>\d+)  Captures the chapter number before the colon
        // (?<required>...) Captures the required verse range after the colon
        // (?<optional>...) Captures anything inside trailing parentheses
        var pattern = @"^(?<book>(?:\d\s+)?[A-Za-z\s]+)\s+(?<chapter>\d+)(?::\s*(?<required>[0-9\-,a-z]+))?\s*(?:\((?<optional>[0-9\-,a-z]+)\))?";
        
        var match = Regex.Match(reading.Trim(), pattern);
        
        if (!match.Success)
            return new ReadingDetails("Unknown", "Unknown", "Unknown", "none");

        string book = match.Groups["book"].Value.Trim();
        string chapter = match.Groups["chapter"].Value.Trim();
        
        string required = match.Groups["required"].Success 
            ? match.Groups["required"].Value.Trim() 
            : "none"; // Fallback if it's just a whole chapter like "Psalm 23"
            
        string optional = match.Groups["optional"].Success 
            ? match.Groups["optional"].Value.Trim() 
            : "none";

        return new ReadingDetails(book, chapter, required, optional);
    }
}