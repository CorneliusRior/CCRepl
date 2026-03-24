# CCRepl
CCRepl is a  C# class library for building REPL-style command systems. Commands are registered as a hierarchical tree, and can be executed via external string input, directly within the program, or by running scripts.

## Overview
CCRepl is designed to quickly and easily create interactive command environments, scripts, and automation tools.

The `Repl` class manages input parsing, prompts, command address assignment, command aliases, execution, and testing.

Commands are added by defining `ReplCommand` objects inside command sets implementing `ICommandSet`, which are provided to `Repl` during construction. 


## Features
- Hierarchical command structure (`command.subcommand`, `help.list`, &c.).
- Command aliases.
- Asynchronous command execution.
- Options for JSON formatting of command arguments.
- Test methods for validating command inputs without executing them.
- Built-in input/output abstraction.
- Easy argument parsing and user input exceptions.
- Automatic command execution using scripts.

## Usage

### Setup
To set up a CCRepl command system:

1. Create the `Repl` object, assigning command sets as constructor parameters (these can be left blank initially). 
2. Assign input & output handlers. 
3. Create input method.

Here is an example for a simple console app:

```csharp
// Define Repl object, assign commandsets:
Repl repl = new(
	new MyCommands(),
	// ...
);

// Assign input & output handlers:
repl.OutputRequested += msg => Console.WriteLine(msg);
repl.InlineOutputRequested += msg => Console.Write(msg);
repl.InputRequestedAsync = prompt =>
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

### Command definition

Commands are defined in `ICommandSet`:

```csharp
public class MyCommands : ICommandSet
{
	public IReadOnlyList<ReplCommand> Commands =>
	[
		new(name "MyCommand"
			executeAsync: MyCommandAsync
		)
	];

	private Task MyCommandAsync(ReplContext ctx, IReadOnlyList<string> args, CancellationToken ct)
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