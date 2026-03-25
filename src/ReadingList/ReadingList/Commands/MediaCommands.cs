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
                .Description("Commands for interacting with media items.")
                .Children
                (
                    Cmd("Add")
                        .Description("Add a new piece of media to the list.")
                        .Exec(NotImplemented)
                        .Children
                        (
                            Cmd("Prompt")
                                .Description("Adds a new piece of media through input prompts.")
                                .Exec(NotImplemented)
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