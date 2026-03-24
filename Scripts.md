# CCRepl Scripting
CCRepl can run scripts of commands with arguments formatted in JSON.

## Script Formatting

### Comments
Comments are inert and are ignored by the parser. CCRepl scripts have two kinds of comment: Block comments and line comments. 

- Block comments start and finish with `#`
- Line comments start with `/` and finish on new line.

```ccreplscript

# This is an example of a block comment.
I can span multiple lines. #

// This is an example of a line comment.
```
Comments can not be put inside of JSON statements, but they can go anywhere else.

### Statements
A statement is a command as defined in a script, it consists of a command head and a JSON statement.

```ccreplscript
MyCommand.Subcommand
{
	"MyString": "String"
	"MyInt": 10
	"MyBool": true
	"MyDouble": 10.5
}
```

### MetaData
The first statement of a script needs to be a `ScriptMetaData` statement, which is formatted like so:

```ccreplscript
ScriptMetaData
{
	"Format": "v2"
	"Name": "[SCRIPT NAME]"
	"Author": "[AUTHOR NAME]"
	"Created": "yyyy-MM-ddTHH:mm:ss"
}
```

More parameters may be added to this in future.

## Running scripts

`BaseCommands` has a command `Script`, with children `Script.Run`, `Script.Run.Force`, and `Script.Test`. These take the file path as an argument. Each will parse the given file. `Script.Test` will test it, `Script.Run` will test it and then run it, `Script.Run.Force` will skip the testing and attempt to run it.

## Planned features

 - Be able to pass scripts to CCRepl externally.
 - Make CCRepl scripts its own FileType (.ccr?) which can be opened by a program running CCRepl by default.