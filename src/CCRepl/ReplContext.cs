using CCRepl.Models;
using CCRepl.Scripting;

namespace CCRepl;

/// <summary>
/// Context class for commands to interact with Repl: Request input, output, run scripts, and execution of other commands.
/// </summary>
public sealed class ReplContext
{
    private readonly Repl _repl;
    internal ReplContext(Repl repl)
    {
        _repl = repl;
    }

    // Command index & other:
    public Dictionary<string, ReplCommand> CommandIndex => _repl.CommandIndex;
    public Dictionary<string, ReplCommand> AliasIndex => _repl.AliasIndex;
    public List<ReplCommand> RootCommands => _repl.RootCommands;
    public int OneLineMaxWidth => _repl.OneLineMaxWidth;

    // IO:
    /// <summary>
    /// Prints 'prompt', returns whatever user types as a string. Used for raw input.
    /// </summary>
    /// <param name="prompt"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public Task<string> ReadLineAsync(string prompt, CancellationToken ct) => _repl.ReadLineAsync(prompt, ct);
    public void WriteLine(string msg = "") => _repl.WriteLine(msg);
    public void Write(string msg) => _repl.Write(msg);
    public void WriteStatus(string msg) => _repl.WriteStatus(msg);
    public void WriteStatusSample(string msg, int length = 150) => _repl.WriteStatusSample(msg, length);
    public void ClearStatus(string msg = "") => _repl.ClearStatus(msg);

    /// <summary>
    /// Uses WriteStatus() to update last line to make a loading animation while awaiting an async task. 
    /// </summary>
    /// <example>
    /// // To await a function with only 1 CancellationToken as an argument:
    /// public int MyFunction(CancellationToken ct) { (...) }
    /// int result = await ctx.WithWaiterAsync(MyFunction, (...) }
    /// 
    /// // To await a function which takes more arguments, wrap it in a lambda:
    /// public int MyFunction(string name, bool real) { (...) }
    /// int result = await ctx.WithWaiterAsync(token => MyFunction("John", true, token), (...) }
    /// 
    /// // You can also just define a new method inline:
    /// int result = await ctx.WithWaiterAsync(
    ///     async token =>
    ///     {
    ///         // ...
    ///     });
    /// <typeparam name="T">Return type</typeparam>
    /// <param name="action">Function which takes argument CancellationToken and returns Task<T></param>
    /// <param name="prefix">String shown before frames (e.g. "Loading").</param>
    /// <param name="suffix">String shown after frames.</param>
    /// <param name="finish">String shown in place when complete (e.g. "Done").</param>
    /// <param name="frameTime">Time in ms between updates</param>
    /// <param name="ct"></param>
    /// <param name="frames">Animation frames. Must contain at least one element.</param>
    /// <returns></returns>
    public Task<T> WithWaiterAsync<T>(Func<CancellationToken, Task<T>> action, string prefix = "Loading", string suffix = "", string finish = "Done", int frameTime = 100, CancellationToken ct = default, params string[] frames) => _repl.WithWaiterAsync(action, prefix, suffix, finish, frameTime, ct, frames);

    /// <summary>
    /// Overload of <see cref="WithWaiterAsync{T}(Func{CancellationToken, Task{T}}, string, string, string, int, CancellationToken, string[])"/>
    /// that uses a predefined <see cref="WaitAnimation"/>
    /// </summary>
    public Task<T> WithWaiterAsync<T>(Func<CancellationToken, Task<T>> action, string prefix = "Loading", string suffix = "", string finish = "Done", int frameTime = 100, CancellationToken ct = default, WaitAnimation animation = WaitAnimation.Spinner) => _repl.WithWaiterAsync(action, prefix, suffix, finish, frameTime, ct, animation);

    /// <summary>
    /// Overload of <see cref="WithWaiterAsync{T}(Func{CancellationToken, Task{T}}, string, string, string, int, CancellationToken, string[])"/>
    /// that can return <see cref="Task"/> instead of <see cref="Task{T}"/>
    /// </summary>
    public Task WithWaiterAsync(Func<CancellationToken, Task> action, string prefix = "Loading", string suffix = "", string finish = "Done", int frameTime = 100, CancellationToken ct = default, params string[] frames) => _repl.WithWaiterAsync(action, prefix, suffix, finish, frameTime, ct, frames);

    /// <summary>
    /// Overload of <see cref="WithWaiterAsync{T}(Func{CancellationToken, Task{T}}, string, string, string, int, CancellationToken, string[])"/>
    /// that can return <see cref="Task"/> instead of <see cref="Task{T}"/> and uses a predefined <see cref="WaitAnimation"/>
    /// </summary>
    public Task WithWaiterAsync(Func<CancellationToken, Task> action, string prefix = "Loading", string suffix = "", string finish = "Done", int frameTime = 100, CancellationToken ct = default, WaitAnimation animation = WaitAnimation.Spinner) => _repl.WithWaiterAsync(action, prefix, suffix, finish, frameTime, ct, animation);

    

    // Commands used to call other commands:
    public Task ExecuteCommandAsync(string commandHead, IReadOnlyList<string> args, CancellationToken ct = default) => _repl.ExecuteCommandAsync(commandHead, args, ct);
    public Task<bool> TestCommandAsync(string commandHead, IReadOnlyList<string> args, CancellationToken ct = default) => _repl.TestCommandAsync(commandHead, args, ct);

    // Json:
    public Task ExecuteJsonAsync(string commandHead, string json, CancellationToken ct = default) => _repl.ExecuteJsonAsync(commandHead, json, ct);
    public Task<bool> TestJsonAsync(string commandHead, string json, CancellationToken ct = default) => _repl.TestJsonAsync(commandHead, json, ct);

    // Script:
    public Task ExecuteScriptAsync(Script script, CancellationToken ct = default) => _repl.ExecuteScriptAsync(script, ct);
    public Task<bool> TestScriptAsync(Script script, CancellationToken ct = default) => _repl.TestScriptAsync(script, ct);
}