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
    }
}