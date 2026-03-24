# CCRepl
CCRepl is a C# class library for building REPL-style command systems. Commands are registered as a hierarchical tree, and can be executed via external string input, directly within the program, or by running scripts.

## Overview
CCRepl is designed to quickly and easily create interactive command environments, scripts, and automation tools.

The `Repl` class manages input parsing, prompts, command address assignment, command aliases, execution, and testing.

Commands are added by defining `ReplCommand` objects inside command sets implementing `ICommandSet`, which are provided to `Repl` during construction. 


## Features
- Hierarchical command structure (`command.subcommand`, `help.list`, &c.).
- Easy argument parsing and user input exceptions.
- Command aliases.
- Asynchronous command execution.
- Execution cancellation.
- Optional console function override to enable cancellation by keystroke.
- Options for JSON formatting of command arguments.
- Test methods for validating command inputs without executing them.
- Built-in input/output abstraction.
- Automatic command execution using scripts.

## Usage

### Setup
To set up a CCRepl command system:

1. Create the `Repl` object, assigning command sets as constructor parameters (these can be left blank initially). 
2. Assign `ReqWriteLine` and `ReqWrite`.
3. Assign `ReqInputAsync` handler.
4. Create input loop.

Here is an example for a simple console app:

```csharp
using CCRepl;

// Define Repl object, assign command sets:
Repl repl = new(
	new MyCommands(),
	// ...
);

// Assign input & output handlers:
repl.ReqWriteLine += msg => Console.WriteLine(msg);
repl.ReqWrite += msg => Console.Write(msg);
repl.ReqInputAsync = (prompt, ct) =>
{
	Console.WriteLine(prompt);
	Console.Write("> ");
	string input = Console.ReadLine() ?? "";
	return Task.FromResult(input);
};

while (true)
{
	Console.Write("> ");
	string? line = Console.ReadLine();
	if (string.IsNullOrWhiteSpace(line)) continue;
	await repl.ExecuteAsync(line);
}
``` 

Here is an example for a console app implementing `ConsoleInputEditor`, enabling command cancellation by pressing "Escape":

```csharp
using CCRepl;
using CCRepl.Tools;

Repl repl = new(...);
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

	using CancellationTokenSource cts = new();
	Task keyWatcher = InputHelpers.ConsoleCancelKeyWatcher(cts, ConsoleKey.Escape);

	try { await repl.ExecuteAsync(input, cts.Token); }
    catch (OperationCanceledException) { Console.WriteLine("Cancelled."); }
    finally
    {
        cts.Cancel();
        try { await keyWatcher; }
        catch (OperationCanceledException) { }
    }
}
```

### Command definition

Commands are defined in `ICommandSet`:

```csharp
public class MyCommands : ICommandSet
{
	public IReadOnlyList<ReplCommand> Commands =>
	[
		new(name: "MyCommand",
			executeAsync: MyCommandAsync
		)
	];

	private async Task MyCommandAsync(ReplContext ctx, IReadOnlyList<string> args, CancellationToken ct)
	{
		// ...
	}
}
```
See `ReplCommand` documentation for more details.

## Prerequisites
- .NET 8

## Project status
Personal project for use in other projects, still evolving. API may change as features are refined.