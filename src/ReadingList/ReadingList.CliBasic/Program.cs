using CCRepl;
using CCRepl.Models;
using ReadingList.Commands;
using ReadingList.Services;

// Define file path and service:
string dataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CCRepl_ReadingList");
Directory.CreateDirectory(dataDir);
string dataPath = Path.Combine(dataDir, "ReadingList.db");
MediaService service = new($"Data Source={dataPath}");

try
{
    // Define Repl object w/ command set.
    Repl repl = new(new MediaCommands(service));

    // Assign input & output handlers:
    repl.ReqWrite += msg => Console.Write(msg);
    repl.ReqWriteLine += msg => Console.WriteLine(msg);
    repl.ReqInputAsync = (prompt, ct) =>
    {
        Console.WriteLine(prompt);
        Console.Write("> ");
        string input = Console.ReadLine() ?? "";
        return Task.FromResult(input);
    };

    // Print startup message:
    Console.WriteLine("CCRepl ReadingList.");
    Console.WriteLine("Type 'help' for commands.");
    Console.WriteLine("Type 'clear' to clear screen.");

    // Main input loop:
    while (true)
    {
        Console.Write("> ");
        string? line = Console.ReadLine();

        // Receive input, process before handing it over to repl:
        if (string.IsNullOrWhiteSpace(line)) continue;
        if (line.Equals("exit", StringComparison.OrdinalIgnoreCase)) break;
        if (line.Equals("clear", StringComparison.OrdinalIgnoreCase)) Console.Clear();
        await repl.ExecuteAsync(line);
    }
}
catch (ReplException ex) { Console.WriteLine($"{ex.Location} {ex.Message}"); }
finally { Console.WriteLine("Exiting..."); }