using CCRepl;
using CCRepl.Cli;
using CCRepl.Example;

Repl repl = new(new SampleCommands(), new CounterCommands(), new TestCommands());
bool running = true;
repl.ReqClose += msg => { running = false; };
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

while (running)
{
    Console.Write("> ");
    string? line = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(line)) continue;    
    await repl.ExecuteAsync(line);
}




