using CCRepl.Tools;
using CCRepl.Models;
using static CCRepl.Tools.CmdBuilder;
using static CCRepl.Scripting.ScriptParser;
using CCRepl.Scripting;

namespace CCRepl.CommandSets;

public sealed class BaseCommands : ICommandSet
{  
    public IReadOnlyList<ReplCommand> Commands =>
    [
        new ReplCommand(
            name: "Help",
            executeAsync: HelpAsync,
            aliases: ["h", "?"],
            usage: "help [string HelpAttribute] [string CommandHeads]",
            description: "Lists all commands and specified help attribute (description by default), or shows full help for all commands with specified CommandHeads.",
            examples:
            [
                "help", 
                "help Diary.Add",
                "help usage Diary.Add",
                "help usage",                
            ],
            longDescription: @"Sharp brackets <> denote required arguments. Round brackets () denote optional arguments.
Help command on its own lists all commands and descriptions. If there is one argument, if it can be parsed as a help attribute, it will dieplay that attribute.
Help attirbutes include: Aliases, Usage, Description, Examples, LongDescription.
Otherwise, if it cannot be parsed as an attribute, it will interpret the argument as a CommandHead, and will show full help for all commands with those CommandHead elements.
If two arguments are given, the first argument will be interpreted as a Help Attribute, and the second argument will be interpreted as a CommandHead.",
            group: "Base",
            children:
            [
                new ReplCommand(
                    name: "List",
                    executeAsync: HelpListAsync,
                    usage: "list [string HelpAttribute] [string CommandHeads]",
                    description: "List all commands and specified help attribute (description by default), or just for all commands with specified CommandHeads.",
                    group: "Base"
                ),
                new ReplCommand(
                    name: "Full",
                    executeAsync: HelpFullAsync,
                    usage: "Help.Full [string CommandHeads]",
                    description: "Show full help for all commands, or all commands with specified CommandHeads.",
                    group: "Base"
                )
            ]            
        ),

        new ReplCommand(
            name: "Commands",
            executeAsync: CommandsAsync,
            aliases: ["CommandList", "cmd", "Command"],
            description: "List all commands",
            group: "Base",
            children:
            [
                new ReplCommand(
                    name: "Aliases",
                    executeAsync: CommandAliasesAsync,
                    aliases: ["All"],
                    description: "Lists all commands and aliases for each command.",
                    group: "Base"
                )
            ]
        ),

        new ReplCommand(
            name: "Test",
            executeAsync: TestAsync,
            usage: "Tast <Command> (arguments ... )",
            description: "Runs the TestAsync method on specified command with specified arguments.",
            group: "Base"
        ),

        Cmd("Json").Description("Commands for manual use of Json Commands").Group("Base").Children(
            Cmd("Run").Exec(RunJson).Description("Run a Json Command")
                .Usage("Json.Run <string CommandHead>")
                .Group("Base")
                .Build(),
            Cmd("Test").Exec(TestJson).Description("Tests a JsonCommand")
                .Usage("Json.Test <string CommandHead>")
                .Group("Base")
                .Build()
        ).Build(),

        Cmd("Script").Description("Commands for running scripts.").Group("Base").Children(
            Cmd("Run").Exec(ScriptTestAndRunAsync).Description("Runs a script from a file path. Tests first.")
                .Usage("Script.Run <string FilePath>")
                .Group("Base")
                .Children(
                    Cmd("Force").Exec(ScriptRunAsync).Description("Runs a script from a file path without testing first.").Build()
                ).Build(),
            Cmd("Test").Exec(ScriptTestAsync).Description("Tests a script from a file path.")
                .Usage("Script.Test <string FilePath>")
                .Group("Base")
                .Build()
        ).Build()
    ];   

    private Task HelpAsync(ReplContext ctx, IReadOnlyList<string> args, CancellationToken ct)
    {
        if (args.Count == 0)
        {
            ctx.WriteLine("Printing all commands. Try 'help <Command>' for more information:");
            PrintShort(ctx, HelpAttribute.Description);
        }
        else if (args.Count == 1)
        {
            string arg1 = args.String(0, "Help Attribute/Command Head");
            if (!Enum.TryParse<HelpAttribute>(arg1, true, out HelpAttribute help))
            {
                ctx.WriteLine($"Printing all commands starting with '{arg1}':");
                PrintLong(ctx, arg1);
            }
            else
            {
                ctx.WriteLine($"Printing all commands and corresponding {help.ToString()}:");
                PrintShort(ctx, help);
            }
        }
        else
        {
            string helpStr = args.String(0, "HelpAttribute");
            string headStr = args.String(1, "CommandHead");
            HelpAttribute helpAtt = Enum.Parse<HelpAttribute>(helpStr);
            ctx.WriteLine($"Printing all commands starting with '{helpStr}' and corresponding {helpAtt.ToString()}:");
            PrintShort(ctx, helpAtt, headStr, false);            
        }
        return Task.CompletedTask;

    }

    private Task HelpListAsync(ReplContext ctx, IReadOnlyList<string> args, CancellationToken ct)
    {
        if (args.Count == 0)
        {
            ctx.WriteLine("Printing all commands. Try 'help <Command>' for more information:");
            PrintShort(ctx, HelpAttribute.Description, "", true);            
        }
        else if (args.Count == 1)
        {
            string arg1 = args.String(0, "Help Attribute/Command Head");
            if (!Enum.TryParse<HelpAttribute>(arg1, true, out HelpAttribute help))
            {
                ctx.WriteLine($"Printing all commands starting with '{arg1}':");
                PrintShort(ctx, HelpAttribute.Description, arg1, true);                
            }
            else
            {
                ctx.WriteLine($"Printing all commands and corresponding {help.ToString()}:");
                PrintShort(ctx, help, "", true);
            }            
        }
        else
        {
            string helpStr = args.String(0, "HelpAttribute");
            string headStr = args.String(1, "CommandHead");
            HelpAttribute helpAtt = Enum.Parse<HelpAttribute>(helpStr);
            ctx.WriteLine($"Printing all commands starting with '{helpStr}' and corresponding {helpAtt.ToString()}:");
            PrintShort(ctx, helpAtt, headStr, true);
        }
        return Task.CompletedTask;
    }

