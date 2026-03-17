using System;

namespace Puppet;

public class CommandBuilder
{
	private readonly string _name;
	private readonly List<string> _aliases = [];
	private readonly List<string> _examples = [];
	private readonly List<PuppetCommand> _children = [];
}
