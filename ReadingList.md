# Reading List

## Overview

ReadingList is a basic console application built with CCRepl. It consists of a `Media` model, SQL service for persistent storage of `Media` entries, and a command set. Despite the name of the program, "Media" can refer to any kind of media. Media entries can contain information about the title itself, information about progress reading (or otherwise consuming) entries as well as notes and a rating system.

This was built so as to serve as a working example for more complete documentation. This file will go through the implementation of `CCRepl` features used to build the program. The code should be available to see as well. I will try to limit examples to more unique features of `CCRepl` which require explanation and not get into more basic C# logic.

## Implementation

There are two different Console Applications based on the `ReadingList` class library.

- `ReadingList.Cli` uses `ConsoleLineEditor` to handle input, which enables asynchronous cancellation (by pressing 'escape')
- `ReadingList.CliBasic` uses the default console input, which is more stable, but without cancellation capability.

In future I will make a UI using WPF which can have the flexibility and stability of both. Generally I would recommend using `ConsoleLineEditor`, but have made both for the sake of example.

Implementation of all CCRepl systems consists of assigning handlers for input and output, then defining the main input loop, these are two slightly different approaches to that:

### ReadingList.CliBasic

After defining a `MediaService` Class required for `MediaCommands` to function:

```csharp

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
    // Await input:
    Console.Write("> ");
    string? line = Console.ReadLine();

    // Recieve input, process before handing it over to repl:
    if (string.IsNullOrWhiteSpace(line)) continue;
    if (line.Equals("exit", StringComparison.OrdinalIgnoreCase)) break;
    if (line.Equals("clear", StringComparison.OrdinalIgnoreCase)) Console.Clear();

    // Execute:
    await repl.ExecuteAsync(line);
}

// Print exit message:
Console.WriteLine("Exiting...");

```

REPL systems will likely follow this pattern, though `CCRepl` systems can function so long as there's an execution method (such as `ExecuteAsync()`, but you could also use `ExecuteJsonAsync()` or `ExecuteScriptAsync()` instead) and some way to provide input from a prompt to `ReqInputAsync`.

### ReadingList.Cli

Implementation with `ConsoleInputEditor` is more flexible, allows for keystroke cancellation, but is less stable, as I needed to rebuild the typing system inside an existing structure. This does indeed function, but there are some quirks, quickly resizing the window can crash the program due to cursor placement.

```csharp

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
    
    // Recieve input, process before handing it over to repl:
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

```

Implementation differs from the basic version in a couple of places:

- History needs to be defined manually (Not done here, but persistent storage could be used to import history from previous sessions).
- Input is recieved with `ConsoleInputEditor` in the form of a `ConsoleResult` which also carries information about cancellation (and perhaps more in future).
- Cancellation handling is required.
- Implementation of `ConsoleCancelKeyWatcher` to detect cancellation keystrokes.

### Checklist

In general, the outer IO layer requires the following:

1. Handler for `ReqWrite` ("Request Write").
2. Handler for `ReqWriteLine`.
3. Handler for `ReqInputAsync` which returns a `string`.
4. Input loop

If using a different platform with other input methods, keywatchers are not necessary, nor disposal.

## Adding Commands

To add commands to the Repl system, define `ReplCommands` using either the `ReplCommand` constructor or `CmdBuilder` (recommended), and put them in the `IReadOnlyList<ReplCommand> Commands` of a class implemented `ICommandSet`. This defines the name and address of the command, and what to do when the command is called.

```csharp

public partial class MediaCommands : ICommandSet
{
    private MediaService _service;
    
    public MediaCommands(MediaService service)
    {
        _service = Service;
    }

    public IReadOnlyList<ReplCommand> Commands =>
    [
        Cmd("Media)
            // ...
            .Build()
    ];
}

```

- Since command sets are just an implementation of an interface, they are flexible and can implement any properties we want.
- In these examples, commands are defined inside the `Commands` list but they can be defined elsewhere.
- In reading list, we defined `MediaCommands` in partial classes, this is not necessary.
- Commands are defined in a hierarchical structure. There is no technical reason not to use single-headed commands such as if you want, simply define each command in series without implementing hierarchy with `Children()`.
- Commands do not need to define handlers. The only manadatory property is "Name". "Nodal" commands without execution capability can be used to compartmentalise commands.
- No two commands can have the same name if they are both root commands or share a parent, the Repl will not build if there is such a conflict. 
- Handler methods do not necessarily need to be defined in the same class, they only need to take arguments `(ReplContext, IReadOnlyList<string>, CancellationToken)` and return `Task`.
- Multiple commands can use the same handlers in different addresses. Presently there is no way to alter command behavious depending on where it was called.
- Commands can be called with any combination of aliases. Note that every combination of alias is defined upon construction, this can be seen with the base command `Commands.Aliases`. The alias dictionary can increase in size exponentially which could be a concern for larger systems.


