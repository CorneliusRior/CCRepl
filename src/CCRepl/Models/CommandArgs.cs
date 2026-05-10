using CCRepl.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

// Notes for putting in CommandArgs
/* Equivalents:

C++:                            C#:

template<typename T>            class ArgSpec<T> / Get<T>() (generics)
std::optional<T>                T?
dynamic_cast<ArgValue<T>*>      is ArgValue<T> (pattern matching)
std::unique_ptr<IArgValue>      (Ordinary reference types/interfaces)
 */


namespace CCRepl.Models
{
    public class CommandArgs
    {
        private readonly List<IArgValue> _args = new();

        public IReadOnlyList<IArgValue> Args => _args;
        public IReadOnlyList<string> Options { get; }
        public IReadOnlyList<string> ArgStrs { get; }
        public string CommandAddress { get; }

        public CommandArgs(ReplContext ctx, Tokens tokens, CancellationToken ct)
        {
            ReplCommand cmd = ctx.FindCommand(tokens.CommandHead);
            CommandAddress = cmd.Address;
            Options = tokens.Options;
            ArgStrs = tokens.ArgStrings;
            IReadOnlyList<IArgSpec> specs = cmd.ArgSpecs;

            for (int i= 0; i < specs.Count; i++)
            {
                IArgSpec spec = specs[i];

                // Argument is present:
                if (i < ArgStrs.Count)
                {
                    string raw = ArgStrs[i];

                    if (!spec.Mode.IsRequired() && spec.PmtInfo.CancelStrings.Contains(raw)) _args.Add(spec.Fallback());
                    else _args.Add(spec.Parse(raw));
                    continue;
                }

                // Argument is not present:
                switch (spec.Mode)
                {
                    case ArgMode.Required:
                        throw new ReplUserException($"Not enough arguments, missing argument {spec.Print()}.");

                    case ArgMode.RequiredPrompt:
                        ReadPromptedRequired(ctx, spec, ct);
                        break;

                    case ArgMode.Optional:
                        _args.Add(spec.Fallback());
                        break;

                    case ArgMode.OptionalPrompt:
                        ReadPromptedOptional(ctx, spec, ct);
                        break;

                    default: throw new ReplException($"Unknown argumentmode: '{spec.Mode}'.");

                }
            }
        }

        public bool HasOption(string option) => Options.Contains(option);
        public bool Opt(params string[] options) => Options.Any(HasOption);

        public T? Get<T>(int pos)
        {
            if (pos < 0 || pos >= _args.Count) throw new ReplException($"Argument out of range. Args.Count = {{_args.Count}}, pos = {pos}, in command '{CommandAddress}'.");
            if (_args[pos] is not ArgValue<T> arg) throw new ReplException($"Argument type mismatch at position {pos}. Expected {typeof(T).Name}.");
            return arg.Value;
        }

        public T GetOr<T>(int pos, T fallback)
        {
            T? value = Get<T>(pos);
            return value is null ? fallback : value;
        }

        public T GetRequired<T>(int pos)
        {
            T? value = Get<T>(pos);
            if (value is null) throw new ReplException($"Required value not present. pos = '{pos}', in command '{CommandAddress}'.");
            return value;
        }

        private async void ReadPromptedRequired(ReplContext ctx, IArgSpec spec, CancellationToken ct)
        {
            PromptInfo info = spec.PmtInfo;
            while (true)
            {
                try
                {
                    string input = await ctx.ReadLineAsync(info.Prompt, ct);
                    if (info.CancelStrings.Contains(input)) throw new OperationCanceledException();
                    _args.Add(spec.Parse(input));
                    return;
                }
                catch (ReplUserException) { ctx.WriteLine(info.RetryPrompt); }
            }
        }

        private async void ReadPromptedOptional(ReplContext ctx, IArgSpec spec, CancellationToken ct)
        {
            PromptInfo info = spec.PmtInfo;
            while (true)
            {
                try
                {
                    string input = await ctx.ReadLineAsync(info.Prompt, ct);
                    if (info.CancelStrings.Contains(input)) _args.Add(spec.Fallback());
                    else _args.Add(spec.Parse(input));
                    return;
                }
                catch (ReplUserException) { ctx.WriteLine(info.RetryPrompt); }
            }
        }

        public string PrintInfo()
        {
            StringBuilder sb = new();
            sb.AppendLine($"Command arguments for command '{CommandAddress}':");
            sb.AppendLine("Arguments:");
            foreach (IArgValue arg in _args) sb.AppendLine(arg.Print());
            sb.AppendLine("\nOptions:");
            sb.AppendLine(Options.PrintVec());
            sb.AppendLine("\nArgStrs:");
            sb.AppendLine(ArgStrs.PrintVec());
            sb.AppendLine("Reconstructing the command (for fun):");
            sb.Append(CommandAddress).Append('(').Append(string.Join(", ", ArgStrs)).Append(") ").Append(string.Join(' ', Options));
            return sb.ToString();
        }
    }

}
