using CCRepl.Models;
using CCRepl.Scripting;
using CCRepl.Tools;
using System.Text;
using static CCRepl.Tools.CmdBuilder;

namespace CCRepl.CommandSets
{
    public sealed class BaseCommands : ICommandSet
    {
        public IReadOnlyList<ReplCommand> Commands =>
        [
            Cmd("Help")
            .Aliases("h", "?")
            .Exec(Help)
            .Args(StrArg("Search Key", ArgMode.Optional, ""))
            .Optn("-a", "-d", "-e", "-f", "-u", "-g", "-l", "-o")
            .Description("Lists all commands and descriptions, or shows full help for all commands starting with Search Key if specified.")
            .LongDescription(
                @"Lists all commands and descriptions, or full help for all commands starting with Search Key. Behaviour altered with options:
 * '-a' ('aliases'): Prints list of all aliases for that command node (to see full list of all possible combinations, see Help.Alias).
 * '-d' ('description'): Prints full description without truncation.
 * '-e' ('example'): Prints example usages.
 * '-f' ('full'): Prints full info regardless of search key presence.
 * '-g' ('group'): Prints by group (by default only done with no search term. Use '-o' to ungroup that)
 * '-l' ('longdescription'): Prints full long description without truncation.
 * '-o' ('oneline', also '-ol'): Prints description regardless of search key presence.
 * '-u' ('usage'): Prints usage statements instead of description.
Checks for options with 'startswith'. Only the first valid options is used (except for '-g').")
            .Examples(
                "Help", 
                "Help(Diary.Add)",
                "Help(Diary) -usage")
            .Group("Base")
            .Children(

                Cmd("Tree")
                .Aliases("t", "map")
                .Exec(HelpTree)
                .Description("Prints full help tree")
                .Group("Base")
                .Build(),

                Cmd("Aliases")
                .Aliases("a", "als")
                .Exec(HelpAliases)
                .Args(StrArg("Search Key", ArgMode.Optional, ""))
                .Optn("-g")
                .Description("Lists all aliases and their canonical names for all commands, or for all commands and aliases starting with Search Key is specified.")
                .LongDescription(@"Lists all combinations of aliases and their canonical names for all commands, or for all commands and aliases starting with Search Key is specified.
Behaviour can be altered with options:
 * '-g' ('group'): Prints by group.")
                .Group("Base")
                .Build()

                )
            .Build(),

            Cmd("CommandList")
            .Aliases("cmd", "Commands", "Command")
            .StringExec(CommandList)
            .Description("Lists all commands.")
            .Group("Base")
            .Children(

                Cmd("Aliases")
                .Aliases("a", "all", "als")
                .StringExec(CommandListAliases)
                .Description("Lists all commands and aliases for each command.")
                .Group("Base")
                .Build()

                )
            .Build(),

            Cmd("Test")
            .Exec(TestAsync)
            .Args(StrArg("Command Input", ArgMode.RequiredPrompt))
            .Description("Runs the TestAsync method on specified command with specified arguments. Prompts if no input is given.")
            .Examples("Test({Diary.Add(This is a test entry) -f})")
            .Group("Base")
            .Build(),

            Cmd("Json")
            .Description("Commands for manual use of Json commands")
            .Group("Base")
            .Children(

                Cmd("Run")
                .StringExec(JsonRunAsync)
                .Usage("Json.Run <string CommandHead> [string Json]")
                .Description("Manually runs a Json command.")
                .Group("Base")
                .Build(),

                Cmd("Test")
                .StringExec(JsonTestAsync)
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
                .StringExec(ScriptRunAsync)
                .Usage("Script.Run <string filePath>")
                .Description("Runs a script from a file path. Tests first.")
                .Group("Base")
                .Children(

                    Cmd("Force")
                    .Aliases("Override")
                    .StringExec(ScriptRunForceAsync)
                    .Usage("Script.Run.Force <string filePath")
                    .Description("Runs a script from a file path without testing.")
                    .Group("Base")
                    .Build()

                    )
                .Build(),

                Cmd("Test")
                .StringExec(ScriptTestAsync)
                .Usage("Script.Test <string filePath>")
                .Description("Tests a script from a file path.")
                .Group("Base")
                .Build()

                )
            .Build(),

            Cmd("Clear")
            .Aliases("ClearScreen", "clr")
            .StringExec(Clear)
            .Description("Clears the screen (as long as ReqClear is set).")
            .Group("Base")
            .Build(),

            Cmd("Exit")
            .Aliases("ext", "quit", "Close", "ExitProgram", "CloseProgram")
            .StringExec(Exit)
            .Description("Closes the program (as long as ReqClear is set).")
            .Group("Base")
            .Build()
        ];

        private Task Help(ReplContext ctx, CommandArgs args, CancellationToken ct)
        {
            string searchKey = args.GetOr<string>(0, "");
            string mode = args.FirstOptionStart("-a", "-d", "-e", "-f", "-l", "-o", "-ol", "-u");
            switch (mode)
            {
                case "-a": HelpPrintshort(ctx, HelpAttribute.Aliases, searchKey, false, args.OptStrt("-g")); break;
                case "-d": HelpPrintshort(ctx, HelpAttribute.Description, searchKey, false, args.OptStrt("-g")); break;
                case "-e": HelpPrintshort(ctx, HelpAttribute.Examples, searchKey, false, args.OptStrt("-g")); break;
                case "-f": HelpPrintLong(ctx, searchKey); break;
                case "-l": HelpPrintshort(ctx, HelpAttribute.LongDescription, searchKey, false, args.OptStrt("-g")); break;
                case "-ol":
                case "-o": HelpPrintshort(ctx, HelpAttribute.Description, searchKey, true, args.OptStrt("-g")); break;
                case "-u": HelpPrintshort(ctx, HelpAttribute.Usage, searchKey, true, args.OptStrt("-g")); break;
                default:
                    if (string.IsNullOrWhiteSpace(searchKey)) HelpPrintshort(ctx, HelpAttribute.Description, "", true);
                    else HelpPrintLong(ctx, searchKey);
                    break;
            };
            return Task.CompletedTask;
        }

        private Task HelpTree(ReplContext ctx, CommandArgs args, CancellationToken ct)
        {
            ctx.WriteLine(ctx.BuildRootTree());
            return Task.CompletedTask;
        }

        private Task HelpAliases(ReplContext ctx, CommandArgs args, CancellationToken ct)
        {
            string inputKey = args.GetR<string>(0);
            string sk = inputKey.DotSeparated();

            // Filter
            var filtered = 
                ctx.AliasIndex
                .Where(it => it.Key.StartsWith(sk, StringComparison.OrdinalIgnoreCase))
                .OrderBy(it => it.Value.Address);

            int col = Math.Min(filtered.Max(kv => kv.Key.Length + kv.Value.Address.Length + 5), (ctx.OneLineMaxWidth - 10) / 2);

            StringBuilder sb = new();

            if (args.HasOptStart("-g"))
            {
                ctx.WriteLine("Not yet implemented.");
            }
            else
            {
                int count = 0;
                foreach(var it in filtered)
                {
                    sb.AppendLine(it.Key.ToIndexLine(it.Value.Address, col));
                    count++;
                }
                ctx.WriteLine($"Printing all commands and aliases{(string.IsNullOrWhiteSpace(sk) ? "" : sk)} ({count} total):\n");
                ctx.WriteLine(sb.ToString());

                ctx.WriteLine();
                string report = $"{count} total aliases{(string.IsNullOrWhiteSpace(sk) ? "" : $" starting with {sk} found")} for {ctx.SearchDictionary(sk).Count} commands.";
                ctx.WriteLine(report.ToBox(boxWidth: Math.Min(ctx.OneLineMaxWidth, report.Length + 20), vPadding: 1, hPadding: 10));
            }
            return Task.CompletedTask;
        }

        private void HelpPrintshort(ReplContext ctx, HelpAttribute help, string searchTerm = "", bool oneline = false, bool group = true)
        {
            List<ReplCommand> commands = ctx.SearchDictionary(searchTerm);
            int col1space = Math.Min(commands.Max(c => c.Address!.Length) + 3, 100);
            int col2space = Math.Max(ctx.OneLineMaxWidth - col1space, 0);

            ctx.WriteLine($"{(string.IsNullOrWhiteSpace(searchTerm) ? $"Printing all commands" : $"Printing all commands beginning with '{searchTerm}'")} ({commands.Count} total). Use 'Help <command>' for more information:".ToBox(vPadding: 1, hPadding: 4));

            if (!group)
            {
                ctx.WriteLine();
                foreach (ReplCommand c in commands) ctx.WriteLine(c.PrintShort(col1space, col2space, help, oneline));
                return;
            }

            // Seperate into groups:
            List<string?> groups = commands.DistinctBy(c => c.Group).Select(c => c.Group).OrderBy(s => s == "Base" ? 0 : s is null ? 2 : 1).ThenBy(s => s).ToList();

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
                ctx.WriteLine($"───[{(g ?? "Ungrouped")}:]" + new string('─', ctx.OneLineMaxWidth - 8 - (g ?? "Ungrouped").Length));
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
            int col = Math.Min(ctx.AliasIndex.Max(kv => kv.Key.Length + kv.Value.Address.Length + 5), (ctx.OneLineMaxWidth - 10) / 2);
            ctx.WriteLine("Printing all commands and aliases. Try 'help <command>` for more information:");
            ctx.WriteLine();
            foreach (var kv in ctx.AliasIndex.OrderBy(kv => kv.Value.Address)) ctx.WriteLine(kv.Key.ToIndexLine(kv.Value.Address, col));
                //ctx.WriteLine($"{kv.Key.Truncate(col) + new string('.', col - kv.Key.Length)}...{kv.Value.Address.Truncate(col)}");

            string report = $"Total of {ctx.AliasIndex.Count} total aliases for {ctx.SearchDictionary().Count} commands.";
            ctx.WriteLine();
            ctx.WriteLine(report.ToBox(boxWidth: Math.Min(ctx.OneLineMaxWidth, report.Length + 20), vPadding: 1, hPadding: 10));
            return Task.CompletedTask;
        }

        private async Task TestAsync(ReplContext ctx, CommandArgs args, CancellationToken ct)
        {
            Tokens tokens = args.GetR<string>(0).TokenizeParen();
            bool success = await ctx.TestAsync(tokens, ct);
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

        private Task Clear(ReplContext ctx, IReadOnlyList<string> args, CancellationToken ct)
        {
            ctx.ClearScreen();
            return Task.CompletedTask;
        }

        private Task Exit(ReplContext ctx, IReadOnlyList<string> args, CancellationToken ct)
        {
            ctx.CloseApplication("Request close from command Exit (Base).");
            return Task.CompletedTask;
        }
    }
}