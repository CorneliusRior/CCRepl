using CCRepl;
using CCRepl.Cli;
using CCRepl.Example;

Repl repl = new(new SampleCommands(), new CounterCommands());
repl.OutputRequested += msg => Console.WriteLine(msg);
repl.InlineOutputRequested += msg => Console.Write(msg);
repl.InputRequestedAsync = prompt =>
{
    Console.WriteLine(prompt);
    Console.Write("> ");
    string input = Console.ReadLine() ?? "";
    return Task.FromResult(input);
};

Console.WriteLine("Puppet CLI. Type 'exit' to quit.");

while (true)
{
    Console.Write("> ");
    string? line = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(line)) continue;
    if (line.Equals("exit", StringComparison.OrdinalIgnoreCase)) break;
    await repl.ExecuteAsync(line);
}




