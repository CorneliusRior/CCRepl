using Puppet.Cli2;
using Puppet.Tools;

using System.Text;
using System.Threading;

Puppet.Puppet puppet = new(new SampleCommands());
puppet.OutputRequested += msg => Console.WriteLine(msg);
puppet.InlineOutputRequested += msg => Console.Write(msg);

List<string> history = [];

puppet.InputRequestedCancelableAsync = async (prompt, ct) =>
{
    Console.WriteLine(prompt);
    ConsoleInputEditor editor = new("> ", history);
    ConsoleInput result = await editor.ReadLineAsync(ct);
    if (result.Cancelled) throw new OperationCanceledException(ct);
    return result.Text;
};

Console.WriteLine("Puppet CLI 2. Type 'exit' to quit");
bool exit = false;

while (!exit)
{
    ConsoleInputEditor editor = new("> ", history);
    ConsoleInput result = await editor.ReadLineAsync();
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
    Task keyWatcher = Task.Run(async () =>
    {
        while (!cts.IsCancellationRequested)
        {
            if (!Console.KeyAvailable)
            {
                await Task.Delay(25);
                continue;
            }

            ConsoleKeyInfo k = Console.ReadKey(intercept: true);
            if (k.Key == ConsoleKey.Escape)
            {
                cts.Cancel();
                break;
            }
        }
    });

    try { await puppet.ExecuteAsync(input, cts.Token); }
    catch (OperationCanceledException) { Console.WriteLine("Cancelled."); }
    finally
    {
        cts.Cancel();
        try { await keyWatcher; }
        catch (OperationCanceledException) { }
    }
}

Console.WriteLine("Exiting.");


