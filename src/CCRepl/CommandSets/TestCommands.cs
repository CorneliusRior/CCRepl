using CCRepl.Models;
using CCRepl.Tools;
using static CCRepl.Tools.CmdBuilder;

namespace CCRepl
{
    public sealed class TestCommands : ICommandSet
    {
        public IReadOnlyList<ReplCommand> Commands =>
        [
            Cmd("NewExecute")
            .Aliases("newexex", "newex", "argexec", "commandargexec")
            .StringExec(NewExecute)
            .Description("Testing new execution method, using CommandArg arguments instead of string lists.")
            .Group("Test")
            .Children(
                
                Cmd("TestCmd")
                .Aliases("parsecmd", "cmd")
                .NewExec(NewExecuteTestCmd)
                .Args(
                    IntArg("A"),
                    DblArg("B", ArgMode.RequiredPrompt),
                    StrArg("C", ArgMode.Optional, "None")
                    )
                .Group("Test")
                .Build()

                )
            .Build()
        ];

        private async Task NewExecute(ReplContext ctx, IReadOnlyList<string> args, CancellationToken ct)
        {
            // Tokenize:
            string input = await ctx.ReadLineAsync("Enter command in new format: ", ct);
            Tokens tk = input.TokenizeParen();

            // Present:
            {
                ctx.WriteLine();
                ctx.Write("CommandHead: ");
                ctx.WriteLine(tk.CommandHead);

                ctx.WriteLine();
                ctx.WriteLine(tk.ArgStrings.PrintVec());

                ctx.WriteLine();
                ctx.WriteLine(tk.Options.PrintVec());
            }

            CommandArgs cmdArgs = new(ctx, tk, ct);
            ctx.WriteLine(cmdArgs.PrintInfo());

            ctx.WriteLine("\nTrying to execute:");
            await NewExecuteTestCmd(ctx, cmdArgs, ct);
        }

        private async Task NewExecuteTestCmd(ReplContext ctx, CommandArgs args, CancellationToken ct)
        {
            ctx.WriteLine("Successfully executed.");
            ctx.WriteLine($"A: {args.GetRequired<int>(0)}");
            ctx.WriteLine($"B: {args.Get<double>(1)}");
            ctx.WriteLine($"C: {args.Get<string>(2)}");
        }

    }
}
