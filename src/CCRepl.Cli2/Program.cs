using CCRepl;
using CCRepl.Cli2;
using CCRepl.Tools;

using System.Text;
using System.Threading;

Repl repl = new(new SampleCommands());
repl.ReqWriteLine += msg => Console.WriteLine(msg);
repl.ReqWrite += msg => Console.Write(msg);

List<string> history = [];

repl.ReqInputAsync = async (prompt, ct) =>
{
    Console.WriteLine(prompt);
    ConsoleInputEditor editor = new("> ", history);
    ConsoleResult result = await editor.ReadLineAsync(ct);
    if (result.Cancelled) throw new OperationCanceledException(ct);
    return result.Text;
};

Console.WriteLine("CCRepl CLI 2. Type 'exit' to quit");
bool exit = false;

while (!exit)
{
    ConsoleInputEditor editor = new("> ", history);
    ConsoleResult result = await editor.ReadLineAsync();
    if (result.Cancelled)
    {
        exit = true;
        continue;
    }
    string input = result.Text;
    if (string.IsNullOrWhiteSpace(input)) continue;
    if (input.Equals("exit", StringComparison.OrdinalIgnoreCase))
    {
        exit = true;
        continue;
    }
    if (input.Equals("clear", StringComparison.OrdinalIgnoreCase))
    {
        Console.Clear();
        continue;
    }

    using CancellationTokenSource cts = new();
    Task keyWatcher = InputHelpers.ConsoleCancelKeyWatcher(cts);

    try { await repl.ExecuteAsync(input, cts.Token); }
    catch (OperationCanceledException) { Console.WriteLine("Cancelled."); }
    finally
    {
        cts.Cancel();
        try { await keyWatcher; }
        catch (OperationCanceledException) { }
    }
}

Console.WriteLine("Exiting.");


