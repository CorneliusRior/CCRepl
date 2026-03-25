namespace ReadingList.Models
{
    public class Media
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public MediaType Type { get; set; }
        public MediaStatus Status { get; set; }
        public int? ReleaseYear { get; set; }
        public string? Genre { get; set; }
        public string? Creator { get; set; }
        public DateTime? StartedOn { get; set; }
        public DateTime? CompletedOn { get; set; }
        public DateTime AddedOn { get; set; }
        public DateTime LastUpdated { get; set; }
        public string? ProgressNote { get; set; }
        public string? Notes { get; set; }
        public double? Rating { get; set; }
    
        // Load:
        public Media(int id, string title, MediaType type, MediaStatus status, int? releaseYear, string? genre, string? creator, DateTime? startedOn, DateTime? completedOn, DateTime addedOn, DateTime lastUpdated, string? progressNote, string? notes, double? rating)
        {
            Id = id;
            Title = title;
            Type = type;
            Status = status;
            ReleaseYear = releaseYear;
            Genre = genre;
            Creator = creator;
            StartedOn = startedOn;
            CompletedOn = completedOn;
            AddedOn = addedOn;
            LastUpdated = lastUpdated;
            ProgressNote = progressNote;
            Notes = notes;
            Rating = rating;
        }

        // New:
        public Media(string title, MediaType type, MediaStatus status = MediaStatus.Planned, int? releaseYear = null, string? genre = null, string? creator = null, DateTime? startedOn = null, DateTime? completedOn = null, string? progressNote = null, string? notes = null, double? rating = null)
        {
            Title = title;
            Type = type;
            Status = status;
            ReleaseYear = releaseYear;
            Genre = genre;
            Creator = creator;
            StartedOn = startedOn;
            CompletedOn = completedOn;
            AddedOn = DateTime.Now;
            LastUpdated = DateTime.Now;
            ProgressNote = progressNote;
            Notes = notes;
            Rating = rating;
        }
    }
}
