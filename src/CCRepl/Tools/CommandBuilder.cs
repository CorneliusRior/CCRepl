using CCRepl.Models;
using CCRepl.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace CCRepl.Tools
{
    /// <summary>
    /// Tool for building commands. Allows definition of ExecuteJsonAsync &c.
    /// </summary>
    public sealed class CommandBuilder
    {
        private readonly string _name;
        private readonly List<string> _aliases = [];
        private readonly List<string> _examples = [];
        private readonly List<ReplCommand> _children = [];

        private Func<ReplContext, CommandArgs, CancellationToken, Task>? _executeAsync;
        private Func<ReplContext, CommandArgs, CancellationToken, Task<bool>>? _testAsync;
        private List<IArgSpec> _argSpecs = [];
        private Func<ReplContext, IReadOnlyList<string>, CancellationToken, Task>? _stringExecuteAsync;
        private Func<ReplContext, IReadOnlyList<string>, CancellationToken, Task<bool>>? _stringTestAsync;

        private Func<ReplContext, object, CancellationToken, Task>? _executeJsonAsync;
        private Func<ReplContext, object, CancellationToken, Task<bool>>? _testJsonAsync;
        private Type? _jsonPayloadType;

        private string? _usage;
        private string? _description;
        private string? _longDescription;
        private string? _group;
        private string? _remarks;

        public CommandBuilder(string name)
        {
            _name = name;
        }

        public ReplCommand Build()
        {
            return new ReplCommand(
                name:               _name,
                executeAsync:       _executeAsync,
                testAsync:          _testAsync,
                argSpecs:           _argSpecs,
                stringExecuteAsync: _stringExecuteAsync,
                stringTestAsync:    _stringTestAsync,
                executeJsonAsync:   _executeJsonAsync,
                testJsonAsync:      _testJsonAsync,
                aliases:            _aliases,
                usage:              _usage,
                description:        _description,
                examples:           _examples,
                longDescription:    _longDescription,
                group:              _group,
                remarks:            _remarks,
                children:           _children
            )
            {
                JsonPayloadType = _jsonPayloadType
            };
        }
        public static CommandBuilder Command(string name) => new(name);

        public CommandBuilder Aliases(params string[] aliases)
        {
            _aliases.AddRange(aliases);
            return this;
        }

        public CommandBuilder AddAlias(string alias)
        {
            _aliases.Add(alias);
            return this;
        }

        public CommandBuilder AliasAdd(string alias) => AddAlias(alias);

        public CommandBuilder Exec(Func<ReplContext, CommandArgs, CancellationToken, Task> newExec)
        {
            _executeAsync = newExec;
            return this;
        }

        public CommandBuilder Test(Func<ReplContext, CommandArgs, CancellationToken, Task<bool>> testAsync)
        {
            _testAsync = testAsync;
            return this;
        }

        public CommandBuilder StringExec(Func<ReplContext, IReadOnlyList<string>, CancellationToken, Task> executeAsync)
        {
            _stringExecuteAsync = executeAsync;
            return this;
        }

        public CommandBuilder StringTest(Func<ReplContext, IReadOnlyList<string>, CancellationToken, Task<bool>> testAsync)
        {
            _stringTestAsync = testAsync;
            return this;
        }

        public CommandBuilder Args(params ICmdArg[] args)
        {
            foreach (ICmdArg arg in args) _argSpecs.Add(arg.ToArgSpec());
            return this;
        }

        public CommandBuilder ExecJson<TPayload>(Func<ReplContext, TPayload, CancellationToken, Task> executeJsonAsync)
        {
            _jsonPayloadType = typeof(TPayload);
            _executeJsonAsync = (ctx, payload, ct) => executeJsonAsync(ctx, (TPayload)payload, ct);
            return this;
        }

        public CommandBuilder TestJson<TPayload>(Func<ReplContext, TPayload, CancellationToken, Task<bool>> testJsonAsync)
        {
            _jsonPayloadType = typeof(TPayload);
            _testJsonAsync = (ctx, payload, ct) => testJsonAsync(ctx, (TPayload)payload, ct);
            return this;
        }

        public CommandBuilder Usage(string usage)
        {
            _usage = usage;
            return this;
        }

        public CommandBuilder Description(string description)
        {
            _description = description;
            return this;
        }

        public CommandBuilder LongDescription(string longDescription)
        {
            _longDescription = longDescription;
            return this;
        }

        public CommandBuilder Examples(params string[] examples)
        {
            _examples.AddRange(examples);
            return this;
        }

        public CommandBuilder AddExample(string example)
        {
            _examples.Add(example);
            return this;
        }

        public CommandBuilder ExampleAdd(string example) => AddExample(example);

        public CommandBuilder Group(string group)
        {
            _group = group;
            return this;
        }

        public CommandBuilder Remarks(string remarks)
        {
            _remarks = remarks;
            return this;
        }

        public CommandBuilder Children(params ReplCommand[] children)
        {
            _children.AddRange(children);
            return this;
        }

        public CommandBuilder AddChild(ReplCommand child)
        {
            _children.Add(child);
            return this;
        }

        public CommandBuilder ChildAdd(ReplCommand child) => AddChild(child);
        public CommandBuilder SubCommands(params ReplCommand[] subCommands) => Children(subCommands);
        public CommandBuilder AddSubcommand(ReplCommand subCommand) => AddChild(subCommand);
        public CommandBuilder SubcommandAdd(ReplCommand subCommand) => AddChild(subCommand);        
    }

    public static class CmdBuilder
    {
        public static CommandBuilder Cmd(string name) => new(name);

        // Argument functions:
        public static CmdArg<int> IntArg(string name, ArgMode mode = ArgMode.Required, int fallback = default, string prompt = "", string retryPrompt = "", params string[] cancelStrings) =>
            new CmdArg<int>(name, Parsers.Parse, mode, fallback, prompt, retryPrompt, cancelStrings);

        public static CmdArg<double> DblArg(string name, ArgMode mode = ArgMode.Required, double fallback = default, string prompt = "", string retryPrompt = "", params string[] cancelStrings) =>
            new CmdArg<double>(name, Parsers.Parse, mode, fallback, prompt, retryPrompt, cancelStrings);

        public static CmdArg<string> StrArg(string name, ArgMode mode = ArgMode.Required, string fallback = "", string prompt = "", string retryPrompt = "", params string[] cancelStrings) =>
            new CmdArg<string>(name, Parsers.Parse, mode, fallback, prompt, retryPrompt, cancelStrings);

        public static CmdArg<DateTime> DtmArg(string name, ArgMode mode = ArgMode.Required, DateTime fallback = default, string prompt = "", string retryPrompt = "", params string[] cancelStrings) =>
            new CmdArg<DateTime>(name, Parsers.Parse, mode, fallback, prompt, retryPrompt, cancelStrings);
    }

    // Structural class for defining command arguments:
    public interface ICmdArg
    {
        IArgSpec ToArgSpec();
    }

    public sealed class CmdArg<T> : ICmdArg
    {
        public string Name { get; }
        public ArgParser<T> Parser { get; }
        public ArgMode Mode { get; }
        public T? Fallback { get; }
        public PromptInfo PmtInfo { get; }

        public CmdArg(string name, ArgParser<T> parser, ArgMode mode = ArgMode.Required, T? fallback = default, string prompt = "", string retryPrompt = "", params string[] cancelStrings)
        {
            Name = name;
            Parser = parser;
            Mode = mode;
            Fallback = fallback;

            PmtInfo = new PromptInfo(prompt, retryPrompt, cancelStrings.Length == 0 ? new[] { "\\" } : cancelStrings);
        }

        public IArgSpec ToArgSpec() => new ArgSpec<T>(Name, Mode, Parser, Fallback, PmtInfo);
    }    
}
