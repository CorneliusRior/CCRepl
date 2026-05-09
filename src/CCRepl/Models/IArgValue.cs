using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CCRepl.Models
{
    public interface IArgValue
    {
    }

    public sealed class ArgValue<T> : IArgValue
    {
        public T? Value { get; }

        public ArgValue(T? value)
        {
            Value = value;
        }
    }
}