Here we will go through the process of implementing some commands.

## Add Command

A system which administers data should have the following commands:

- Add
- Delete
- Edit
- List
- View

Here is how we define the root command `Media`, commands `Media.Add` and `Media.Add.Prompt` :

```csharp
public IReadOnlyList<ReplCommand> Commands =>
[
    Cmd("Media")
        .Aliases("m", "md", "Read", "ReadingList", "rd", "rdl")
        .Description("Commands for interacting with media items.")
        .Children
        (
            Cmd("Add")
                .Aliases("a", "+", "New", "nw", "AddNew")
                .Exec(MediaAdd)
                .Test(MediaAddTest)
                .ExecJson<MediaAddPayload>(MediaAdd)
                .TestJson<MediaAddPayload>(MediaAddTest)
                .Usage("Media.Add <string Title> <string Type> <string Status> [int Release Year] [string Genre] [DateTime StartedOn] [DateTime FinishedOn] [string ProgressNote] [string Notes] [double Rating]")
                .Description("Add a new piece of media to the list.")
                .AddExample("Media.Add Thunderbirds Show InProgress 1965 _ \"Gerry Anderson\" 07-03-2026 _ \"Episode 23\" _ 10")
                .AddExample("Media.Add \"Moby Dick\" Book Dropped 1851 _ \"Herman Melville\" _ _ _ \"Gave up, too boring, not as good as Thunderbirds (1965)\" 2.5")
                .AddExample("m.+ \"Romance of the Three Kingdoms\" Book InProgress _ Romance \"Attributed to Luo GuangZhong\" 01-01-2026 _ \"Chapter 64\"")                        
                .Children
                (
                    Cmd("Prompt")
                        .Aliases("p", "pmpt", "pmt", "async")
                        .Description("Adds a new piece of media through input prompts.")
                        .Exec(MediaAddPromptAsync)
                        .Build()

                )
                .Build(),

    // ...
];
                    

```

### Media (Root Command)

- The `Media` root command does not have any execution functions - this is an optional parameter which can be used to make "nodal" commands which have no function and are instead used to organise other commands.
- I recommend giving each root command a one-letter alias, as these are used frequently.
- There is nothing special about root commands, you do not need to make them, it is just a structural decision.

### Media.Add

Basic add function which uses arguments. This is the most developed of the command definitions as of writing, with the most `CmdBuilder` extensions. Most commands will likely not look like this.
- Command names can consist of any character other than `' '` or `'.'`, so adding aliases such as `Media.+` is perfectly acceptable. I recommend keeping canonical names as letters.
- This command implements 4 execution methods, we will go into these in more detail below:
    - `ExecuteAsync` (`.Exec()`): Default execution function, takes `IReadOnlyList<string>` argument and returns `Task`.
    - `TestAsync` (`.Test()`): Default testing function, takes `IReadOnlyList<string>` argument and returns `Task<bool>`.
    - `ExecuteJsonAsync` (`.ExecuteJson()`): Json execution function, takes the defined `object` argument and retuns `Task`.
    - `TestJsonAsync` (`.TestJson()`): Json testing function, takes the defined `object` argument and returns `Task`.
- For commands with arguments, it is highly recommended to define the `usage` field. This string has no connection to function, so it is up to the developer to keep it updated. Mandatory arguments should be denoted with `<Type Name>` and optional with `[Type Name]`.
- Examples are added here individually. They can also be added as parameters with `.Examples()`. It is recommended that you do run the commands written here by copying and pasting, as users will rely on this precedent.
     - You will notice that some arguments with multiple words are in quotations, and some optional arguments are given as `'_'` - this character generally means "blank" or "revert to default". Implementation of this is shown later.
- `.Children()` is added here, we could also use `AddChild()`. One new line is put after each command, including this single child.
- `.Build()` is the last command implemented, this is necessary to turn this from a `CommandBuilder` instance to a `ReplCommand`.

### Media.Add.Prompt

Prompt add function, where the user is asked to enter each piece of information individually. These are a little bit harder to implement, but are usually more convenient to the user as there is no need to worry about argument orders.
- Please note that if cancellation is not implemented, there is no way to leave the prompt loop until it is finished apart from closing the program.
- This command has less parameters, with no examples or usage string, as it is simple to use.

