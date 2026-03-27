using static CCRepl.Tools.CmdBuilder;
using CCRepl.Tools;
using CCRepl.Models;
using CCRepl.CommandSets;
using CCRepl;
using ReadingList.Services;
using System.Runtime.CompilerServices;
using ReadingList.Models;

namespace ReadingList.Commands
{    
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
                        .Description("Add a note to a piece of media if none exists")
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
                                .Build()
                        )
                        .Build(),

                    Cmd("Delete")
                        .Aliases("d", "del", "rm", "Remove", "Erase")
                        .Exec(Delete)
                        .Usage("Media.Delete <int Id>")
                        .Description("Deletes a media item from the list.")
                        .Build()

                )
                .Build(),

            Cmd("Stats")
                .Description("Commands for viewing statistics for media items.")
                .Children
                (
                    Cmd("Summary")
                        .Exec(StatsSummary)
                        .Description("Prints summary statistics.")
                        .Build(),

                    Cmd("ByType")
                        .Description("Prints summary statistics by media type.")
                        .Exec(NotImplemented)
                        .Build(),

                    Cmd("ByStatus")
                        .Description("Prints summary statistics by media status.")
                        .Exec(NotImplemented)
                        .Build()
                )
                .Build(),

            Cmd("TestNew")
                .Description("Testcommands which you shouldn't see.")
                .Children
                (
                    Cmd("Wrap")
                        .Exec(TestWordWrap)
                        .Build()
                )
                .Build()
        ];

        private Task NotImplemented(ReplContext ctx, IReadOnlyList<string> args, CancellationToken ct)
        {
            ctx.WriteLine("This command is not implemented yet. Sorry.");
            return Task.CompletedTask;
        }

        private Task TestWordWrap(ReplContext ctx, IReadOnlyList<string> args, CancellationToken ct)
        {
            int width = args.Int(0, "Width");
            string str = args.String(1, "str");

            ctx.WriteLine($"Trying to wrap string: \n\"{str}\"\n into a space of {width}:");
            ctx.WriteLine("├" + new string ('─', width - 2) + "┤");
            List<string> wrapped = str.Wrap(width);
            foreach (string l in wrapped) ctx.WriteLine(l);
            return Task.CompletedTask;
        }
    }
}