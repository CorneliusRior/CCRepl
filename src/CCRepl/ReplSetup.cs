
using CCRepl.CommandSets;
using CCRepl.Models;

namespace CCRepl;
public sealed partial class Repl
{
    // Constructor:
    public Repl(params ICommandSet[] commandSets)
    {
        List<ReplCommand> rootCommands = new();
        rootCommands.AddRange(new BaseCommands().Commands);
        foreach (ICommandSet cs in commandSets) rootCommands.AddRange(cs.Commands);
        AssignCommandAddresses(rootCommands);
        BuildAliasDictionary(rootCommands);
    }

    // Set-up functions:
    public void AssignCommandAddresses(List<ReplCommand> commandList)
    {
        commandList = commandList.OrderBy(c => c.Name).ToList();
        foreach (ReplCommand root in commandList)
        {
            List<string> commandHead = new();
            AssignChildAddress(root, commandHead);
        }
    }

    public void AssignChildAddress(ReplCommand command, List<string> commandHead)
    {
        commandHead.Add(command.Name);        
        command.Address = string.Join('.', commandHead);
        // put a try argument here: crashes when you have multiple commands with same name.
        CommandIndex.Add(command.Address, command);
        foreach (ReplCommand child in command.Children.OrderBy(c => c.Name).ToList()) AssignChildAddress(child, commandHead);
        commandHead.RemoveAt(commandHead.Count - 1);
    }

    public void BuildAliasDictionary(List<ReplCommand> rootCommandList)
    {
        foreach (ReplCommand root in rootCommandList)
        {
            List<string> addresses = new();
            addresses.Add(root.Name);
            addresses.AddRange(root.Aliases);
            foreach (string alias in addresses)
            {
                if (!AliasIndex.TryAdd(alias, root)) throw new ReplException($"Duplicate command or alias address: '{alias}' in '{root.Name}' ('{(root.Address ?? "Unknown address")}')");
                foreach (ReplCommand child in root.Children)
                {
                    AliasDictionaryAdd(alias, child);
                }
            }
        }
    }

    public void AliasDictionaryAdd(string parentAddress, ReplCommand command)
    {
        List<string> addresses = new();
        addresses.Add(command.Name);
        addresses.AddRange(command.Aliases);
        foreach (string alias in addresses)
        {
            string aliasAddress = parentAddress + '.' + alias;
            if (!AliasIndex.TryAdd(aliasAddress, command)) throw new ReplException($"Duplocate command or alias address: `{aliasAddress}` in '{command.Name}' ('{command.Address ?? "Unknown address"}')");
            foreach (ReplCommand child in command.Children)
            {
                AliasDictionaryAdd(aliasAddress, child);
            }
        }
    }
}