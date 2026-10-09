using Microsoft.Data.Sqlite;

namespace st_mark_bulletin_generator.Data;

public class HymnRepository
{
    private readonly string _connectionString;
    public HymnRepository(string dbPath)
    {
        _connectionString = $"Data Source={dbPath}";
        CreateTable();
    }

    /**
    Creates a new table (hymn) in the Database
    */
    private void CreateTable(){
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();

        var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS Hymns(
                Number INTEGER PRIMARY KEY,
                Title TEXT NOT NULL
            )";
        cmd.ExecuteNonQuery();
    }

    /**
    Reads the TSV file: number <TAB> title.
    Adds hymns that aren't in the database, so any title
    that is corrected aren't overwritten by a re-import.
    */
    public int ImportTsv(string tsvPath)
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        using var tx = conn.BeginTransaction();
        int added = 0;
        
        foreach (var line in File.ReadLines(tsvPath))
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;
            var parts = line.Split('\t', 2);
            if (parts.Length < 2) continue;
            if (!int.TryParse(parts[0].Trim(), out var number)) continue;

            var cmd = conn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = 
                "INSERT OR IGNORE INTO Hymns (Number, Title) VALUES ($n, $t)";
            cmd.Parameters.AddWithValue("$n", number);
            cmd.Parameters.AddWithValue("$t", parts[1].Trim());
            added += cmd.ExecuteNonQuery();
        }
        tx.Commit();
        return added;
    }

    /**
    Retrieves a hymn by its number.
    Returns null if not found.
    */
    public Hymn? GetByNumber(int number)
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Number, Title FROM Hymns WHERE Number = $n";
        cmd.Parameters.AddWithValue("$n", number);
        using var reader = cmd.ExecuteReader();
        return reader.Read() ? new Hymn(reader.GetInt32(0), reader.GetString(1)) : null;
    }

    /**
    Adds a hymn or overwrites an existing title.
    This is for the GUI when there are missing numbers or wrong titles/
    */
    public void Upsert(int number, string title)
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = 
            "INSERT OR REPLACE INTO Hymns (Number, Title) VALUES ($n, $t)";
        cmd.Parameters.AddWithValue("$n", number);
        cmd.Parameters.AddWithValue("$t", title.Trim());
        cmd.ExecuteNonQuery();
    }
}