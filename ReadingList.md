# Reading List

## Overview

ReadingList is a basic console application built with CCRepl. It consists of a `Media` model, SQL service for persistent storage of `Media` entries, and a command set. Despite the name of the program, "Media" can refer to any kind of media. Media entries can contain information about the title itself, information about progress reading (or otherwise consuming) entries as well as notes and a rating system.

This was built so as to serve as a working example for more complete documentation. This file will go through the implementation of `CCRepl` features used to build the program. The code should be available to see as well. I will try to limit examples to more unique features of `CCRepl` which require explanation and not get into more basic C# logic.

## Implementation

There are two different Console Applications based on the `ReadingList` class library.

- `ReadingList.Cli` uses `ConsoleLineEditor` to handle input, which enables asynchronous cancellation (by pressing 'escape')
- `ReadingList.CliBasic` uses the default console input, which is more stable, but without cancellation capability.

In future I will make a UI using WPF which can have the flexibility and stability of both. Generally I would recommend using `ConsoleLineEditor`, but have made both for the sake of example.

Implementation of all CCRepl systems consists of assigning handlers for input and output, then defining the main input loop, these are two slightly different approaches to that:

### ReadingList.CliBasic

After defining a `MediaService` Class required for `MediaCommands` to function:

```csharp

// Define Repl object w/ command set.
Repl repl = new(new MediaCommands(service));

// Assign input & output handlers:
repl.ReqWrite += msg => Console.Write(msg);
repl.ReqWriteLine += msg => Console.WriteLine(msg);
repl.ReqInputAsync = (prompt, ct) =>
{
    Console.WriteLine(prompt);
    Console.Write("> ");
    string input = Console.ReadLine() ?? "";
    return Task.FromResult(input);
};

// Print startup message:
Console.WriteLine("CCRepl ReadingList.");
Console.WriteLine("Type 'help' for commands.");
Console.WriteLine("Type 'clear' to clear screen.");

// Main input loop:
while (true)
{
    // Await input:
    Console.Write("> ");
    string? line = Console.ReadLine();

    // Recieve input, process before handing it over to repl:
    if (string.IsNullOrWhiteSpace(line)) continue;
    if (line.Equals("exit", StringComparison.OrdinalIgnoreCase)) break;
    if (line.Equals("clear", StringComparison.OrdinalIgnoreCase)) Console.Clear();

    // Execute:
    await repl.ExecuteAsync(line);
}

// Print exit message:
Console.WriteLine("Exiting...");

```

REPL systems will likely follow this pattern, though `CCRepl` systems can function so long as there's an execution method (such as `ExecuteAsync()`, but you could also use `ExecuteJsonAsync()` or `ExecuteScriptAsync()` instead) and some way to provide input from a prompt to `ReqInputAsync`.

### ReadingList.Cli

Implementation with `ConsoleInputEditor` is more flexible, allows for keystroke cancellation, but is less stable, as I needed to rebuild the typing system inside an existing structure. This does indeed function, but there are some quirks, quickly resizing the window can crash the program due to cursor placement.

```csharp

// Define Repl object w/ command set.
Repl repl = new(new MediaCommands(service));

// History:
List<string> history = [];

// Assign input & output handlers:
repl.ReqWrite += msg => Console.Write(msg);
repl.ReqWriteLine += msg => Console.WriteLine(msg);
repl.ReqInputAsync = async (prompt, ct) =>
{
    Console.WriteLine(prompt);
    ConsoleInputEditor editor = new("> ", history);
    ConsoleResult result = await editor.ReadLineAsync(ct);
    if (result.Cancelled) throw new OperationCanceledException(ct);
    return result.Text;
};

// Print startup message:
Console.WriteLine("CCRepl ReadingList.");
Console.WriteLine("Type 'help' for commands.");
Console.WriteLine("Type 'clear' to clear screen.");

// Main input loop:
while (true)
{
    // Await input:
    ConsoleInputEditor editor = new("> ", history);
    ConsoleResult result = await editor.ReadLineAsync();
    
    // Recieve input, process before handing it over to repl:
    if (result.Cancelled) continue;
    string input = result.Text;
    if (string.IsNullOrWhiteSpace(input)) continue;
    if (input.Equals("exit", StringComparison.OrdinalIgnoreCase)) break;
    if (input.Equals("clear", StringComparison.OrdinalIgnoreCase))
    {
        Console.Clear();
        continue;
    }

    // Define variables for cancellation:
    using CancellationTokenSource cts = new();
    Task keyWatcher = InputHelpers.ConsoleCancelKeyWatcher(cts, ConsoleKey.Escape);

    // Execute, watching for cancellation:
    try { await repl.ExecuteAsync(input, cts.Token); }
    catch (OperationCanceledException) { Console.WriteLine("Cancelled."); }

    // Finally, dispose of keywatcher & handle exceptions:
    finally
    {
        cts.Cancel();
        try { await keyWatcher; }
        catch (OperationCanceledException) { }
    }
}

// Print exit message: 
Console.WriteLine("Exiting...");

```