## Add Command Handlers

### Media.Add Execute

Here is the definition for the `Media.Add` handler, `MediaAdd`:

```csharp

private Task MediaAdd(ReplContext ctx, IReadOnlyList<string> args, CancellationToken ct)
{
    // Mandatory arguments use "args.[Type]()"
    string title = args.String(0, "Title");

    // Types without dedicated extractors need to be manually converted after:
    string typeStr = args.String(1, "Type");
    if (!typeStr.TryToMediaType(out MediaType type)) throw new ReplUserException($"Could not parse type '{typeStr}'.");

    string statStr = args.String(2, "Status");
    if (!statStr.TryToMediaStatus(out MediaStatus status)) throw new ReplUserException($"Could not parse status '{statStr}'.");

    // Optional/Nullable arguments can use nullable extractors:
    int? releaseYear = args.IntOrNullable(3, "Release Year", null);
    string? genre = args.StringNullableOrDefault(4, "Genre", null);
    string? creator = args.StringNullableOrDefault(5, "Creator", null);
    DateTime? startedOn = args.dateTimeOrNullable(6, "Started On", null);
    DateTime? completedOn = args.dateTimeOrNullable(7, "Ended On", null);
    string? progressNote = args.StringNullableOrDefault(8, "Progress Note", null);
    string? notes = args.StringNullableOrDefault(9, "Notes", null);
    double? rating = args.DoubleOrNullable(10, "Rating", null);

    _service.AddMedia(new Media(title, type, status, releaseYear, genre, creator, startedOn, completedOn, progressNote, notes, rating));
            
    // Always provide feedback upon success:
    ctx.WriteLine($"Added entry #{_service.GetLastId()}");
    return Task.CompletedTask;
}  

```

Commands which take arguments should make use of argument extractor functions from `CCRepl.ArgumentHelpers`. These are extension methods to the type `IReadOnlyList<string>` and are generally named by the return type with the case of the first letter changed (`string` > `args.String()`, `DateTime` > `args.dateTime()`). These types are listed in `Tools.md` so I will not go into more detail here.

Here we define each of the required input variables for our `Media` class using argument extractors, add it, and give feedback.

If a suitable extraction method does not exist, you can easily make your own, for example, instead of converting string to MediaType() here, we can do:

```csharp

public static MediaType mediaType(this IReadOnlyList<string> args, int index, string name)
{
    if (index >= args.Count) throw new ReplUserException($"Not enough arguments, missing MediaType '{name}');
    if (!MediaType.TryParse(args[index], out MediaType v)) throw new ReplUserException($"Cannot parse MediaType '{Name}': '{args[index]}');
    else return v;
}

```

### Media.Add.Prompt Execute

Here is the definition for the `Media.Add.Prompmt` handler, `MediaAddPromptAsync`:

```csharp

private async Task MediaAddPromptAsync(ReplContext ctx, IReadOnlyList<string> args, CancellationToken ct)
{
    // Announcing command:
    ctx.WriteLine("Add Media Item.");

    // For required string arguments, use "RequireString()". This does not accept null/whitespace arguments.
    string title = await ctx.RequireString(ct, "Title: ", "Title cannot be blank, please try again.");

    // For non-string required arguments, use "RequireAsync()".
    MediaType type = await ctx.RequireAsync(ct, "Media type: ",
        s => (s.TryToMediaType(out MediaType v), v),
        $"Could not parse. Valid media types are: {MediaTypeExt.MediaTypeList}.");
    MediaStatus status = await ctx.RequireAsync(ct, "Status: ",
        s => (s.TryToMediaStatus(out MediaStatus v), v),
        $"Could not parse. Valid media statuses are: {MediaStatusExt.MediaStatusList}.");

    // For non-string optional arguments, you can use "RequestAsync()" or "RequireAsync()" with cancellation strings (see below).
    int? releaseYear = await ctx.RequestAsync<int?>(ct, "Release year (optional): ",
        s => (int.TryParse(s, out int v), v), null);

    // For optional string arguments, there are a couple of options. RequestStringNullable returns null if null or whitespace:
    string? genre = await ctx.RequestStringNullable(ct, "Genre (optional): ");
    string? creator = await ctx.RequestStringNullable(ct, "Creator (author, director, studio, &c., optional): ");

    // Cancellation strings can be used with RequireAsync() if you want explicit confirmation of no entry for non-string optional arguments. 
    DateTime? startedOn = await ctx.RequireAsync<DateTime?>(ct, 
        "Start date, if known, and have started (optional, leave blank, 'null', or 'not started' otherwise): ",
        s => (DateTime.TryParse(s, out DateTime v), v), 
        "Could not parse, please try again.", null, "", " ", "_", "null", "notstarted", "not started");
    DateTime? completedOn = await ctx.RequireAsync<DateTime?>(ct, 
        "Completion date, if known, and have finished (optional, leave blank, 'null', or 'unfinished' otheriwse): ",
        s => (DateTime.TryParse(s, out DateTime v), v), 
        "Could not parse, please try again.", null, "", " ", "_", "null", "unfinished", "notfinished");

    string? progressNote = await ctx.RequestStringNullable(ct, "Note on progress (e.g. 'Chapter 20', 'Episode 5', '1hr 20mins', optional): ");
    string? notes = await ctx.RequestStringNullable(ct, "Other notes (optional): ");
    double? rating = await ctx.RequireAsync<double?>(ct, 
        "Rate out of 10 (optional, leave blank, 'null', or 'unrated' otherwise): ",
        s => (double.TryParse(s, out double v), v), "Could not parse, please try again.", null, "", " ", "_", "null", "unrated", "idk");

    _service.AddMedia(new Media(title, type, status, releaseYear, genre, creator, startedOn, completedOn, progressNote, notes, rating));

    ctx.WriteLine($"Added entry #{_service.GetLastId()}");
}   

```

