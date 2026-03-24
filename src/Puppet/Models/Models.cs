using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace CCRepl.Models;

public sealed class ReplUserException : Exception
{
    public string Location { get; }
    public ReplUserException(string message, [CallerMemberName] string member = "", [CallerFilePath] string file = "", [CallerLineNumber] int line = 0) : base (message)
    {
        Location = $"{Path.GetFileName(file)} (line {line}) {member}():";
    }    
}

public sealed class ReplException : Exception
{
    public string Location { get; }
    public ReplException(string message, [CallerMemberName] string member = "", [CallerFilePath] string file = "", [CallerLineNumber] int line = 0) : base(message)
    {
        Location = $"{Path.GetFileName(file)} (line {line}) {member}():";
    }
}

public enum HelpAttribute
{
    Aliases,
    Usage,
    Description,
    Examples,
    LongDescription
}

public static class HelpAttributeExtensions
{
    public static bool TryParse(string input, out HelpAttribute output) => Enum.TryParse(input, true, out output);
}

public enum WaitAnimation
{
    Spinner,
    Elipses,
    Bounce,
    Road
}

public static class WaitAnimationExt
{
    public static string[] GetFrames(this WaitAnimation type) =>
        type switch
        {
            WaitAnimation.Spinner   => ["|", "/", "-", "\\"],
            WaitAnimation.Elipses   => [".", "..", "...", ".."],
            WaitAnimation.Bounce    => ["[*     ]", "[ *    ]", "[  *   ]", "[   *  ]", "[    * ]", "[     *]", "[    * ]", "[   *  ]", "[  *   ]", "[ *    ]"],
            WaitAnimation.Road      => ["[*   * ]", "[ *   *]", "[  *   ]", "[   *  ]"],
            _ => throw new ArgumentOutOfRangeException()
        };
}