Implementation differs from the basic version in a couple of places:

- History needs to be defined manually (Not done here, but persistent storage could be used to import history from previous sessions).
- Input is recieved with `ConsoleInputEditor` in the form of a `ConsoleResult` which also carries information about cancellation (and perhaps more in future).
- Cancellation handling is required.
- Implementation of `ConsoleCancelKeyWatcher` to detect cancellation keystrokes.

### Checklist

In general, the outer IO layer requires the following:

1. Handler for `ReqWrite` ("Request Write").
2. Handler for `ReqWriteLine`.
3. Handler for `ReqInputAsync` which returns a `string`.
4. Input loop

If using a different platform with other input methods, keywatchers are not necessary, nor disposal.

## Commands

Commands are defined in command set classes which implement `ICommandSet`. `ICommandSet` classes must implement a public `IReadOnlyList<ReplCommand> Commands`, commands can be defined in here, which can be done either with the `ReplCommand` constructor or `CmdBuilder` (recommended). 

Commands do not necessarily need to be defined in the `Commands` list (as opposed to some other class, only referenced in the list), and the methods do not necessarily need to be defined in the same class. In this example, the `MediaCommands` class has commands defined in the list, and the class is split into two partials.

Here are the commands:

```csharp

public partial class MediaCommands : ICommandSet
    {
        private MediaService _service;
        public MediaCommands(MediaService service)
        {
            _service = service;
        }

        public IReadOnlyList<ReplCommand> Commands =>
        [
            Cmd("Media")
                .Aliases("m", "md", "Read", "ReadingList", "rd", "rdl")
                .Description("Commands for interacting with media items.")
                .Children
                (
                    Cmd("Add")
                        .Aliases("a", "+", "New", "nw", "AddNew")
                        .Exec(MediaAdd)
                        .Usage("Media.Add <string Title> <string Type> <string Status> [int Release Year] [string Genre] [DateTime StartedOn] [DateTime FinishedOn] [string ProgressNote] [string Notes] [double Rating]")
                        .Description("Add a new piece of media to the list.")
                        .AddExample("Media.Add Thunderbirds Show InProgress 1965 _ \"Gerry Anderson\" 07-03-2026 _ \"Episode 23\" _ 10")
                        .AddExample("Media.Add \"Moby Dick\" Book Dropped 1851 _ \"Herman Melville\" _ _ _ \"Gave up, too boring, not as good as Thunderbirds (1965)\" 2.5")
                        .AddExample("m.+ \"Romance of the Three Kingdoms\" Book InProgress _ Romance \"Attributed to Luo GuangZhong\" 01-01-2026 _ \"Chapter 64\"")                        
                        .Children
                        (
                            Cmd("Prompt")
                                .Aliases("p", "pmpt", "pmt", "async")
                                .Description("Adds a new piece of media through input prompts.")
                                .Exec(MediaAddPrompt)
                                .Build()

                        )
                        .Build(),

                    Cmd("List")
                        .Aliases("l", "ls", "lst", "PrintAll", "Table", "tbl", "ReadingList")
                        .Exec(MediaList)
                        .Usage("Media.List [string SortBy]")
                        .Description("Lists all added media items.")
                        .LongDescription("Lists all added media items. Optional argument 'SortBy' has options 'Id', 'Title', 'Type', 'Status', 'Genre', 'Year', 'Creator', 'Rating', 'Started', 'Completed', 'Added' and 'Updated'.")
                        .Build(),

                    Cmd("Show")
                        .Aliases("shw", "View", "vw", "v")
                        .Exec(MediaShow)
                        .Usage("Media.Show <int Id>")
                        .Description("Shows all information for a particular media item.")
                        .Build(),

                    Cmd("Search")
                        .Exec(MediaSearch)
                        .Usage("Media.Search <string SearchKey> [string SortBy]")
                        .Description("Searches the reading list with given search key.")
                        .LongDescription("Searches the reading list with given search key. This works by building a 'SearchExpression' for each item consisting of 'Title Year Type Genre Creator': if searching for multiple of these, it needs to be in that order. Optional argument 'SortBy' has options 'Id', 'Title', 'Type', 'Status', 'Genre', 'Year', 'Creator', 'Rating', 'Started', 'Completed', 'Added' and 'Updated'.")
                        .Children
                        (

                        )
                        .Build(),

                    Cmd("Status")
                        .Aliases("s", "st", "stat", "sts", "state")
                        .Exec(MediaStatus)
                        .Usage("Media.Status <int Id> <string Status>")
                        .Description("Set media status for a media item.")
                        .LongDescription($"Sets media status for a media item to the specified status. Available statuses are: {MediaStatusExt.MediaStatusList}.")
                        .Children
                        (
                            Cmd("Planned")
                                .Aliases("Plan", "pln", "plnd", "Future", "ft", "ftr")
                                .Exec(MediaSetStatusPlanned)
                                .Usage("Media.Status.Planned <int Id>")
                                .Description("Set media status for specified media item as \"Planned\".")
                                .Build(),

                            Cmd("InProgress")
                                .Aliases("inp", "prg", "Progress", "prog", "Current", "Present")
                                .Exec(MediaSetStatusInProgress)
                                .Usage("Media.Status.InProgress <int Id>")
                                .Description("Set media status for specified media item as \"InProgress\".")
                                .Build(),
                            
                            Cmd("Completed")
                                .Aliases("c", "cplt", "Complete", "Finished", "Finish", "Done", "Read", "Watched")
                                .Exec(MediaSetStatusCompleted)
                                .Usage("Media.Status.Completed <int Id>")
                                .Description("Set media status for specified media item as \"Completed\".")
                                .Build(),

                            Cmd("Dropped")
                                .Aliases("d", "drp", "drpd", "GiveUp", "GiveIn", "Abandoned")
                                .Exec(MediaSetStatusDropped)
                                .Usage("Media.Status.Dropped <int Id>")
                                .Description("Set media status for specified media item as \"Dropped\".")
                                .Build(),

                            Cmd("Paused")
                                .Aliases("psd", "Break", "brk")
                                .Exec(MediaSetStatusPaused)
                                .Usage("Media.Status.Paused <int Id>")
                                .Description("Set media status for specified media item as \"Paused\".")
                                .Build(),

                            Cmd("AwaitingNew")
                                .Aliases("a", "an", "Awaiting", "Waiting", "wtn")
                                .Exec(MediaSetStatusAwaitingNew)
                                .Usage("Media.Static.AwaitingNew <int Id>")
                                .Description("Set media status for specified media item as \"AwaitingNew\".")
                                .Build(),

                            Cmd("Other")
                                .Exec(MediaSetStatusOther)
                                .Usage("Media.Status.Other <int Id>")
                                .Description("Set media status for specified media item as \"Other\".")
                                .Build()
                        )
                        .Build(),

                    Cmd("Rate")
                        .Aliases("r", "rt", "judge")
                        .Exec(MediaRate)
                        .Description("Rate a piece of media out of 10.")
                        .Build(),

                    Cmd("Note")
                        .Aliases("n", "Notes", "nt")
                        .Exec(MediaNote)
                        .Usage("Media.Note <int Id> [string Note]")
                        .Description("Set note, or append note for entry if one exists.")
                        .LongDescription("Will check if there is a note for Id, if there is one, will append, if not, will set (override). If argument \"Note\" is not given, will be prompted.")
                        .Children
                        (
                            Cmd("Override")
                                .Aliases("o", "ovr", "ovrd", "Replace", "Rplc")
                                .Exec(MediaNoteOverride)
                                .Usage("Media.Note.Override <int Id> [string Note]")
                                .Description("Sets note, overriding any existing notes.")
                                .Build(),
                            
                            Cmd("Append")
                                .Aliases("a", "app", "apnd", "add")
                                .Exec(MediaNoteAppend)
                                .Usage("Media.Note.Append <int Id> [string Note]")
                                .Description("Appends text to the end of a note.")
                                .Build(),

                            Cmd("Progress")
                                .Aliases("Prog", "ProgNote", "pn", "ProgressNote")
                                .Exec(MediaProgress)
                                .Usage("Media.Progress <int Id> [string Note]")
                                .Description("Sets Progress note")
                                .LongDescription("Sets the progress note for the specified media item. Will override anything that exists there presently.\nThe idea of a progress note is to note where you last left off so you know where to pick up next time, a book mark essentially, e.g. \"Chapter 5\", \"Episode 5\", \"1hr 5mins in\", &c.\nCanonical version of this command is 'Media.Progress'.")
                                .Build()

                        )
                        .Build(),

                    Cmd("Progress")
                        .Aliases("Prog", "ProgNote", "pn", "ProgressNote")
                        .Exec(MediaProgress)
                        .Usage("Media.Progress <int Id> [string Note]")
                        .Description("Sets Progress note")
                        .LongDescription("Sets the progress note for the specified media item. Will override anything that exists there presently.\nThe idea of a progress note is to note where you last left off so you know where to pick up next time, a book mark essentially, e.g. \"Chapter 5\", \"Episode 5\", \"1hr 5mins in\", &c.\nFor a more permanant record, see 'Media.Notes'.")
                        .Build(),

                    Cmd("Delete")
                        .Aliases("d", "del", "rm", "Remove", "Erase")
                        .Exec(MediaDelete)
                        .Usage("Media.Delete <int Id>")
                        .Description("Deletes a media item from the list.")
                        .Build()

                )
                .Build(),

            Cmd("Stats")
                .Exec(StatsSummary)
                .Description("Displays some information about reading list.")
                .Build()
        ];
    }

```

