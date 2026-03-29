using CCRepl.Models;
using CCRepl.Scripting;
using CCRepl.Tools;
using static CCRepl.Tools.CmdBuilder;

namespace CCRepl.CommandSets
{
    public sealed class BaseCommands : ICommandSet
    {
        public IReadOnlyList<ReplCommand> Commands =>
        [
            Cmd("Help")
            .Exec(Help)
            .Aliases("h", "?")
            .Usage("Help [string SearchKey]")
            .Description("Lists all commands and descriptions, or shows full help for all commands starting with SearchKey if specified.")
            .Examples("Help", "Help Diary.Add")
            .Group("Base")
            .Children(

                Cmd("List")
                .Aliases("l", "ls", "lst")
                .Exec(HelpDescription)
                .Usage("Help.List [string SearchKey]")
                .Description("Lists all commands and descriptions, or lists commands and descriptions for all commands starting with SearchKey if specified.")
                .Group("Base")
                .Build(),

                Cmd("Full")
                .Aliases("f", "fl")
                .Exec(HelpFull)
                .Usage("Help.Full [string SearchKey]")
                .Description("Shows full help information for all commands, or for all command starting with SearchKey if specified.")
                .Group("Base")
                .Build(),

                Cmd("Aliases")
                .Aliases("a", "als")
                .Exec(HelpAliases)
                .Usage("Help.Aliases [string SearchKey]")
                .Description("Lists all commands and their aliases, or lists all commands and description for all commands starting with SearchKey if specified.")
                .Group("Base")
                .Build(),

                Cmd("Description")
                .Aliases("d", "desc")
                .Exec(HelpDescription)
                .Usage("Help.Description [string SearchKey]")
                .Description("Lists all commands and descriptions, or lists commands and descriptions for all commands starting with SearchKey if specified.")
                .Group("Base")
                .Build(),

                Cmd("Examples")
                .Exec(HelpExamples)
                .Aliases("e", "x", "exmpl")
                .Usage("Help.Examples [string SearchKey]")
                .Description("Lists all commands and examples, or lists commands and examples for all commands starting with SearchKey if specified.")
                .Group("Base")
                .Build(),

                Cmd("LongDescription")
                .Exec(HelpLongDescription)
                .Aliases("ld", "long", "LongDesc")
                .Usage("Help.LongDescription")
                .Description("Lists all commands and Long Descriptions, or lists all commands and long descriptions for all commands starting with SearchKey if specified")
                .Group("Base")
                .Build()

                )
            .Build(),

            Cmd("CommandList")
            .Aliases("cmd", "Commands", "Command")
            .Exec(CommandList)
            .Description("Lists all commands.")
            .Group("Base")
            .Children(

                Cmd("Aliases")
                .Aliases("a", "all", "als")
                .Exec(CommandListAliases)
                .Description("Lists all commands and aliases for each command.")
                .Group("Base")
                .Build()

                )
            .Build(),

            Cmd("Test")
            .Exec(TestAsync)
            .Usage("Test <string Command>")
            .Description("Runs the TestAsync method on specified command with specified arguments.")
            .Group("Base")
            .Build(),

            Cmd("Json")
            .Description("Commands for manual use of Json commands")
            .Group("Base")
            .Children(

                Cmd("Run")
                .Exec(JsonRunAsync)
                .Usage("Json.Run <string CommandHead> [string Json]")
                .Description("Manually runs a Json command.")
                .Group("Base")
                .Build(),

                Cmd("Test")
                .Exec(JsonTestAsync)
                .Usage("Json.Run <string CommandHead> [string Json]")
                .Description("Manually tests a Json command.")
                .Group("Base")
                .Build()
                )
            .Build(),

            Cmd("Script")
            .Description("Commands for running scripts")
            .Group("Base")
            .Children(

                Cmd("Run")
                .Exec(ScriptRunAsync)
                .Usage("Script.Run <string filePath>")
                .Description("Runs a script from a file path. Tests first.")
                .Group("Base")
                .Children(

                    Cmd("Force")
                    .Aliases("Override")
                    .Exec(ScriptRunForceAsync)
                    .Usage("Script.Run.Force <string filePath")
                    .Description("Runs a script from a file path without testing.")
                    .Group("Base")
                    .Build()

                    )
                .Build(),

                Cmd("Test")
                .Exec(ScriptTestAsync)
                .Usage("Script.Test <string filePath>")
                .Description("Tests a script from a file path.")
                .Group("Base")
                .Build()

                )
            .Build()
        ];

