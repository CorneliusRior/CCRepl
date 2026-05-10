using CCRepl.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CCRepl.Models
{
    public interface IArgSpec
    {
        string Name { get; }
        ArgMode Mode { get; }
        PromptInfo PmtInfo { get; }
        IArgValue Parse(string text);
        IArgValue Fallback();
        string TypeString { get; }
        string Print();
    }

    public delegate bool ArgParser<T>(string text, out T value);

    public sealed class ArgSpec<T> : IArgSpec
    {
        private readonly ArgParser<T> _parser;
        private readonly T? _fallback;

        public string Name { get; }
        public ArgMode Mode { get; }
        public PromptInfo PmtInfo { get; }

        public string TypeString => TypeName.For<T>();

        public ArgSpec(string name, ArgMode mode, ArgParser<T> parser, T? fallback = default, PromptInfo? pmtInfo = null)
        {
            Name = name;
            Mode = mode;
            _parser = parser;
            _fallback = fallback;
            PmtInfo = PmtInfo ?? new PromptInfo();

            if (mode.IsPrompt()) GeneratePrompt();
        }

        public IArgValue Parse(string text)
        {
            if (_parser(text, out T value)) return new ArgValue<T>(Name, value);
            throw new ReplUserException($"Cannot parse argument {Print()}: '{text}'");
        }

        public IArgValue Fallback()
        {
            return new ArgValue<T>(Name, _fallback);
        }

        public string Print()
        {
            return $"{Mode.OpenChar()}{TypeString} {Name}{Mode.CloseChar()}";
        }

        private void GeneratePrompt()
        {
            if (string.IsNullOrWhiteSpace(PmtInfo.Prompt))
            {
                string requiredText = Mode.IsRequired() ? "required" : "optional";
                PmtInfo.Prompt = $"Enter value for {Name} ({requiredText}, {TypeString}): ";
            }

            if (string.IsNullOrWhiteSpace(PmtInfo.RetryPrompt))
            {
                if (PmtInfo.CancelStrings.Count == 0)
                {
                    PmtInfo.RetryPrompt = "Could not parse, please try again.";
                    return;
                }

                string action = Mode.IsRequired() ? "to cancel" : "for default";
                string cancelList = string.Join("', '", PmtInfo.CancelStrings);
                PmtInfo.RetryPrompt = $"Could not parse, please try again. ({action}, type one of the following: {{ '{cancelList}' }}.";
            }
        }
    }

    public static class TypeName
    {
        public static string For<T>() => For(typeof(T));

        public static string For(Type type)
        {
            if (type == typeof(string)) return "string";
            if (type == typeof(int)) return "int";
            if (type == typeof(long)) return "long";
            if (type == typeof(float)) return "float";
            if (type == typeof(double)) return "double";
            if (type == typeof(decimal)) return "decimal";
            if (type == typeof(bool)) return "bool";

            return type.Name;
        }
    }
}