    private void PrintShort(ReplContext ctx, HelpAttribute help, string searchTerm = "", bool oneline = false)
    {
        ctx.WriteLine("");
        List<ReplCommand> commands = ctx.SearchDictionary(searchTerm);
        int col1space = Math.Min(commands.Max(c => c.Address!.Length) + 3, 100);
        int col2space = Math.Max(ctx.OneLineMaxWidth - col1space, 0);
        foreach (ReplCommand c in commands) ctx.WriteLine(c.PrintShort(col1space, col2space, help, oneline));
    }

    private Task HelpFullAsync(ReplContext ctx, IReadOnlyList<string> args, CancellationToken ct)
    {
        if (args.Count == 0)
        {
            ctx.WriteLine("Printing full information for all commands:");
            PrintLong(ctx);
        }
        else
        {
            string searchTerm = args.String(0, "Search Term");
            ctx.WriteLine($"Printing full information for all commands starting with {searchTerm}:");
            PrintLong(ctx, searchTerm);

        }
        return Task.CompletedTask;
    }

    private void PrintLong(ReplContext ctx, string searchTerm = "")
    {
        ctx.WriteLine("");
        List<ReplCommand> commands = ctx.SearchDictionary(searchTerm);
        foreach (ReplCommand c in commands) ctx.WriteLine(c.PrintLong());
    }

    private Task CommandsAsync(ReplContext ctx, IReadOnlyList<string> args, CancellationToken ct)
    {
        ctx.WriteLine("Printing all commands. Try 'help <command>' for more information:");
        List<ReplCommand> orderedCommands = ctx.SearchDictionary();
        foreach (ReplCommand c in orderedCommands) ctx.WriteLine(c.Address!);
        return Task.CompletedTask;
    }

    private Task CommandAliasesAsync(ReplContext ctx, IReadOnlyList<string> args, CancellationToken ct)
    {
        int col = Math.Min(ctx.AliasIndex.Max(kv => kv.Key.Length), (ctx.OneLineMaxWidth - 10) / 2);
        ctx.WriteLine("Printing all commands and aliases. Try 'help <command> for more information:");
        ctx.WriteLine("");
        foreach (var kv in ctx.AliasIndex.OrderBy(kv => kv.Value.Address)) ctx.WriteLine($"{kv.Key.Truncate(col).PadRight(col)} ({kv.Value.Address.Truncate(col)})");
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

    private async Task RunJson(ReplContext ctx, IReadOnlyList<string> args, CancellationToken ct)
    {
        string commandHead = args.String(0, "CommandHead");
        string json = await ctx.ReadLineAsync("Please enter Json argument:", ct);
        await ctx.ExecuteJsonAsync(commandHead, json);
    }

    private async Task TestJson(ReplContext ctx, IReadOnlyList<string> args, CancellationToken ct)
    {
        string commandHead = args[0];
        string json = await ctx.ReadLineAsync("Please enter Json argument:", ct);
        bool success = await ctx.TestJsonAsync(commandHead, json, ct);
        if (success) ctx.WriteLine($"No issues found: '{string.Join(' ', args)}'.");
        else ctx.WriteLine($"Failed test: '{string.Join(' ', args)}'.");
    }

    private async Task ScriptRunAsync(ReplContext ctx, IReadOnlyList<string> args, CancellationToken ct)
    {
        string path = args.StringOrNull(0, "FilePath") ?? await ctx.ReadLineAsync("Please enter filepath:", ct);
        ctx.WriteLine($"Parsing file '{Path.GetFileName(path)}'...");
        Script script = await ctx.WithWaiterAsync(_ => Task.Run(() => FromPath(path)), "Parsing Script ", "", "Parsed.", 100, ct, WaitAnimation.Spinner);
        await ctx.ExecuteScriptAsync(script, ct);
    }

    private async Task ScriptTestAsync(ReplContext ctx, IReadOnlyList<string> args, CancellationToken ct)
    {
        string path = args.StringOrNull(0, "File Path") ?? await ctx.ReadLineAsync("Please enter filepath:", ct);
        ctx.WriteLine($"Parsing file '{Path.GetFileName(path)}'...");
        Script script = await ctx.WithWaiterAsync(_ => Task.Run(() => FromPath(path)), "Parsing Script ", "", "Parsed.", 100, ct, WaitAnimation.Spinner);
        await ctx.TestScriptAsync(script, ct);
    }

    private async Task ScriptTestAndRunAsync(ReplContext ctx, IReadOnlyList<string> args, CancellationToken ct)
    {
        string path = args.StringOrNull(0, "File Path") ?? await ctx.ReadLineAsync("Please enter filepath:", ct);
        ctx.WriteLine($"Parsing file '{Path.GetFileName(path)}'...");
        Script script = await ctx.WithWaiterAsync(_ => Task.Run(() => FromPath(path)), "Parsing Script ", "", "Parsed.", 100, ct, WaitAnimation.Spinner);
        if (await ctx.TestScriptAsync(script, ct)) await ctx.ExecuteScriptAsync(script, ct);
    }
}