Each of the prompt commands are derivative of `ReadLineAsync()`. Obviously these must be `async` functions.

Structure is generally the same as `Media.Add`: We define each of the input variables necessary for our `Media` class using prompt functions, add it, and give feedback.

Notes on the functions used, and other prompt functions:

| Name | Use | Notes | 
| :--- | :-- | :---- |
| `ReadLinedAsync()` | Raw input. | Calls method of the same name in `Repl`, which invokes `ReqInputAsync`. All other prompt functions use this. | 
| `RequestStringNullable()` | Optional arguments. | If input is null or whitespace, returns null. |
| `RequestStringOrDefault()` | Optional arguments & maintaining defaults. | If input is null or whitespace, or equal to '_', returns fallBack argument. |
| `RequestStringOrDefaultNullable()` | Optional arguments & maintaining nullable defaults. | If input is null or whitepsace, returns null. If input is '_', returns fallback. |
| `RequireString()` | Required arguments. | If input is null or whitespace, prints retryPrompt argument and tries again. |
| `RequireStringOrDefault()` | Required arguments & maintaining defaults. | If input is null or whitespace, prints retryPrompt and tries again. If input is '_', will return fallBack. |
| `RequestAsync<T>()`[^RequestAsync] | Optional argument of any type. | Returns fallBack if input string is equal to a defaultString. If no defaultString is defined, will return fallBack if cannot parse. Generally not recommended. See footnote for more details. |
| `RequireAsync<T>()`[^RequireAsync] | Required argument of any type. | Prints retry prompt and tries again if cannot parse, if input string is equal to a defaultString (if defined), will return fallBack. See footnote for more details. |

[^RequestAsync]: RequestAsync() has two overloads:
    
    - `public static async Task<T> RequestAstnc<T>(this ReplContext ctx, CancellationToken ct, string prompt, Func<string, (bool success, T value)> parser, T fallBack = default)`
        - Returns fallBack if cannot parse.
    - `public static async Task<T> RequestAsync<T>(this ReplContext ctx, CancellationToken ct, string prompt, Func<string, (bool success, T value)> parser, T fallBack, params string[] defaultStrings)`
        - Throws exception if cannot parse.

    You can use these, but I generally recommend just using RequireAsync() with defaultStrings, as RequestAsync() methods cannot distinguish between intentional omition of data and typos. RequireAsync() methods use RequestAsync().

    Examples:
    ``` csharp

    // Overload 1:
    int x = await ctx.RequestAsync(ct,
        "Enter Number:", 
        s => (int.TryParse(s, out int v), v), 
        0);

    // Overload 2:
    int x = ctx.RequestAsync(
        "Enter Number:",
        s => (int.TryParse(s, out int v), v),
        0, " ", "Default", "FallBack", "Zero", No");

    ```

