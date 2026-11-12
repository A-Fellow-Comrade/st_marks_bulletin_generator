using st_mark_bulletin_generator.Models;

namespace st_mark_bulletin_generator.Data;

// A single .bulletinbackup file: the app settings plus every saved Sunday, so
// the user can move all their bulletin data between computers in one file.
// Hymns (the SQLite database and TSV corrections) are deliberately NOT included.
public class BulletinBundle
{
    public int Version { get; set; } = 1;
    public AppSettings? Settings { get; set; }
    public List<SavedBulletin> Bulletins { get; set; } = new();
}
