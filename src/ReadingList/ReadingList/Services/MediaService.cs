using CCRepl.Models;
using Microsoft.Data.Sqlite;
using ReadingList.Models;

namespace ReadingList.Services
{
    public class MediaService
    {
        private string _connString;

        public MediaService(string connString)
        {
            _connString = connString;
            EnsureDatabase();
        }

        private void EnsureDatabase()
        {
            using SqliteConnection conn = new(_connString);
            conn.Open();
            string sql = @"
            CREATE TABLE IF NOT EXISTS ReadingList(
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Title TEXT NOT NULL,
                Type TEXT NOT NULL,
                Status TEXT NOT NULL,
                ReleaseYear INTEGER,
                Genre TEXT,
                Creator TEXT,
                StartedOn TEXT,
                CompletedOn TEXT,
                AddedOn TEXT,
                LastUpdated TEXT,
                ProgressNote TEXT,
                Notes TEXT,
                Rating DOUBLE
            );
            ";
            using SqliteCommand cmd = new(sql, conn);
            cmd.ExecuteNonQuery();
        }

        // New
        public void AddMedia(Media item)
        {
            using SqliteConnection conn = new(_connString);
            conn.Open();
            string sql = @"INSERT INTO ReadingList (Title, Type, Status, ReleaseYear, Genre, Creator, StartedOn, CompletedOn, AddedOn, LastUpdated, ProgressNote, Notes, Rating) VALUES ($title, $type, $status, $releaseYear, $genre, $creator, $startedOn, $completedOn, $addedOn, $lastUpdated, $progressNote, $notes, $rating)";
            using SqliteCommand cmd = new(sql, conn);

            cmd.Parameters.AddWithValue("$title", item.Title);
            cmd.Parameters.AddWithValue("$type", item.Type.ToString());
            cmd.Parameters.AddWithValue("$status", item.Status.ToString());
            cmd.Parameters.AddWithValue("$releaseYear", item.ReleaseYear ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("$genre", item.Genre ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("$creator", item.Creator ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("$startedOn", item.StartedOn ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("$completedOn", item.CompletedOn ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("$addedOn", item.AddedOn);
            cmd.Parameters.AddWithValue("$lastUpdated", DateTime.Now);
            cmd.Parameters.AddWithValue("$progressNote", item.ProgressNote ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("$notes", item.Notes ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("$rating", item.Rating ?? (object)DBNull.Value);

            cmd.ExecuteNonQuery();
        }

        // Get
        public Media GetById(int id)
        {
            using SqliteConnection conn = new(_connString);
            conn.Open();
            string sql = "SELECT * FROM ReadingList WHERE Id = $id";
            using SqliteCommand cmd = new(sql, conn);
            cmd.Parameters.AddWithValue("$id", id);
            SqliteDataReader reader = cmd.ExecuteReader();
            if (!reader.Read()) throw new ReplUserException($"Could not find entry with Id #{id}.");
            return new Media(
                reader.GetInt32(0),
                reader.GetString(1),
                reader.GetString(2).ToMediaType(),
                reader.GetString(3).ToMediaStatus(),
                reader.IsDBNull(4) ? null : reader.GetInt32(4),
                reader.IsDBNull(5) ? null : reader.GetString(5),
                reader.IsDBNull(6) ? null : reader.GetString(6),
                reader.IsDBNull(7) ? null : reader.GetDateTime(7),
                reader.IsDBNull(8) ? null : reader.GetDateTime(8),
                reader.GetDateTime(9),
                reader.GetDateTime(10),
                reader.IsDBNull(11) ? null : reader.GetString(11),
                reader.IsDBNull(12) ? null : reader.GetString(12),
                reader.IsDBNull(13) ? null : reader.GetDouble(13)
            );
        }

        // GetAll
        public List<Media> GetAll()
        {
            List<Media> readingList = new();
            using SqliteConnection conn = new(_connString);
            conn.Open();
            string sql = "SELECT * FROM ReadingList";
            using SqliteCommand cmd = new(sql, conn);
            SqliteDataReader reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                readingList.Add(
                    new Media(
                        reader.GetInt32(0),
                        reader.GetString(1),
                        reader.GetString(2).ToMediaType(),
                        reader.GetString(3).ToMediaStatus(),
                        reader.IsDBNull(4) ? null : reader.GetInt32(4),
                        reader.IsDBNull(5) ? null : reader.GetString(5),
                        reader.IsDBNull(6) ? null : reader.GetString(6),
                        reader.IsDBNull(7) ? null : reader.GetDateTime(7),
                        reader.IsDBNull(8) ? null : reader.GetDateTime(8),
                        reader.GetDateTime(9),
                        reader.GetDateTime(10),
                        reader.IsDBNull(11) ? null : reader.GetString(11),
                        reader.IsDBNull(12) ? null : reader.GetString(12),
                        reader.IsDBNull(13) ? null : reader.GetDouble(13)
                    )
                );
            }
            return readingList;
            
        }

        // Get Last ID:
        public int GetLastId()
        {
            using SqliteConnection conn = new(_connString);
            conn.Open();
            string sql = "SELECT Id FROM ReadingList ORDER BY Id DESC LIMIT 1";
            using SqliteCommand cmd = new(sql, conn);
            SqliteDataReader reader = cmd.ExecuteReader();
            if (!reader.Read()) throw new IndexOutOfRangeException();
            return reader.GetInt32(0);
        }

        // Update
        public void Update(Media item)
        {
            using SqliteConnection conn = new(_connString);
            conn.Open();
            string sql = @"
            UPDATE ReadingList SET
                Title = $title,
                Type = $type,
                Status = $status,
                ReleaseYear = $releaseYear,
                Genre = $genre,
                Creator = $creator,
                StartedOn = $startedOn,
                CompletedOn = $completedOn,
                AddedOn = $addedOn,
                LastUpdated = $lastUpdated,
                ProgressNote = $progressNote,
                Notes = $notes,
                Rating = $rating
            WHERE Id = $id
            ";
            using SqliteCommand cmd = new(sql, conn);

            cmd.Parameters.AddWithValue("$id", item.Id);
            cmd.Parameters.AddWithValue("$title", item.Title);
            cmd.Parameters.AddWithValue("$type", item.Type.ToString());
            cmd.Parameters.AddWithValue("$status", item.Status.ToString());
            cmd.Parameters.AddWithValue("$releaseYear", item.ReleaseYear ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("$genre", item.Genre ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("$creator", item.Creator ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("$startedOn", item.StartedOn ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("$completedOn", item.CompletedOn ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("$addedOn", item.AddedOn);
            cmd.Parameters.AddWithValue("$lastUpdated", DateTime.Now);
            cmd.Parameters.AddWithValue("$progressNote", item.ProgressNote ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("$notes", item.Notes ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("$rating", item.Rating ?? (object)DBNull.Value);

            cmd.ExecuteNonQuery();
        }

        // Delete
        public void Delete(int id)
        {
            using SqliteConnection conn = new(_connString);
            conn.Open();
            string sql = "DELETE FROM ReadingList WHERE Id = $id";
            using SqliteCommand cmd = new(sql, conn);
            cmd.Parameters.AddWithValue("$id", id);
            cmd.ExecuteNonQuery();
        }
    }
}