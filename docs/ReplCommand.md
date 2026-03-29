# ReplCommand

The `ReplCommand` class is the building block of a CCRepl command system. Commands are defined inside command sets implementing `ICommandSet`, and provided to `Repl` during construction.

The `ReplCommand` has only one required property, `Name`. When `Repl` is built, each assigned command is automatically given an address `Address`, structured like `parent.command.child`, which we call the "Command Head". The command can then be called by inputting the command head, followed by arguments separated by spaces, stored in an IReadOnlyList\<string\> `args`.

A `ReplCommand` can be defined using either the class constructor, or `CommandBuilder`. To define a command which can take JSON input, use `CommandBuilder`.

## Properties:

- `Name`: Canonical name of the command. The only required property. Address is assigned based on this parameter. Name must be unique in its respective level and have no siblings with identical names. 
- `Address`:
String by which this command is called. Consists of the root command's name followed by descendant names seperated by '.' (e.g. `Help.List`).
- `Aliases`: A list of aliases which can be used instead of `Name`. Canonical name takes priority in all searches. Every alias must be unique in its respective level and have no siblings with identical aliases.
- `Children`: Child commands of this command which are given `AddressString` "ThisName.ChildName".
- `ExecuteAsync`: Main execution method of the command. Method does not need to be defined in the same command set class. Methods must return `Task`, and accept arguments `(ReplContext ctx, IReadOnlyList<string> args, CancellationToken ct)`. They do not need to be `async`.
- `CanExecute`: Returns true if `ExecuteAsync` is not null.
- `TestAsync`: Testing method of the command. Methods must return `Task<bool>` and accept the same arguments as `ExecuteAsync`: They do not need to be `async`. These should be constructed to parse arguments identically `ExecuteAsync`, and further validation can be made too.
- `CanTest`: Returns true if `TestAsync` is not null.
- `ExecuteJsonAsync`: Execution method which takes JSON formatting of inputs. These should not require additional input once executed, but should otherwise be identical to `ExecuteAsync` if defined. Scripts run using `ExecuteJsonAsync`. `ExecuteJsonAsync` can only be defined using `CommandBuilder`.
- `CanExecuteJson`: Returns true if `ExecuteJsonAsync` is not null.
- `TestJsonAsync`: Testing method for `ExecuteJsonAsync`. Due to the nature of JSON formatting, input validation is done prior, so it is not necessary to define `TestJsonAsync` unless further logic is necessary. By default, each statement in a script must be tested and return true before it runs.
- `CanTestJson`: Returns true if `TestJsonAsync` is not null.
- `JsonPayloadType`: Definition of payload object, record used to parse JSON arguments. Should follow format of ExecuteAsync if defined.
- `Usage`: String showing how the command is used. Format as "Command.Head \<type RequiredArgument\> [type OptionalArgument]".
- `Description`: String describing what this command does. This is the default "Help" parameter shown when the `help` command is called.
- `Examples`: Examples showing how to use the command.
- `LongDescription`: String describing the command.

## Methods:
- `ReplCommand.PrintShort(int col1space, intcol2space, HelpAttribute help, bool oneline = true)`: Prints the command address and specified help parameter. `HelpAttribute` is an Enum, with options [ Aliases, Usage, Description, Examples, LongDescription ]. Truncates to specified column length and down to one line if `oneline` is set true.
- `ReplCommand.PrintLong()`: Prints all help parameters. Shown when `help` command is called on a specific command, or `help.full` is called.

## Formatting:
CCRepl internal systems are generally case insensitive, so names and aliases can be written in any way. However, for the sake of consistency, write names and aliases starting with a capital letter. Canonical names should only contain letters, no numbers or symbols. 

## Implementation:
Create an implementation of `ICommandSet`, and add new command(s) to the list `Commands`. You can create placeholder commands with just `Name`, these will be registered in `Repl` and will be listed in `help` and `commands` commands, but will have no functionality.

This can be done with the constructor defined in the `ReplCommand` class:

```csharp
public class MyCommands : ICommandSet
{
	public IReadOnlyList<ReplCommand> Commands =>
	[
		new(name: "MyCommand",
			children:
			[
				new(name: "MySubCommand")
			]
		)
	];
}
```

or by using CommandBuilder:

```csharp
using static CmdBuilder;

public class MyCommands : ICommandSet
{
	public IReadOnlyList<ReplCommand> Commands =>
	[
		Cmd("MyCommand").Children(
			Cmd("MySubCommand").Build()
		)
		.Build()
	];
}
```