Commands are defined in a hierarchical structure,for example, `Media.Status.Planned`. There is no technical reason not to use single-headed commands such as `MediaStatusPlanned` if you want, simply define each command in series without implementing hierarchy with `Children()` (see below).

Commands can be repeated in different places. For example, `Media.Progress` is found on its own, and inside of `Media.Note`. This does not matter, as both call upon the same method (`MediaProgress()`).

You will notice that every command has its own method. As of present there is no way to pass an argument from a command definition to a method. In future, I might add a parameter in the form of an anonymous object which can be passed to methods, but presently, if you want to reuse code that way, commandhandlers can call other methods, see `MediaSetStatus()`.

Be cautious with naming, no two commands can have the same name, the Repl will not build if there is such a conflict. This does not count on different levels of the hierachy, for example, `Media.Add` is a command here. If we added a new command `Log.Add`.

Aliases are a handy way to handle for typos, and allowing the user to operate faster. Personally, I almost never use the canonical names as opposed to the 1-2 letter aliases. It must be noted however, that upon construction of the `Repl` object, every combination of alias for every command is entered into the alias dictionary. This can be seen with the base command `Commands.Aliases`. The size of this dictionary can increase exponentially which might be a concern for larger systems.

```csharp

public IReadOnlyList<ReplCommand> Commands =>
[
    Cmd("MediaStatus")
        .Exec(MediaStatus)
        .Usage("Media.Status <int Id> <string Status>")
        .Build(),

    Cmd("MediaStatusPlanned")
        .Exec(MediaSetStatusPlanned)
        .Usage("Media.Status.Planned <int Id>")
        .Description("Set media status for specified media item as \"Planned\".")
        .Build(),
];

```

## Command Handling

```csharp

using CCRepl;
using CCRepl.Models;
using CCRepl.Tools;
using ReadingList.Models;
using System.Text;

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
                s => (double.TryParse(s, out double v), v), "Could not parse, please try again.", null, "", " ", "_", "null", "unrated", "idk");

            _service.AddMedia(new Media(title, type, status, releaseYear, genre, creator, startedOn, completedOn, progressNote, notes, rating));

            ctx.WriteLine($"Added entry #{_service.GetLastId()}");
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
    }
}

```