        private Task Help(ReplContext ctx, IReadOnlyList<string> args, CancellationToken ct)
        {
            string? searchKey = args.StringOrNull(0, "SearchKey");
            if (searchKey is null) HelpPrintshort(ctx, HelpAttribute.Description, "", true);
            else HelpPrintLong(ctx, searchKey);
            return Task.CompletedTask;
        }

        private Task HelpDescription(ReplContext ctx, IReadOnlyList<string> args, CancellationToken ct)
        {
            string searchKey = args.StringOr(0, "SearchKey", "");
            HelpPrintshort(ctx, HelpAttribute.Description, searchKey, true);
            return Task.CompletedTask;
        }

        private Task HelpFull(ReplContext ctx, IReadOnlyList<string> args, CancellationToken ct)
        {
            string searchKey = args.StringOr(0, "SearchKey", "");
            HelpPrintLong(ctx, searchKey);
            return Task.CompletedTask;
        }

        private Task HelpAliases(ReplContext ctx, IReadOnlyList<string> args, CancellationToken ct)
        {
            string searchKey = args.StringOr(0, "SearchKey", "");
            HelpPrintshort(ctx, HelpAttribute.Aliases, searchKey, true);
            return Task.CompletedTask;
        }

        private Task HelpExamples(ReplContext ctx, IReadOnlyList<string> args, CancellationToken ct)
        {
            string searchKey = args.StringOr(0, "SearchKey", "");
            HelpPrintshort(ctx, HelpAttribute.Examples, searchKey, false);
            return Task.CompletedTask;
        }

        private Task HelpLongDescription(ReplContext ctx, IReadOnlyList<string> args, CancellationToken ct)
        {
            string searchKey = args.StringOr(0, "SearchKey", "");
            HelpPrintshort(ctx, HelpAttribute.LongDescription, searchKey, false);
            return Task.CompletedTask;
        }

        private void HelpPrintshort(ReplContext ctx, HelpAttribute help, string searchTerm = "", bool oneline = false, bool group = true)
        {
            List<ReplCommand> commands = ctx.SearchDictionary(searchTerm);
            int col1space = Math.Min(commands.Max(c => c.Address!.Length) + 3, 100);
            int col2space = Math.Max(ctx.OneLineMaxWidth - col1space, 0);

            if (!group)
            {
                ctx.WriteLine();
                foreach (ReplCommand c in commands) ctx.WriteLine(c.PrintShort(col1space, col2space, help, oneline));
                return;
            }

            // Seperate into groups:
            List<string?> groups = commands.DistinctBy(c => c.Group).Select(c => c.Group).ToList();
            if (groups.Count < 2)
            {
                ctx.WriteLine();
                foreach (ReplCommand c in commands) ctx.WriteLine(c.PrintShort(col1space, col2space, help, oneline));
                return;
            }

            foreach (string? g in groups)
            {
                List<ReplCommand> gc = commands.Where(c => c.Group == g).ToList();

                ctx.WriteLine();
                ctx.WriteLine($"/─── {(g ?? "(Ungrouped)")}: " + new string('─', ctx.OneLineMaxWidth - 8 - (g ?? "(Ungrouped)").Length) + '/');
                ctx.WriteLine();

                foreach (ReplCommand c in gc) ctx.WriteLine(c.PrintShort(col1space, col2space, help, oneline));
            }
        }

        private void HelpPrintLong(ReplContext ctx, string searchTerm = "")
        {
            ctx.WriteLine();
            List<ReplCommand> commands = ctx.SearchDictionary(searchTerm);
            foreach (ReplCommand c in commands) ctx.WriteLine(c.PrintLong());
        }

        private Task CommandList(ReplContext ctx, IReadOnlyList<string> args, CancellationToken ct)
        {
            ctx.WriteLine("Printing all commands. Try 'help <command>' for more information:");
            List<ReplCommand> commands = ctx.SearchDictionary();
            foreach (ReplCommand c in commands) ctx.WriteLine(c.Address);
            return Task.CompletedTask;
        }

