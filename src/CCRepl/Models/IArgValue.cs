using CCRepl.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CCRepl.Models
{
    public interface IArgValue
    {
        string Print();
    }

    public sealed class ArgValue<T> : IArgValue
    {
        public string Name { get; }
        public T? Value { get; }

        public ArgValue(string name, T? value)
        {
            Name = name;
            Value = value;
        }

        public string Print()
        {
            if (Value is IEnumerable<string> strs) return strs.PrintVec(Name);
            return $"{Name}: {Value?.ToString() ?? "null"}";
        }
    }
}
