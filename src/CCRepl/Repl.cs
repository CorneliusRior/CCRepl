using CCRepl.CommandSets;
using CCRepl.Tools;
using CCRepl.Models;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using CCRepl.Scripting;

namespace CCRepl;

/// <summary>
/// Entry point for the CCRepl library. This class is split between Repl.cs, ReplIO, and ReplSetup.
/// </summary>
public sealed partial class Repl
{
    // CommandIndex:
    public Dictionary<string, ReplCommand> CommandIndex = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, ReplCommand> AliasIndex = new(StringComparer.OrdinalIgnoreCase);
    public List<ReplCommand> RootCommands = new();
       
    // Other variables:
    public int OneLineMaxWidth { get; set; } = 200;
    private readonly JsonSerializerOptions _jsonOptions = new();
    
    // Get command:
    public ReplCommand FindCommand(string commandHead)
    {
        ReplCommand cmd;
        if (CommandIndex.ContainsKey(commandHead)) cmd = CommandIndex[commandHead];
        else if (AliasIndex.ContainsKey(commandHead)) cmd = AliasIndex[commandHead];
        else throw new ReplUserException($"Unknown command '{commandHead}': no command or alias found.");
        return cmd;
    }

    // Execute:


    /// <summary>
    /// Tuns a command with token input.
    /// </summary>
    /// <param name="tokens"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public async Task ExecuteAsync(Tokens tokens, CancellationToken ct = default)
    {
        try
        {
            ReplCommand cmd = FindCommand(tokens.CommandHead);
            ReplContext ctx = new(this);

            if (cmd.CanNewExecute) await cmd.ExecuteAsync!(ctx, new(ctx, tokens, ct), ct);
            else if (cmd.CanStringExecute) await cmd.StringExecuteAsync!(ctx, tokens.ArgStrings, ct);
            else throw new ReplException($"Command '{tokens.CommandHead}' has no ExecuteAsync method: cannot execute.");
        }
        catch (OperationCanceledException) { WriteLine($"Cancelled."); }
        catch (ReplUserException ex) { WriteLine($"Input Error, {ex.Location} {ex.Message}"); }
        catch (ReplException ex) { WriteLine($"Error in {ex.Location} {ex.Message}"); }
        catch (Exception ex) { WriteLine($"Error: {ex.Message}"); }
    }

