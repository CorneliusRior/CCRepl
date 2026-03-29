using CCRepl;
using CCRepl.Tools;
using ReadingList.Commands;
using ReadingList.Services;

string dataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CCRepl_ReadingList");
Directory.CreateDirectory(dataDir);
string dataPath = Path.Combine(dataDir, "ReadingList.db");
MediaService service = new($"Data Source={dataPath}");

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
    
    // Receive input, process before handing it over to repl:
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