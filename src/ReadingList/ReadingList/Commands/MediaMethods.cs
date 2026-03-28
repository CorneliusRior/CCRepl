using CCRepl;
using CCRepl.Models;
using CCRepl.Tools;
using ReadingList.Models;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace ReadingList.Commands
{
    public partial class MediaCommands
    {
        private Task MediaAdd(ReplContext ctx, IReadOnlyList<string> args, CancellationToken ct)
        {
            // Mandatory arguments use "args.[Type]()"
            string title = args.String(0, "Title");

            // Types without dedicated extractors need to be manually converted after:
            string typeStr = args.String(1, "Type");
            if (!typeStr.TryToMediaType(out MediaType type)) throw new ReplUserException($"Could not parse type '{typeStr}'.");

            string statStr = args.String(2, "Status");
            if (!statStr.TryToMediaStatus(out MediaStatus status)) throw new ReplUserException($"Could not parse status '{statStr}'.");

            // Optional/Nullable arguments can use nullable extractors:
            int? releaseYear = args.IntOrNullable(3, "Release Year", null);
            string? genre = args.StringNullableOrDefault(4, "Genre", null);
            string? creator = args.StringNullableOrDefault(5, "Creator", null);
            DateTime? startedOn = args.dateTimeOrNullable(6, "Started On", null);
            DateTime? completedOn = args.dateTimeOrNullable(7, "Ended On", null);
            string? progressNote = args.StringNullableOrDefault(8, "Progress Note", null);
            string? notes = args.StringNullableOrDefault(9, "Notes", null);
            double? rating = args.DoubleOrNullable(10, "Rating", null);

            _service.AddMedia(new Media(title, type, status, releaseYear, genre, creator, startedOn, completedOn, progressNote, notes, rating));
            
            // Always provide feedback upon success:
            ctx.WriteLine($"Added entry #{_service.GetLastId()}");
            return Task.CompletedTask;
        }        

        private Task MediaAdd(ReplContext ctx, MediaAddPayload pl, CancellationToken ct)
        {
            // JSON overload. Uses "Payload" record.

            // Custom types/enums must be converted:
            MediaType type = pl.Type.ToMediaType();
            MediaStatus status = pl.Status.ToMediaStatus();

            _service.AddMedia(new Media(pl.Title, type, status, pl.ReleaseYear, pl.Genre, pl.Creator, pl.StartedOn, pl.CompletedOn, pl.ProgressNotes, pl.Notes, pl.Rating));

            // Feedback should be brief:
            ctx.WriteLine($"Added entry {_service.GetById(_service.GetLastId()).PrintRef()}");
            return Task.CompletedTask;
        }

        private Task<bool> MediaAddTest(ReplContext ctx, IReadOnlyList<string> args, CancellationToken ct)
        {
            // Use the same data extraction logic.
            string title = args.String(0, "Title");

            // You can have these return as "False" if you prefer, but you will not get information from exceptions. Test is considered a failure if there are any exceptions:
            string typeStr = args.String(1, "Type");
            if (!typeStr.TryToMediaType(out MediaType type)) throw new ReplUserException($"Could not parse type '{typeStr}'.");

            string statStr = args.String(2, "Status");
            if (!statStr.TryToMediaStatus(out MediaStatus status)) throw new ReplUserException($"Could not parse status '{statStr}'.");

            int? releaseYear = args.IntOrNullable(3, "Release Year", null);
            string? genre = args.StringNullableOrDefault(4, "Genre", null);
            string? creator = args.StringNullableOrDefault(5, "Creator", null);
            DateTime? startedOn = args.dateTimeOrNullable(6, "Started On", null);
            DateTime? completedOn = args.dateTimeOrNullable(7, "Ended On", null);
            string? progressNote = args.StringNullableOrDefault(8, "Progress Note", null);
            string? notes = args.StringNullableOrDefault(9, "Notes", null);
            double? rating = args.DoubleOrNullable(10, "Rating", null);

            Media Sample = new Media(title, type, status, releaseYear, genre, creator, startedOn, completedOn, progressNote, notes, rating);

            // If you set this as "async Task<bool> MediaAddTest()" this can just be "Return true;"
            return Task.FromResult(true);
        }

        private Task<bool> MediaAddTest(ReplContext ctx, MediaAddPayload pl, CancellationToken ct)
        {
            // JSON Tester. You usually do not need one of these! Testing of parsing payload is done automatically. Only use these if you need some conversion logic, or information which does not exist in the payload.

            MediaType type = pl.Type.ToMediaType();
            MediaStatus status = pl.Status.ToMediaStatus();

            Media Sample = new Media(pl.Title, type, status, pl.ReleaseYear, pl.Genre, pl.Creator, pl.StartedOn, pl.CompletedOn, pl.ProgressNotes, pl.Notes, pl.Rating);

            return Task.FromResult(true);
        }

        private async Task MediaAddPromptAsync(ReplContext ctx, IReadOnlyList<string> args, CancellationToken ct)
        {
            // Announcing command:
            ctx.WriteLine("Add Media Item.");

            // For required string arguments, use "RequireString()". This does not accept null/whitespace arguments.
            string title = await ctx.RequireString(ct, "Title: ", "Title cannot be blank, please try again.");

            // For non-string required arguments, use "RequireAsync()".
            MediaType type = await ctx.RequireAsync(ct, "Media type: ",
                s => (s.TryToMediaType(out MediaType v), v),
                $"Could not parse. Valid media types are: {MediaTypeExt.MediaTypeList}.");
            MediaStatus status = await ctx.RequireAsync(ct, "Status: ",
                s => (s.TryToMediaStatus(out MediaStatus v), v),
                $"Could not parse. Valid media statuses are: {MediaStatusExt.MediaStatusList}.");

            // For non-string optional arguments, you can use "RequestAsync()" or "RequireAsync()" with cancellation strings (see below).
            int? releaseYear = await ctx.RequestAsync<int?>(ct, "Release year (optional): ",
                s => (int.TryParse(s, out int v), v), null);

            // For optional string arguments, there are a couple of options. RequestStringNullable returns null if null or whitespace:
            string? genre = await ctx.RequestStringNullable(ct, "Genre (optional): ");
            string? creator = await ctx.RequestStringNullable(ct, "Creator (author, director, studio, &c., optional): ");

            // Cancellation strings can be used with RequireAsync() if you want explicit confirmation of no entry for non-string optional arguments. 
            DateTime? startedOn = await ctx.RequireAsync<DateTime?>(ct, 
                "Start date, if known, and have started (optional, leave blank, 'null', or 'not started' otherwise): ",
                s => (DateTime.TryParse(s, out DateTime v), v), 
                "Could not parse, please try again.", null, "", " ", "_", "null", "notstarted", "not started");
            DateTime? completedOn = await ctx.RequireAsync<DateTime?>(ct, 
                "Completion date, if known, and have finished (optional, leave blank, 'null', or 'unfinished' otheriwse): ",
                s => (DateTime.TryParse(s, out DateTime v), v), 
                "Could not parse, please try again.", null, "", " ", "_", "null", "unfinished", "notfinished");

            string? progressNote = await ctx.RequestStringNullable(ct, "Note on progress (e.g. 'Chapter 20', 'Episode 5', '1hr 20mins', optional): ");
            string? notes = await ctx.RequestStringNullable(ct, "Other notes (optional): ");
            double? rating = await ctx.RequireAsync<double?>(ct, 
                "Rate out of 10 (optional, leave blank, 'null', or 'unrated' otherwise): ",
                s => (double.TryParse(s, out double v), v), "Could not parse, please try again.", null, "", " ", "_", "null", "unrated", "idk");

            _service.AddMedia(new Media(title, type, status, releaseYear, genre, creator, startedOn, completedOn, progressNote, notes, rating));

            ctx.WriteLine($"Added entry #{_service.GetLastId()}");
        }        

        private Task MediaEdit(ReplContext ctx, IReadOnlyList<string> args, CancellationToken ct)
        {
            throw new NotImplementedException();
        }

        private async Task MediaEditAsync(ReplContext ctx, IReadOnlyList<string> args, CancellationToken ct)
        {
            int? id = args.IntOrNull(0, "Id");
            if (id is null) id = await ctx.RequireAsync(ct, "Enter entry ID: ", s => (int.TryParse(s, out int v), v), "Could not parse, please try again.");
            Media entry = _service.GetById(id.Value);
            ctx.WriteLine(entry.PrintInfo());

            entry.Title = await ctx.RequestStringOrDefault(ct, "Title (leave blank, or '_' to keep unchanged): ", entry.Title);
            entry.Type = await ctx.RequireAsync(ct, "Media type (leave blank, or '_' to keep unchanged): ", s => (s.TryToMediaType(out MediaType v), v), "Could not parse, please try again.", entry.Type, "", "_");
            entry.Status = await ctx.RequireAsync(ct, "Status (leave blank, or '_' to keep unchanged): ", s => (s.TryToMediaStatus(out MediaStatus v), v), "Could not parse, please try again.",entry.Status, "", "_");
            
        }

        private Task MediaList(ReplContext ctx, IReadOnlyList<string> args, CancellationToken ct)
        {
            string sortKey = args.StringOr(0, "SortBy", "Id");

            List<Media> readingList = SortBy(_service.GetAll(), sortKey);

            List<string?[]> stringList = new();
            foreach (Media m in readingList) stringList.Add(m.Items);
            PrintTable table = new(Media.Columns, stringList);
            ctx.WriteLine("Printing list.");
            ctx.WriteLine(table.Print());
            return Task.CompletedTask;
        }

        private List<Media> SortBy(List<Media> list, string sortKey)
        {
            return sortKey.Trim().ToLowerInvariant() switch
            {
                "id"        => list.OrderBy(m => m.Id).ToList(),
                "title"     => list.OrderBy(m => m.Title).ToList(),
                "type"      => list.OrderBy(m => m.Type).ToList(),
                "status"    => list.OrderBy(m => m.Status).ToList(),
                "year"      => list.OrderByDescending(m => m.ReleaseYear).ToList(),
                "genre"     => list.OrderBy(m => m.Genre).ToList(),
                "creator"   => list.OrderBy(m => m.Creator).ToList(),
                "rating"    => list.OrderByDescending(m => m.Rating).ToList(),
                "started"   => list.OrderByDescending(m => m.StartedOn).ToList(),
                "completed" => list.OrderByDescending(m => m.CompletedOn).ToList(),
                "added"     => list.OrderByDescending(m => m.AddedOn).ToList(),
                "updated"   => list.OrderByDescending(m => m.LastUpdated).ToList(),

                // Aliases:
                "name"          => list.OrderBy(m => m.Title).ToList(),
                "released"      => list.OrderByDescending(m => m.ReleaseYear).ToList(),
                "releaseyear"   => list.OrderByDescending(m => m.ReleaseYear).ToList(),
                "by"            => list.OrderBy(m => m.Creator).ToList(),
                "startdate"     => list.OrderByDescending(m => m.StartedOn).ToList(),
                "startedon"     => list.OrderByDescending(m => m.StartedOn).ToList(),
                "completeddate" => list.OrderByDescending(m => m.CompletedOn).ToList(),
                "completedon"   => list.OrderByDescending(m => m.CompletedOn).ToList(),
                "addeddate"     => list.OrderByDescending(m => m.AddedOn).ToList(),
                "addedon"       => list.OrderByDescending(m => m.AddedOn).ToList(),
                "updateddate"   => list.OrderByDescending(m => m.LastUpdated).ToList(),
                "lastupdated"   => list.OrderByDescending(m => m.LastUpdated).ToList(),
                _ => throw new ReplUserException($"Unknown sort type '{sortKey}', available sort types are: 'Id', 'Title', 'Type', 'Status', 'Genre', 'Year', 'Creator', 'Rating', 'Started', 'Completed', 'Added' and 'Updated'.")
            };
        }

        private Task MediaShow(ReplContext ctx, IReadOnlyList<string> args, CancellationToken ct)
        {
            int id = args.Int(0, "Id");
            Media entry = _service.GetById(id);
            ctx.WriteLine(entry.PrintInfo());
            return Task.CompletedTask;
        }

        private Task MediaSearch(ReplContext ctx, IReadOnlyList<string> args, CancellationToken ct)
        {
            string searchKey = args.String(0, "Search Key");
            string sortKey = args.StringOr(1, "SortBy", "Id");
            if (string.IsNullOrWhiteSpace(searchKey))
            {
                ctx.WriteLine($"Please add search key, usage: Media.Search <string SearchKey>");
                return Task.CompletedTask;
            }

            List<Media> full = _service.GetAll();
            List<Media> filt = full.Where(m => m.SearchExpression.Contains(searchKey, StringComparison.OrdinalIgnoreCase)).ToList();
            filt = SortBy(filt, sortKey);

            List<string?[]> stringList = new();
            foreach (Media m in filt) stringList.Add(m.Items);
            PrintTable table = new(Media.Columns, stringList);
            ctx.WriteLine($"Printing all items containing '{searchKey}':");
            ctx.WriteLine(table.Print());
            return Task.CompletedTask;
        }

        private Task MediaStatus(ReplContext ctx, IReadOnlyList<string> args, CancellationToken ct)
        {
            int id = args.Int(0, "Id");
            string statusStr = args.String(1, "Status");
            if (!statusStr.TryToMediaStatus(out MediaStatus status)) throw new ReplUserException($"Could not parse status '{statusStr}', available statuses are: {MediaStatusExt.MediaStatusList}.");
            
            Media entry = _service.GetById(id);
            entry.Status = status;
            _service.Update(entry);

            ctx.WriteLine($"Set status for entry {entry.PrintRef()} to '{entry.Status.ToString()}'.");
            return Task.CompletedTask; 
        }

        private Task MediaSetStatusPlanned(ReplContext ctx, IReadOnlyList<string> args, CancellationToken ct) => MediaSetStatus(ctx, args.Int(0, "Id"), Models.MediaStatus.Planned);

        private Task MediaSetStatusInProgress(ReplContext ctx, IReadOnlyList<string> args, CancellationToken ct) => MediaSetStatus(ctx, args.Int(0, "Id"), Models.MediaStatus.InProgress);

        private Task MediaSetStatusCompleted(ReplContext ctx, IReadOnlyList<string> args, CancellationToken ct) => MediaSetStatus(ctx, args.Int(0, "Id"), Models.MediaStatus.Completed);

        private Task MediaSetStatusDropped(ReplContext ctx, IReadOnlyList<string> args, CancellationToken ct) => MediaSetStatus(ctx, args.Int(0, "Id"), Models.MediaStatus.Dropped);

        private Task MediaSetStatusPaused(ReplContext ctx, IReadOnlyList<string> args, CancellationToken ct) => MediaSetStatus(ctx, args.Int(0, "Id"), Models.MediaStatus.Paused);

        private Task MediaSetStatusAwaitingNew(ReplContext ctx, IReadOnlyList<string> args, CancellationToken ct) => MediaSetStatus(ctx, args.Int(0, "Id"), Models.MediaStatus.AwaitingNew);

        private Task MediaSetStatusOther(ReplContext ctx, IReadOnlyList<string> args, CancellationToken ct) => MediaSetStatus(ctx, args.Int(0, "Id"), Models.MediaStatus.Other);

        private Task MediaSetStatus(ReplContext ctx, int id, MediaStatus status)
        {
            Media entry = _service.GetById(id);
            entry.Status = status;
            _service.Update(entry);
            ctx.WriteLine($"Set status for entry {entry.PrintRef()} to '{entry.Status.ToString()}'.");
            return Task.CompletedTask;
        }

        private async Task MediaRate(ReplContext ctx, IReadOnlyList<string> args, CancellationToken ct)
        {
            int id = args.Int(0, "Id");
            double rating = args.Double(1, "Rating");
            Media entry = _service.GetById(id);

            if (rating > 10)
            {
                ctx.WriteLine("The rating is meant to be out of ten, was it really better than that?");
                bool yes = await ctx.ConfirmAsync(ct);
                if (!yes) return;
            }
            if (rating < 0)
            {
                ctx.WriteLine("The rating is meant to be out of ten, given number is negative, was it really that bad?");
                bool yes = await ctx.ConfirmAsync(ct);
                if (!yes) return;
            }
            entry.Rating = rating;
            _service.Update(entry);
            ctx.WriteLine($"Set rating for {entry.PrintRef()} to {rating}/10");
        }

        private async Task MediaNote(ReplContext ctx, IReadOnlyList<string> args, CancellationToken ct)
        {
            int id = args.Int(0, "Id");
            string? note = args.StringOrNull(1, "Note");
            Media entry = _service.GetById(id);

            if (note is null)
            {
                ctx.WriteLine(entry.PrintRef());
                ctx.WriteLine($"Notes: " + (entry.Notes ?? "(none)"));
                ctx.WriteLine();
                note = await ctx.ReadLineAsync($"Add note to {entry.PrintRef()}: ", ct);
            }

            if (string.IsNullOrWhiteSpace(note))
            {
                ctx.Write("Empty string, returning.");
                return;
            }

            if (entry.Notes is null || string.IsNullOrWhiteSpace(entry.Notes)) entry.Notes = note;
            else entry.Notes = entry.Notes + $"\n\n(Appended at {DateTime.Now.ToString("g")}):\n" + note;

            _service.Update(entry);
            ctx.WriteLine("Edited:");
            ctx.WriteLine(entry.PrintInfo());
        }

        private async Task MediaNoteOverride(ReplContext ctx, IReadOnlyList<string> args, CancellationToken ct)
        {
            int id = args.Int(0, "Id");
            string? note = args.StringOrNull(1, "Note");
            Media entry = _service.GetById(id);

            if (note is null)
            {
                ctx.WriteLine(entry.PrintRef());
                ctx.WriteLine($"Notes: " + (entry.Notes ?? "(none)"));
                ctx.WriteLine();
                note = await ctx.ReadLineAsync($"Override note to {entry.PrintRef()}: ", ct);
            }

            if (string.IsNullOrWhiteSpace(note))
            {
                ctx.Write("Empty string, returning.");
                return;
            }

            entry.Notes = note;
            _service.Update(entry);
            ctx.WriteLine("Edited:");
            ctx.WriteLine(entry.PrintInfo());
        }

        private async Task MediaNoteAppend(ReplContext ctx, IReadOnlyList<string> args, CancellationToken ct)
        {
            int id = args.Int(0, "Id");
            string? note = args.StringOrNull(1, "Note");
            Media entry = _service.GetById(id);

            if (note is null)
            {
                ctx.WriteLine(entry.PrintRef());
                ctx.WriteLine($"Notes: " + (entry.Notes ?? "(none)"));
                ctx.WriteLine();
                note = await ctx.ReadLineAsync($"Append note to {entry.PrintRef()}: ", ct);
            }

            if (string.IsNullOrWhiteSpace(note))
            {
                ctx.Write("Empty string, returning.");
                return;
            }

            entry.Notes = entry.Notes + $"\n\n(Appended at {DateTime.Now.ToString("g")}):\n" + note;

            _service.Update(entry);
            ctx.WriteLine("Edited:");
            ctx.WriteLine(entry.PrintInfo());
        }

        private async Task MediaProgress(ReplContext ctx, IReadOnlyList<string> args, CancellationToken ct)
        {
            int id = args.Int(0, "Id");
            string? note = args.StringOrNull(1, "Note");
            Media entry = _service.GetById(id);

            // In the event of no entry:
            if (note is null)
            {
                ctx.WriteLine(entry.PrintInfo());
                note = await ctx.ReadLineAsync($"Set progress note: ", ct);
            }

            // Now we assume something was entered, in which case, null or whitespace can mean to delete progress note:
            if (string.IsNullOrWhiteSpace(note)) note = null;
            entry.ProgressNote = note;
            _service.Update(entry);
            ctx.WriteLine($"Updated progress note for {entry.PrintRef()}: \"{(entry.ProgressNote ?? "(None)")}\"");
        }

        private async Task MediaDelete(ReplContext ctx, IReadOnlyList<string> args, CancellationToken ct)
        {
            int id = args.Int(0, "Id");
            Media entry = _service.GetById(id);
            bool conf = await ctx.ConfirmAsync(ct, $"Delete entry #{id}: '{entry.Title}'? (Y/N): ", false);
            if (conf)
            {
                _service.Delete(id);
                ctx.WriteLine($"Deleted entry #{id}");
            }
        }

        private Task StatsSummary(ReplContext ctx, IReadOnlyList<string> args, CancellationToken ct)
        {
            List<Media> readingList = _service.GetAll();
            StatSummary stats = GenerateStatSummary(readingList);
            if (readingList.Count == 0) ctx.WriteLine("No items added to reading list.");
            else
            {
                ctx.WriteLine($"You have {stats.Count} item{(stats.Count == 1 ? "" : "s")} in your reading list:");

                foreach (var t in stats.TypeList)
                {
                    if (t.Value.Count > 0)
                    {
                        StringBuilder sb = new();

                        sb.AppendLine($"You have {t.Value.Count} {t.Key.ToDisplayString() + (t.Value.Count == 1 ? "" : "s")} on your list, of which:");

                        foreach (MediaStatus status in Enum.GetValues(typeof(MediaStatus)))
                        {
                            int count = t.Value.Where(m => m.Status == status).Count();
                            if (count > 0) sb.AppendLine($" - {count} {status.ToDisplayString()} ({((double)count / (double)t.Value.Count * 100).ToString("0.#")}%)");
                        }

                        List<Media> typeRated = t.Value.Where(m => m.Rating.HasValue).ToList();
                        if (typeRated.Count == 0) sb.AppendLine("\tNone of which are rated.");
                        else
                        {
                            sb.Append($"{typeRated.Count}/{t.Value.Count} of which are rated ({((double)typeRated.Count / (double)t.Value.Count * 100).ToString("0.#")}%), with an average rating of {typeRated.Average(m => m.Rating)!.Value.ToString("0.#")}/10.");
                        }

                        List<Media> typeWithYear = t.Value.Where(m => m.ReleaseYear.HasValue).ToList();
                        if (typeWithYear.Count > 0) sb.AppendLine($"The average (specified) year of publication is {(int?)typeWithYear.Average(m => m.ReleaseYear!)}");

                        ctx.WriteLine(sb.ToString().ToBox(boxWidth: 100, hPadding: 2, vPadding: 1, title: t.Key.ToDisplayString() + 's'));
                    }
                }

                ctx.WriteLine($"Overall, of all {stats.Count} item{(stats.Count == 1 ? "" : "s")},");
                foreach (var s in stats.StatusList)
                {
                    if (s.Value.Count > 0) ctx.WriteLine($" - {s.Value.Count} {(s.Value.Count == 1 ? "is" : "are")} listed as {s.Key.ToDisplayString()} ({((double)s.Value.Count / (double)stats.Count * 100).ToString("0.#")}%)");
                }

                List<Media> rated = readingList.Where(m => m.Rating.HasValue).ToList();
                if (rated.Count > 0)
                {
                    double avg = rated.Average(m => m.Rating)!.Value;
                    ctx.WriteLine($"\nYou rated {rated.Count} of them ({((double)rated.Count / (double)stats.Count * 100).ToString("0.#")}%), with an average rating of {avg.ToString("0.#")}/10.");
                }
                List<Media> withYear = readingList.Where(m => m.ReleaseYear.HasValue).ToList();
                if (withYear.Count > 0)
                {
                    if (withYear.Count > 0) ctx.WriteLine($"The average (specified) year of publication is {(int?)withYear.Average(m => m.ReleaseYear!)}");
                }
            }
            return Task.CompletedTask;
        }

        private StatSummary GenerateStatSummary(List<Media> list)
        {
            int count = list.Count;

            Dictionary<MediaType, List<Media>> typeList = new();
            foreach (MediaType type in Enum.GetValues(typeof(MediaType)))
            {
                typeList.Add(type, list.Where(m => m.Type == type).ToList());
            }

            Dictionary<MediaStatus, List<Media>> statusList = new();
            foreach (MediaStatus status in Enum.GetValues(typeof(MediaStatus)))
            {
                statusList.Add(status, list.Where(m => m.Status == status).ToList());
            }

            return new StatSummary(count, typeList, statusList);
        }

        private sealed record StatSummary(int Count, Dictionary<MediaType, List<Media>> TypeList, Dictionary<MediaStatus, List<Media>> StatusList);

        private sealed record MediaAddPayload(string Title, string Type, string Status, int? ReleaseYear, string? Genre, string? Creator, DateTime? StartedOn, DateTime? CompletedOn, string? ProgressNotes, string? Notes, double? Rating);
    }
}