It is recommended that you at least add a description, and usage if this is intended to be an executable command instead of a command category.

To make an executable command, assign a suitable method to `ExecuteAsync`. Arguments are passed in the form of the IReadOnlyList\<string\> `args`. These can be parsed with `ArgumentHelpers` extensions, which can handle varying argument counts, parsing and input errors. A `TestAsync` method can be made just by wrapping the input in `try { }`, returning false if a `ReplUserException` is caught, true otherwise:

```csharp
public class MyCommands : ICommandSet
{
	public IReadOnlyList<ReplCommand> Commands =>
	[
		new(name: "MyCommand",
			executeAsync: MyCommandAsync,
			testAsync: MyCommandTestAsync,
			usage: "MyCommand <int MyInt> <string MyString> [bool MyBool] [double? MyDouble]"
	];

	private Task MyCommandAsync(ReplContext ctx, IReadOnlyList<string> args, CancellationToken ct)
	{
		string myString		= args.String(1, "My String");			// Throws exception if absent
		int myInt			= args.Int(0, "My Int");				// Throws exception if absent or cannot parse
		bool myBool			= args.BoolOr(2, "My Bool", true);		// Returns default if absent
		double? myDouble	= args.DoubleOrNull(3, "My Double");	// Returns null if absent
		// ...
	}

	private Task<bool> MyCommandTestAsync(ReplContext ctx, IReadOnlyList<string> args, CancellationToken ct)
	{
		try
		{
			string myString		= args.String(1, "My String");
			int myInt			= args.Int(0, "My Int");		
			bool myBool			= args.BoolOr(2, "My Bool", true);	
			double? myDouble	= args.DoubleOrNull(3, "My Double");	
		}
		catch (ReplUserException) { return false; }
		return true;
	}
}
```

With `CommandBuilder`, this command would be defined like:

```csharp
Cmd("MyCommand")
	.Exec(MyCommandAsync).Test(MyCommandTestAsync)
	.Usage("MyCommand <int MyInt> <string MyString> [bool MyBool] [double? MyDouble]")
.Build()
```

### JSON Formatting:
Commands can accept arguments in JSON format by defining `ExecuteJsonAsync`. This must be done using `CommandBuilder`. JSON formatting is used for scripting.

```csharp
using static CCRepl.Tools.CmdBuilder;

public class MyCommands : ICommandSet
{
	public IReadOnlyList<ReplCommand> Commands =>
	[
		Cmd("MyCommand").ExecJson<MyCommandPayload>(MyCommandJsonAsync).Build()
	];

	private Task MyCommandJsonAsync(ReplContext ctx, MyCommandPayload pl, CancellationToken ct)
	{
		// ...
	}

	private sealed record MyCommandPayload(string MyString, int MyInt, bool MyBool, double? MyDouble);
}
```

This code has the same functionality as the previous code block showing the definitions of `ExecuteAsync` and `TestAsync` - there is no need for input validation, as it is validated on input. `TestJsonAsync` can be added if further validation or testing is needed, but this should be reserved for exceptional cases.

### Prompts:
Continuous input can be given using an `async` execution method and `ReplContext` methods. Useful methods include:

- `ReplContext.ConfirmAsync()`: (Y/N) confirmation.
- `ReplContext.ConfirmRequireAsync()`: (Y/N) confirmation which loops if not parsed.
- `ReplContext.RequestAsync()`: Request entry of a certain type.
- `ReplContext.RequireAsync()`: Request entry of a certain type and loops if not parsed or cancelled.

```csharp
private async Task MyPromptAsync(ReplContext ctx, IReadOnlyList<string> args, CancellationToken ct)
{
	string myString = await ctx.ReadLineAsync("Enter MyString: ", ct); // Accepts regardless.
	
	int myInt = await ctx.RequireAsync(				
		ct,											// Will not accept any answer which cannot be parsed
		"Enter MyInt: "
		s => (int.TryParse(s, out int v), v),
		"Could not parse, please try again."
		);

	bool myBool = await ctx.ConfirmAsync(ct, "Enter MyBool: ", true) // returns fallback: "true" if cannot parse

	double myDouble = await ctx.RequireAsync(
		ct,											// Will not accept any answer which cannot be parsed,
		"Enter MyDouble: "							// but will revert to default "0" if one of the 
		s => (double.TryParse(s, out double v), v),	// specified "defaultStrings" is entered.
		"Could not parse, please try again.",
		0, "Default", "Fallback", "Zero")
}
```

