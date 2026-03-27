using System.Data;
using System.Text;
using CCRepl.Tools;

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

        // Search Expression:
        public string SearchExpression => $"{Title} {ReleaseYear} {Type.ToDisplayString()} {Genre} {Creator}";

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

        // Print variables/functions:
        public string PrintRef() => $"#{Id} '{Title}' {(ReleaseYear is not null ? $"({ReleaseYear})" : "")}";
        public string PrintInfo()
        {
            StringBuilder sb = new();
            sb.Append($"[#{Id}] '{Title}' ");
            if (ReleaseYear is not null) sb.Append($"({ReleaseYear.ToString()}) ");            
            sb.AppendLine($"({Type.ToString()}) ");
            
            sb.AppendLine();
            sb.AppendLine($"Status: {Status.ToDisplayString()}");

            sb.AppendLine($"Genre: {Genre ?? "(unspecified)"}");
            sb.AppendLine($"Creator: {Creator ?? "(unspecified)"}");

            if (StartedOn is not null) sb.Append($"Started {Type.ToVerb()} on {StartedOn.Value.ToString("d")}. ");
            if (CompletedOn is not null) sb.Append($"Finished {Type.ToVerb()} on {CompletedOn.Value.ToString("d")}.");
            if (StartedOn is not null || CompletedOn is not null) sb.AppendLine();

            sb.AppendLine("Notes: " + (string.IsNullOrWhiteSpace(Notes) ? "(none)." : Notes));
            sb.AppendLine("Rating: " + (Rating is null ? "-" : Rating.Value.ToString("0.#")) + "/10");
            sb.AppendLine();
            sb.Append($"(Added: {AddedOn.ToString("g")}. Last Updated: {LastUpdated.ToString("g")})");

            return sb.ToString().ToBox();
        }
        public static List<PrintTableColumn> Columns => 
        [
            new("Id",           4,      false),
            new("Title:",       24,     false),
            new("Type:",        8,      false),
            new("Status:",      12,     false),
            new("Year:",        5,      true),
            new("Genre:",       10,     false),
            new("Creator",      20,     false),
            new("Started:",     10,     true),
            new("Finished:",    10,     true),
            new("Progress:",    20,     false),
            new("Notes:",       25,     false),
            new("Rating:",      8,      true),
            new("Added:",       10,     true),
            new("Last Updated:",20,     true)
        ];

        public string?[] Items => 
        [
            $"#{Id}",
            Title,
            Type.ToString(),
            Status.ToString(),
            ReleaseYear is null ? null : ReleaseYear.ToString(),
            Genre,
            Creator,
            StartedOn is null ? null : StartedOn.Value.ToString("d"),
            CompletedOn is null ? null : CompletedOn.Value.ToString("d"),
            ProgressNote,
            Notes,
            Rating is null ? "-/10" : Rating.Value.ToString("0.#") + "/10",
            AddedOn.ToString("d"),
            LastUpdated.ToString("g")
        ];

        //public static PrintTable GetTable() => new PrintTable(Headers, ColumnWidths, AlignRight);
    }
}
