using CCRepl;
using CCRepl.Cli;
using CCRepl.Example;

Repl repl = new(new SampleCommands(), new CounterCommands());
repl.ReqWriteLine += msg => Console.WriteLine(msg);
repl.ReqWrite += msg => Console.Write(msg);
repl.ReqInputAsync = (prompt, ct) =>
{
    Console.WriteLine(prompt);
    Console.Write("> ");
    string input = Console.ReadLine() ?? "";
    return Task.FromResult(input);
};

Console.WriteLine("CCRepl CLI. Type 'exit' to quit.");

while (true)
{
    Console.Write("> ");
    string? line = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(line)) continue;
    if (line.Equals("exit", StringComparison.OrdinalIgnoreCase)) break;
    await repl.ExecuteAsync(line);
}