[^RequireAsync]: RequireAsync() has two overloads:

    - `public static async Task<T> RequireAsync<T>(this ReplContext ctx, CancellationToken ct, string prompt, Func<string, (bool success, T Value)> parser, string retryPrompt)`
        - If cannot parse, will repetedly print retryPrompt, prompt and request input again until can parse.
    - `public static async Task<T> RequireAsync<T>(this ReplContext ctx, CancellationToken ct, string prompt, Func<string, (bool success, T Value)> parser, string retryPrompt, T fallBack, params string[] defaultStrings)`
        - Identical to the other overload, but will return the fallBack value if one if input is equal to one of the defaultStrings.
    
    Examples:
    ```csharp

    // Overload 1:
    int x = await ctx.RequireAsync(ct,
        "Enter Number:", 
        s => (int.TryParse(s, out int v), v),
        "Could not parse, please try again.");

    // Overload 2:
    int x = await ctx.RequireAsync(ct,
        "Enter Number:", 
        s => (int.TryParse(s, out int v), v),
        "Could not parse, please try again.",
        0, " ", "Default", "FallBack", "Zero", No");

    ```

### Media.Add Test

Going back to Media.Add, lets define a test function.

`TestAsync` functions return `Task<bool>` and should be used for input validation. It is up to the developer to make sure that input processing aligns with the main `ExecuteAsync` function.

Test functions are optional, and not required to pass tests: `Repl.TestCommandAsync` returns `true` if there is no test function.

The best way to define a test function is to copy the input section of the `ExecuteAsync` and return true, in this case:

```csharp

private Task<bool> MediaAddTest(ReplContext ctx, IReadOnlyList<string> args, CancellationToken ct)
{
    // Use the same data extraction logic.
    string title = args.String(0, "Title");

    // You can have these return as "False" if you prefer, but you will not get information from exceptions. Test is considered a failure if there are any exceptions:
    string typeStr = args.String(1, "Type");
    if (!typeStr.TryToMediaType(out MediaType type)) throw new ReplUserException($"Could not parse type '{typeStr}'.");

    string statStr = args.String(2, "Status");
    if (!statStr.TryToMediaStatus(out MediaStatus status)) throw new ReplUserException($"Could not parse status '{statStr}'.");

    int? releaseYear = args.IntOrNullable(3, "Release Year", null);
    string? genre = args.StringNullableOrDefault(4, "Genre", null);
    string? creator = args.StringNullableOrDefault(5, "Creator", null);
    DateTime? startedOn = args.dateTimeOrNullable(6, "Started On", null);
    DateTime? completedOn = args.dateTimeOrNullable(7, "Ended On", null);
    string? progressNote = args.StringNullableOrDefault(8, "Progress Note", null);
    string? notes = args.StringNullableOrDefault(9, "Notes", null);
    double? rating = args.DoubleOrNullable(10, "Rating", null);

    Media Sample = new Media(title, type, status, releaseYear, genre, creator, startedOn, completedOn, progressNote, notes, rating);

    // If you set this as "async Task<bool> MediaAddTest()" this can just be "Return true;"
    return Task.FromResult(true);
}

```

### Media.Add Json Execute

Commands can be called with arguments formatted in Json. This is mainly used for scripting, but this can be done manually as well by replacing `Repl.ExecuteAsync()` with `Repl.ExecuteJsonAsync()`.

Json execution methods take an `object` argument which can only be assigned with `CmdBuilder`. In this case, we use `MediaAddPayload`.

To make a command Json executable:

- Define the Json Payload (sealed record).
- Define the Json handler with arguments `(ReplContext, [Your Payload], CancellationToken)` (this can have the same name as normal execute method).
- Assign it to the command with `.ExecJson<[Your Payload]>([Your Handler])`

Parsing the Json statement and some input validation will be done automatically. I do not recommend assigning a method to 

```csharp

private Task MediaAdd(ReplContext ctx, MediaAddPayload pl, CancellationToken ct)
{
    // JSON overload. Uses "Payload" record.

    // Custom types/enums must be converted:
    MediaType type = pl.Type.ToMediaType();
    MediaStatus status = pl.Status.ToMediaStatus();

    _service.AddMedia(new Media(pl.Title, type, status, pl.ReleaseYear, pl.Genre, pl.Creator, pl.StartedOn, pl.CompletedOn, pl.ProgressNotes, pl.Notes, pl.Rating));

    // Feedback should be brief:
    ctx.WriteLine($"Added entry {_service.GetById(_service.GetLastId()).PrintRef()}");
    return Task.CompletedTask;
}

private sealed record MediaAddPayload(string Title, string Type, string Status, int? ReleaseYear, string? Genre, string? Creator, DateTime? StartedOn, DateTime? CompletedOn, string? ProgressNotes, string? Notes, double? Rating);

```