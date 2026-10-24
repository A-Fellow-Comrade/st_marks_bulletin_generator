using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace LiturgyTools;

public class PsalmParser
{
    public static (string V, string[] Lines)[] GetPsalm(string book, int chapter, string verseRange, string translation, string directory = ".")
    {
        int startVerse, endVerse;
        if (verseRange.Contains("-"))
        {
            var parts = verseRange.Split('-');
            if (!int.TryParse(parts[0], out startVerse) || !int.TryParse(parts[1], out endVerse))
            {
                throw new ArgumentException("Invalid verse range format.");
            }
        }
        else
        {
            if (!int.TryParse(verseRange, out startVerse))
            {
                throw new ArgumentException("Invalid verse range format.");
            }
            endVerse = startVerse;
        }

        string fileName = Path.Combine(directory, $"{book}_{translation}.csv");
        if (!File.Exists(fileName))
        {
            throw new FileNotFoundException($"File '{fileName}' not found.");
        }

        var verseData = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        string csvContent = File.ReadAllText(fileName);
        var parsedCsv = ParseCsv(csvContent);

        // Populate dictionary: Key is PSALM:1:1, Value is the verse text
        foreach (var row in parsedCsv)
        {
            if (row.Count >= 2)
            {
                verseData[row[0].Trim()] = row[1];
            }
        }

        var resultList = new List<(string V, string[] Lines)>();

        for (int v = startVerse; v <= endVerse; v++)
        {
            string key = $"{book.ToUpper()}:{chapter}:{v}";
            
            if (!verseData.TryGetValue(key, out string text))
            {
                throw new KeyNotFoundException($"Verse {key} does not exist in the file.");
            }

            // Split the verse text by newlines to create the string array for poetic lines
            var verseLines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                                 .Select(l => l.Trim())
                                 .ToArray();
            
            resultList.Add((v.ToString(), verseLines));
        }

        // Return the actual array of tuples
        return resultList.ToArray();
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