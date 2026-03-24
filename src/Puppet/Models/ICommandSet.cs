namespace CCRepl.Models;

public interface ICommandSet
{
    IReadOnlyList<ReplCommand> Commands { get; }
}