    /// <summary>
    /// Overload which parses first.
    /// </summary>
    /// <param name="input"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public async Task ExecuteAsync(string input, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(input)) return;
        try { await ExecuteAsync(input.TokenizeParen(), ct); }
        catch (OperationCanceledException) { WriteLine($"Cancelled."); }
        catch (ReplUserException ex) { WriteLine($"Input Error, {ex.Location} {ex.Message}"); }
        catch (ReplException ex) { WriteLine($"Error in {ex.Location} {ex.Message}"); }
        catch (Exception ex) { WriteLine($"Error: {ex.Message}"); }
    }

    // Get rid of this later:
    /// <summary>
    /// Runs the ExecuteAsync method on the specified command with specified arguments. This is kept separate from ExecuteAsync so that commands can execute other commands directly.
    /// </summary>
    /// <param name="commandHead"></param>
    /// <param name="args"></param>
    public async Task ExecuteCommandAsync(string commandHead, IReadOnlyList<string> args, CancellationToken ct = default)
    {
        ReplCommand cmd = FindCommand(commandHead);
        if (!cmd.CanStringExecute) throw new ReplException($"Command '{commandHead}' has no ExecuteAsync method: cannot execute.");
        ReplContext ctx = new(this);
        await cmd.StringExecuteAsync!(ctx, args, ct);
    }

    // Test:

    /// <summary>
    /// Attempts to parse the command and run the TestAsync method on specified command with specified arguments.
    /// </summary>
    /// <param name="input"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public async Task<bool> TestAsync(Tokens tokens, CancellationToken ct = default)
    {
        try
        {
            ReplCommand cmd = FindCommand(tokens.CommandHead);
            ReplContext ctx = new(this);
            bool ok;

            if (cmd.CanNewTest) ok = await cmd.TestAsync!(ctx, new(ctx, tokens, ct), ct);
            else if (cmd.CanStringTest) ok = await cmd.StringTestAsync!(ctx, tokens.ArgStrings, ct);
            else
            {
                WriteLine($"Command {cmd.Address} has no TestAsync method: cannot test.");
                ok = true;
            }

            if (ok) WriteLine($"[SUCCESS]: '{tokens.Print()}'.");
            else WriteLine($"[FAILURE]: '{tokens.Print()}'.");
            return ok;
        }
        catch (OperationCanceledException) { WriteLine($"Cancelled."); }
        catch (ReplUserException ex) { WriteLine($"Input Error, {ex.Location} {ex.Message}"); }
        catch (ReplException ex) { WriteLine($"Error in {ex.Location} {ex.Message}"); }
        catch (Exception ex) { WriteLine($"Error: {ex.Message}"); }
        return false;
    }

    /// <summary>
    /// Overload which parses first
    /// </summary>
    /// <param name="input"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public async Task<bool> TestAsync(string input, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(input)) return true;
        try { return await TestAsync(input.TokenizeParen(), ct); }
        catch (OperationCanceledException) { WriteLine($"Cancelled."); }
        catch (ReplUserException ex) { WriteLine($"Input Error, {ex.Location} {ex.Message}"); }
        catch (ReplException ex) { WriteLine($"Error in {ex.Location} {ex.Message}"); }
        catch (Exception ex) { WriteLine($"Error: {ex.Message}"); }
        return false;
    }

    /// <summary>
    /// Tests command, runs if successful, otherwise doesn't.
    /// </summary>
    /// <param name="input"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public async Task TestAndExecuteAsync(string input, CancellationToken ct = default)
    {
        if (await TestAsync(input, ct)) await ExecuteAsync(input, ct);
        else WriteLine($"Failed test, not executing: '{input}'.");
    }

    // Json input:
    public async Task ExecuteJsonAsync(string commandHead, string json, CancellationToken ct = default)
    {
        ReplCommand cmd = FindCommand(commandHead);
        if (!cmd.CanExecuteJson) throw new ReplException($"Command '{commandHead}' has no ExecuteJsonAsync method: cannot execute.");
        if (cmd.JsonPayloadType is null) throw new ReplException("Null JSON Payload - this command cannot parse JSON.");

        object pl;
        pl = JsonSerializer.Deserialize(json, cmd.JsonPayloadType, _jsonOptions) ?? throw new ReplUserException($"Invalid JSON: Cannot parse.");
        ReplContext ctx = new(this);
        await cmd.ExecuteJsonAsync!(ctx, pl, ct);
    }

    public async Task<bool> TestJsonAsync(string commandHead, string json, CancellationToken ct = default)
    {
        // Make sure command exists and can run:
        ReplCommand cmd;
        try { cmd = FindCommand(commandHead); }
        catch(ReplUserException ex) 
        {
            WriteLine(ex.Message);
            return false;
        }
        if (!cmd.CanExecuteJson)
        {
            WriteLine($"No ExecuteJson method found for '{cmd.Address}', cannot execute.");
            return false;
        }

        // Return true unless there's a TestJsonAsync method
        if (cmd.JsonPayloadType is null)
        {
            WriteLine($"Null JsonPayload Type, '{cmd.Address}' cannot parse JSON, cannot execute.");
            return false;
        }

        // Try to parse JSON:
        object pl;
        try{ pl = JsonSerializer.Deserialize(json, cmd.JsonPayloadType, _jsonOptions) ?? throw new ReplUserException($"Invalid JSON: Cannot parse."); }
        catch (ReplUserException ex)
        {
            WriteLine($"Command '{commandHead}' failed to parse JSON: '{ex.Message}'\n\"{json}\"");
            return false;
        }

        // If there no TestJsonAsync method, return true, otherwise, run it:
        if (!cmd.CanTestJson) return true;
        ReplContext ctx = new(this);
        return await cmd.TestJsonAsync!(ctx, pl, ct);
    }

    // Scripting:
    public async Task ExecuteScriptAsync(Script script, CancellationToken ct = default)
    {
        WriteLine(script.PrintInfo().ToDoubleBox());
        WriteLine("Running...\n");
        foreach (ScriptStatement s in script.Statements) await ExecuteJsonAsync(s.CommandHead, s.JsonPayload, ct);        
        WriteLine('\n' + "Finished".ToBox());
    }

    public async Task<bool> TestScriptAsync(Script script, CancellationToken ct = default)
    {
        WriteLine(script.PrintInfo().ToDoubleBox());
        WriteLine("Testing...\n");
        bool ok = true;
        List<ScriptStatement> error = new();
        foreach (ScriptStatement s in script.Statements)
        {
            try
            {
                if (await TestJsonAsync(s.CommandHead, s.JsonPayload, ct)) WriteLine($"[OK] {s.PrintRef()}");
                else
                {
                    WriteLine($"[ERROR] {s.PrintRef()}");
                    ok = false;
                    error.Add(s);
                }
            }
            catch (ReplUserException ex)
            {
                WriteLine($"[ERROR] {s.PrintRef()}: Input Error, {ex.Location} {ex.Message}");
                ok = false;
                error.Add(s);
            }
            catch (ReplException ex)
            {
                WriteLine($"[ERROR] {s.PrintRef()}: Error in {ex.Location} {ex.Message}");
                ok = false;
                error.Add(s);
            }
            catch (Exception ex)
            {
                WriteLine($"[ERROR] {s.PrintRef()}: {ex.Message}");
                ok = false;
                error.Add(s);
            }            
        }
        if (ok) WriteLine('\n' + "No Errors found".ToBox());
        else
        {
            WriteLine('\n' + $"{error.Count} Error(s) found:".ToBox());
            if (error.Count < 5) foreach (ScriptStatement est in error) WriteLine(est.PrintInfoShort());
            else foreach (ScriptStatement est in error) WriteLine(est.PrintInfo());
        }
        return ok;
    }

}
