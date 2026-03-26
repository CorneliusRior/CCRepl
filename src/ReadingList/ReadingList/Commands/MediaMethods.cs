using CCRepl;
using CCRepl.Models;
using CCRepl.Tools;
using ReadingList.Models;

namespace ReadingList.Commands
{
    public partial class MediaCommands
    {
        private Task MediaAdd(ReplContext ctx, IReadOnlyList<string> args, CancellationToken ct)
        {
            string title = args.String(0, "Title");
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

            _service.AddMedia(new Media(title, type, status, releaseYear, genre, creator, startedOn, completedOn, progressNote, notes, rating));

            ctx.WriteLine($"Added entry #{_service.GetLastId()}");

            return Task.CompletedTask;
        }

        private async Task MediaAddPrompt(ReplContext ctx, IReadOnlyList<string> args, CancellationToken ct)
        {
            ctx.WriteLine("Add Media Item.");
            string title = await ctx.RequireString(ct, "Title: ", "Title cannot be blank, please try again.");
            MediaType type = await ctx.RequireAsync(ct, "Media type: ",
                s => (s.TryToMediaType(out MediaType v), v),
                $"Could not parse. Valid media types are: {MediaTypeExt.MediaTypeList}.");
            MediaStatus status = await ctx.RequireAsync(ct, "Status: ",
                s => (s.TryToMediaStatus(out MediaStatus v), v),
                $"Could not parse. Valid media statuses are: {MediaStatusExt.MediaStatusList}.");
            int? releaseYear = await ctx.RequestAsync<int?>(ct, "Release year (optional): ",
                s => (int.TryParse(s, out int v), v), null);
            string? genre = await ctx.RequestStringNullable(ct, "Genre (optional): ");
            string? creator = await ctx.RequestStringNullable(ct, "Creator (author, director, studio, &c., optional): ");
            DateTime? startedOn = await ctx.RequireAsync<DateTime?>(ct, "Start date, if known, and have started (optional, leave blank, 'null', or 'not started' otherwise): ",
                s => (DateTime.TryParse(s, out DateTime v), v), "Could not parse, please try again.", null, "", " ", "_", "null", "notstarted", "not started");
            DateTime? completedOn = await ctx.RequireAsync<DateTime?>(ct, "Completion date, if known, and have finished (optional, leave blank, 'null', or 'unfinished' otheriwse): ",
                s => (DateTime.TryParse(s, out DateTime v), v), "Could not parse, please try again.", null, "", " ", "_", "null", "unfinished", "notfinished");
            string? progressNote = await ctx.RequestStringNullable(ct, "Note on progress (e.g. 'Chapter 20', 'Episode 5', '1hr 20mins', optional): ");
            string? notes = await ctx.RequestStringNullable(ct, "Other notes (optional): ");
            double? rating = await ctx.RequireAsync<double?>(ct, "Rate out of 10 (optional, leave blank, 'null', or 'unrated' otherwise): ",
                s => (int.TryParse(s, out int v), v), "Could not parse, please try again.", null, "", " ", "_", "null", "unrated", "idk");

            _service.AddMedia(new Media(title, type, status, releaseYear, genre, creator, startedOn, completedOn, progressNote, notes, rating));

            ctx.WriteLine($"Added entry #{_service.GetLastId()}");
        }

        private Task MediaList(ReplContext ctx, IReadOnlyList<string> args, CancellationToken ct)
        {
            List<Media> readingList = _service.GetAll();
            List<string?[]> stringList = new();
            foreach (Media m in readingList) stringList.Add(m.Items);
            PrintTable table = Media.GetTable();
            table.AddItems(stringList);
            ctx.WriteLine("Printing list.");
            ctx.WriteLine(table.Print());
            return Task.CompletedTask;
        }

        private Task MediaShow(ReplContext ctx, IReadOnlyList<string> args, CancellationToken ct)
        {
            int id = args.Int(0, "Id");
            Media entry = _service.GetById(id);
            ctx.WriteLine(entry.PrintInfo());
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

        private async Task Delete(ReplContext ctx, IReadOnlyList<string> args, CancellationToken ct)
        {
            int id = args.Int(0, "Id");
            Media entry = _service.GetById(id);
            bool conf = await ctx.ConfirmAsync(ct, $"Delete entry #{id}: '{entry.Title}'? (Y/N): ", false);
            if (conf) _service.Delete(id);
            ctx.WriteLine($"Deleted entry #{id}");
        }
    }
}