        private Task CommandListAliases(ReplContext ctx, IReadOnlyList<string> args, CancellationToken ct)
        {
            int col = Math.Min(ctx.AliasIndex.Max(kv => kv.Key.Length), (ctx.OneLineMaxWidth - 10) / 2);
            ctx.WriteLine("Printing all commands and aliases. Try 'help <command>` for more information:");
            ctx.WriteLine();
            foreach (var kv in ctx.AliasIndex.OrderBy(kv => kv.Value.Address)) ctx.WriteLine($"{kv.Key.Truncate(col) + new string('.', col - kv.Key.Length)}...{kv.Value.Address.Truncate(col)}");

            string report = $"Total of {ctx.AliasIndex.Count} total aliases for {ctx.SearchDictionary().Count} commands.";
            ctx.WriteLine();
            ctx.WriteLine(report.ToBox(boxWidth: Math.Min(ctx.OneLineMaxWidth, report.Length + 20), vPadding: 1, hPadding: 10));
            return Task.CompletedTask;
        }

        private async Task TestAsync(ReplContext ctx, IReadOnlyList<string> args, CancellationToken ct)
        {
            string commandHead = args[0];
            IReadOnlyList<string> testArgs = args.Skip(1).ToList();
            bool success = await ctx.TestCommandAsync(commandHead, args, ct);
            if (success) ctx.WriteLine($"No issues found: '{string.Join(' ', args)}'.");
            else ctx.WriteLine($"Failed test: '{string.Join(' ', args)}'.");
        }

        private async Task JsonRunAsync(ReplContext ctx, IReadOnlyList<string> args, CancellationToken ct)
        {
            string commandHead = args.String(0, "CommandHead");
            string json = string.Join(' ', args.Skip(1));
            if (string.IsNullOrWhiteSpace(json)) json = await ctx.ReadLineAsync("Please enter Json argument:", ct);
            await ctx.ExecuteJsonAsync(commandHead, json);
        }

        private async Task JsonTestAsync(ReplContext ctx, IReadOnlyList<string> args, CancellationToken ct)
        {
            string commandHead = args.String(0, "CommandHead");
            string json = string.Join(' ', args.Skip(1));
            if (string.IsNullOrWhiteSpace(json)) json = await ctx.ReadLineAsync("Please enter Json argument:", ct);
            bool success = await ctx.TestJsonAsync(commandHead, json, ct);
            if (success) ctx.WriteLine($"No issues found: '{string.Join(' ', args)}'.");
            else ctx.WriteLine($"Failed test: '{string.Join(' ', args)}'.");
        }

        private async Task ScriptRunAsync(ReplContext ctx, IReadOnlyList<string> args, CancellationToken ct)
        {
            string path = args.StringOrNull(0, "File Path") ?? await ctx.ReadLineAsync("Please enter filepath:", ct);
            ctx.WriteLine($"Parsing file '{Path.GetFileName(path)}'...");
            Script script = await ctx.WithWaiterAsync(_ => Task.Run(() => ScriptParser.FromPath(path)), "Parsing Script ", "", "Parsed.", 100, ct, WaitAnimation.Spinner);
            if (await ctx.TestScriptAsync(script, ct)) await ctx.ExecuteScriptAsync(script, ct);
        }

        private async Task ScriptRunForceAsync(ReplContext ctx, IReadOnlyList<string> args, CancellationToken ct)
        {
            string path = args.StringOrNull(0, "FilePath") ?? await ctx.ReadLineAsync("Please enter filepath:", ct);
            ctx.WriteLine($"Parsing file '{Path.GetFileName(path)}'...");
            Script script = await ctx.WithWaiterAsync(_ => Task.Run(() => ScriptParser.FromPath(path)), "Parsing Script ", "", "Parsed.", 100, ct, WaitAnimation.Spinner);
            await ctx.ExecuteScriptAsync(script, ct);
        }

        private async Task ScriptTestAsync(ReplContext ctx, IReadOnlyList<string> args, CancellationToken ct)
        {
            string path = args.StringOrNull(0, "File Path") ?? await ctx.ReadLineAsync("Please enter filepath:", ct);
            ctx.WriteLine($"Parsing file '{Path.GetFileName(path)}'...");
            Script script = await ctx.WithWaiterAsync(_ => Task.Run(() => ScriptParser.FromPath(path)), "Parsing Script ", "", "Parsed.", 100, ct, WaitAnimation.Spinner);
            await ctx.TestScriptAsync(script, ct);
        }
    }
}