using static CCRepl.Tools.CmdBuilder;
using CCRepl.Tools;
using CCRepl.Models;
using CCRepl.CommandSets;
using CCRepl;
using ReadingList.Services;
using System.Runtime.CompilerServices;

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
                        .Description("Lists all added media items.")
                        .Exec(NotImplemented)
                        .Children
                        (
                            Cmd("SortBy")
                                .Description("Displays list, sorted by specified parameter")
                                .Exec(NotImplemented)
                                .Build()

                        )
                        .Build(),

                    Cmd("Show")
                        .Description("Shows all information for a particular media item.")
                        .Exec(NotImplemented)
                        .Build(),

                    Cmd("Search")
                        .Description("Searches the reading list with given search key.")
                        .Exec(NotImplemented)
                        .Children
                        (

                        )
                        .Build(),

                    Cmd("Status")
                        .Description("Set media status for a media item.")
                        .Exec(NotImplemented)
                        .Children
                        (

                        )
                        .Build(),

                    Cmd("Rate")
                        .Description("Rate a piece of media out of 10.")
                        .Exec(NotImplemented)
                        .Build(),

                    Cmd("Note")
                        .Description("Add a note to a piece of media if none exists")
                        .Exec(NotImplemented)
                        .Children
                        (
                            Cmd("Override")
                                .Description("Sets note, overriding any existing notes.")
                                .Exec(NotImplemented)
                                .Build(),
                            
                            Cmd("Append")
                                .Description("Appends text to the end of a note.")
                                .Exec(NotImplemented)
                                .Build()
                        )
                        .Build(),

                    Cmd("Delete")
                        .Description("Deletes a media item from the list.")
                        .Exec(NotImplemented)
                        .Build()

                )
                .Build(),

            Cmd("Stats")
                .Description("Commands for viewing statistics for media items.")
                .Children
                (
                    Cmd("Summary")
                        .Description("Prints summary statistics.")
                        .Exec(NotImplemented)
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
                .Build()
        ];

        private Task NotImplemented(ReplContext ctx, IReadOnlyList<string> args, CancellationToken ct)
        {
            ctx.WriteLine("This command is not implemented yet. Sorry.");
            return Task.CompletedTask;
        }
    }
}