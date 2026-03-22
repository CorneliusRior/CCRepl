using Puppet.Cli2;
using Puppet.Tools;

using System.Text;
using System.Threading;

Puppet.Puppet puppet = new(new SampleCommands());
puppet.OutputRequested += msg => Console.WriteLine(msg);
puppet.InlineOutputRequested += msg => Console.Write(msg);

puppet.InputRequestedCancelableAsync = async (prompt, ct) =>
{
    Console.WriteLine(prompt);
    Console.Write("> ");

    StringBuilder sb = new();

    while (true)
    {
        ct.ThrowIfCancellationRequested();
        ConsoleKeyInfo key = Console.ReadKey(intercept: true);
        if (key.Key == ConsoleKey.Escape)
        {
            //Console.WriteLine("Escape pressed");
            throw new OperationCanceledException(ct);
        }
        if (key.Key == ConsoleKey.Backspace)
        {
            if (sb.Length > 0)
            {
                sb.Length--;
                Console.Write("\b \b");
            }
            continue;
        }
        if (key.Key == ConsoleKey.Enter)
        {
            // if ctrl: multiline
            if (key.Modifiers.HasFlag(ConsoleModifiers.Control))
            {
                sb.Append('\n');
                Console.WriteLine("  ");
                continue;
            }

            Console.WriteLine();
            return sb.ToString();
        }
        if (!char.IsControl(key.KeyChar))
        {
            sb.Append(key.KeyChar);
            Console.Write(key.KeyChar);
        }        
    }
};

Console.WriteLine("Puppet CLI 2. Type 'exit' to quit");
bool exit = false;

while (!exit)
{
    Console.WriteLine("Sample loop");
    ConsoleInputEditor ci = new("> ", Console.CursorTop);
    ci.ReqRender += (buffer, length, caret, row) =>
    {
        Console.Write("\r" + new string(' ', length) + "\r" + buffer);
        Console.SetCursorPosition(caret, row);
    };
    ci.ReqCancel += () =>
    {
        Console.WriteLine("Supposed to cancel.");
    };
    ci.ReqExecute += (input) =>
    {
        Console.WriteLine("This is the input: " + input);
    };

    while (!exit)
    {
        ConsoleKeyInfo key = Console.ReadKey(intercept: true);
        ci.HandleKey(key);
    }
}

/*
while (!exit)
{
    Console.Write("> ");
    StringBuilder sb = new();

    while (!exit)
    {
        ConsoleKeyInfo key = Console.ReadKey(intercept: true);
        if (key.Key == ConsoleKey.Escape)
        {
            if (sb.Length > 0)
            {
                // This is meant to clear what's already written:
                Console.Write("\r" + new string(' ', sb.Length + 2) + "\r" + "> ");
                sb.Clear();
                caret = 0;
                continue;
            }
            else
            {
                exit = true;
                break;
            }
        }
        if (key.Key == ConsoleKey.Backspace)
        {
            if (sb.Length > 0)
            {
                sb.Length--;
                caret--;
                Console.Write("\b \b");
            }
            continue;
        }
        if (key.Key == ConsoleKey.Enter)
        {
            if (key.Modifiers.HasFlag(ConsoleModifiers.Control))
            {
                sb.Insert(caret, '\n');
                Console.WriteLine("  ");
                caret += 3;
                continue;
            }
            
            Console.WriteLine();
            string input = sb.ToString();
            sb.Clear();
            caret = 0;
            if (string.IsNullOrWhiteSpace(input)) break;
            if (input.Equals("exit", StringComparison.OrdinalIgnoreCase))
            {
                exit = true;
                break;
            }
            if (input.Equals("clear", StringComparison.OrdinalIgnoreCase))
            {
                Console.Clear();                
                continue;
            }

            using CancellationTokenSource cts = new();
            Task keyWatcher = Task.Run(async () =>
            {
                while (!cts.IsCancellationRequested)
                {
                    if (!Console.KeyAvailable)
                    {
                        await Task.Delay(25);
                        continue;
                    }

                    ConsoleKeyInfo k = Console.ReadKey(intercept: true);
                    if (k.Key == ConsoleKey.Escape)
                    {
                        cts.Cancel();
                        break;
                    }
                }
            });

            try { await puppet.ExecuteAsync(input, cts.Token); }
            catch (OperationCanceledException) { Console.WriteLine("Cancelled (program)."); }
            finally 
            {
                cts.Cancel();
                try { await keyWatcher; } catch (OperationCanceledException) { }
            }
            break;
        }
        if (!char.IsControl(key.KeyChar))
        {
            sb.Insert(caret++, key.KeyChar);
            Console.Write(key.KeyChar);
        }   
        if (key.Key == ConsoleKey.Home)
        {
            Console.Write("\r");
            caret = 0;
        }
        if (key.Key == ConsoleKey.LeftArrow)
        {
            Console.Write("\b");
            caret--;
        }
    }
}
*/
Console.WriteLine("Exiting.");


