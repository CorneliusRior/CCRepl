using CCRepl.Models;
using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace CCRepl.Tools;

public static class StringHelpers
{
    public static string ToSingleLine(this string input) => input.Replace("\r\n", " ").Replace("\r", " ").Replace("\n", " ");
    public static string? ToSingleLineNullable(this string? input) => input is null ? null : input.Replace("\r\n", " ").Replace("\r", " ").Replace("\n", " ");
    public static string Unindent(this string input) => input.Replace("\t", "");

    /// <summary>
    /// Truncates string to specified length, adding truncateString to the end if cut off.
    /// </summary>
    /// <param name="input"></param>
    /// <param name="length"></param>
    /// <param name="truncateString"></param>
    /// <returns></returns>
    public static string Truncate(this string input, int length, string truncateString = "…")
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;
        input = input.Replace("\r\n", " ").Replace("\r", " ").Replace("\n", " ");
        length = Math.Abs(length);
        if (length <= truncateString.Length) return truncateString[..(Math.Max(0, length))];
        if (input.Length <= length) return input;
        return input[..(length - truncateString.Length)] + truncateString;
    }

    /// <summary>
    /// Truncates string to specified length, adding truncateString to the end if cut off, or pads to the right if not.
    /// </summary>
    /// <param name="input"></param>
    /// <param name="length"></param>
    /// <param name="truncateString"></param>
    /// <returns></returns>
    public static string TruncatePadRight(this string input, int length, string truncateString = "…") => input.Truncate(length, truncateString).PadRight(length);

    /// <summary>
    /// Truncates string to specified length, adding truncateString to the end if cut off, or pads to the left if not.
    /// </summary>
    /// <param name="input"></param>
    /// <param name="length"></param>
    /// <param name="truncateString"></param>
    /// <returns></returns>
    public static string TruncatePadLeft(this string input, int length, string truncateString = "…") => input.Truncate(length, truncateString).PadLeft(length);

    /// <summary>
    /// Truncates nullable string to specified length, adding truncateString to the end if cut off, or returning null if null.
    /// </summary>
    /// <param name="input"></param>
    /// <param name="length"></param>
    /// <param name="truncateString"></param>
    /// <returns></returns>
    public static string? TruncateNullable(this string? input, int length, string truncateString = "…")
    {
        if (input is null) return null;
        return input.Truncate(length, truncateString);
    }

    /// <summary>
    /// Converts double to string and truncates to specified length, addubg truncateString to the end if cut off, with optional arguments for prefix, suffix, and format.
    /// </summary>
    /// <param name="input"></param>
    /// <param name="length"></param>
    /// <param name="format"></param>
    /// <param name="prefix"></param>
    /// <param name="suffix"></param>
    /// <param name="truncateString"></param>
    /// <returns></returns>
    public static string ToStringTruncate(this double input, int length, string format = "0.#", string prefix = "", string suffix = "", string truncateString = "…")
    {
        string output = prefix + input.ToString(format) + suffix;
        output = output.Truncate(length, truncateString);
        return output;
    }

    /// <summary>
    /// Converts int to string and truncates to specified length, addubg truncateString to the end if cut off, with optional arguments for prefix, suffix, and format.
    /// </summary>
    /// <param name="input"></param>
    /// <param name="length"></param>
    /// <param name="format"></param>
    /// <param name="prefix"></param>
    /// <param name="suffix"></param>
    /// <param name="truncateString"></param>
    /// <returns></returns>
    public static string ToStringTruncate(this int input, int length, string prefix = "", string suffix = "", string truncateString = "…")
    {
        string output = prefix + input.ToString() + suffix;
        output = output.Truncate(length, truncateString);
        return output;
    }

    /// <summary>
    /// Returns a string depending on if the bool is true or false. Default return value for true is "[x]". amd false if "[ ]". If invert is set true, inverts return. Used to display bools as strings. Checked and UnChecked strings can be set as anything with no character limits.
    /// </summary>
    /// <param name="check">Bool to represent as string.</param>
    /// <param name="invert">Inverts the return.</param>
    /// <param name="checkedString">String returned if "check" is true (unless invert)</param>
    /// <param name="unCheckedString">String returned if "check" is false (unless invert)</param>
    public static string Checked(this bool check, bool invert = false, string checkedString = "[x]", string unCheckedString = "[ ]" )
    {
        if (invert) return check ? unCheckedString : checkedString;
        else return check ? checkedString : unCheckedString;
    }

    /// <summary>
    /// This is for very specific kind of case. For listing things out.
    /// 
    /// Does this basically:
    /// Header: ListItem1
    ///         ListItem2
    ///         ListItem3
    /// </summary>
    /// <param name="input"></param>
    /// <param name="leftMargin"></param>
    /// <returns></returns>
    public static string AlignList(this List<string> input, int leftMargin, int? rightMargin = null)
    {
        if (input.Count == 0) return "";

        List<string> inter = new();
        if (rightMargin is not null) foreach (string l in input) inter.Add(l.Truncate(rightMargin.Value));
        else inter = input;

        if (inter.Count == 1) return inter[1];
        StringBuilder sb = new();
        sb.AppendLine(inter[0]);
        foreach (string l in inter.Skip(1)) sb.AppendLine(new string(' ', leftMargin) + l);
        return (sb.ToString());
    }

    private static void p(string msg = "Reached.", [CallerFilePath] string file = "", [CallerLineNumber] int line = 0, [CallerMemberName] string name = "")
    {
        Debug.WriteLine(file + $" (Line {line}) " + name + "(): " + msg);
    }    

    /// <summary>
    /// Wraps text to a given width (max).
    /// </summary>
    /// <param name="input"></param>
    /// <param name="max"></param>
    public static List<string> Wrap(this string input, int max)
    {
        List<string> wrapped = new();
        if (max < 1) throw new ArgumentOutOfRangeException();
        if (max == 1)
        {
            foreach (char c in input) if(c != '\r' && c != '\n') wrapped.Add(c.ToString());
            return wrapped;
        }

        string[] lines = input.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
        if (lines.Max(l => l.Length) <= max) return lines.ToList();

        foreach (string l in lines)
        {
            if (l.Length == 0)
            {
                wrapped.Add(string.Empty);
                continue;
            }

            StringBuilder current = new();
            string[] words = l.Split(' ', StringSplitOptions.None);

            foreach (string word in words)
            {
                // Handle for spaces:
                string w = current.Length == 0 ? word : " " + word;

                // Add to current line if adding new word won't hurt:
                if (w.Length + current.Length <= max)
                {
                    current.Append(w);
                    continue;
                }

                // Now we've established that we need a new line: deal with current first:
                if (current.Length > 0)
                {
                    wrapped.Add(current.ToString());
                    current.Clear();
                }

                // Is the word itself too long?:
                if (word.Length > max)
                {
                    int index = 0;
                    while (index < word.Length)
                    {
                        int remaining = word.Length - index;
                        if (remaining <= max)
                        {
                            wrapped.Add(word.Substring(index));
                            break;
                        }
                        wrapped.Add(word.Substring(index, max - 1) + '-');
                        index += max - 1;
                    }
                }
                else
                {
                    current.Append(word);
                }                
            }
            if (current.Length > 0) wrapped.Add(current.ToString());
        }
        return wrapped;
    }
    
    /*
    public static string ToBox(this string msg)
    {
        if (string.IsNullOrWhiteSpace(msg)) return "┌─┐\n└─┘";

        string[] lines = msg.Split(new[] {"\r\n", "\n" }, StringSplitOptions.None);
        int msgWidth = lines.Max(s => s.Length);
        int msgHeight = lines.Length;

        string vert = new string('─', msgWidth + 2);
        StringBuilder sb = new();
        sb.AppendLine('┌' + vert + '┐');
        foreach (string l in lines) sb.AppendLine("│ " + l.PadRight(msgWidth) + " │");
        sb.AppendLine('└' + vert + '┘');
        return sb.ToString();
    }*/

    /// <summary>
    /// Draws a box around msg using box characters, with optional title which appears in the top left.
    /// </summary>
    /// <param name="msg"></param>
    /// <param name="boxWidth">Manually set width of the box</param>
    /// <param name="minBoxWidth">Minimum box size</param>
    /// <param name="maxBoxWidth">Maximum box size</param>
    /// <param name="vPadding">Blank space above and below the body</param>
    /// <param name="hPadding">Blank space to left and right of the body</param>
    /// <param name="title">String which appears in the top left</param>
    /// <returns></returns>
    public static string ToBox(this string msg, int? boxWidth = null, int? minBoxWidth = null, int? maxBoxWidth = null, int vPadding = 0, int hPadding = 0, string title = "")
    {
        // Determine the width:
        int width;
        string[] lineArray = msg.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

        if (boxWidth is not null) width = boxWidth.Value;
        else width = Math.Max(title.Length + 4, lineArray.Max(s => s.Length));
        if (maxBoxWidth is not null && width > maxBoxWidth) width = maxBoxWidth.Value;
        if (minBoxWidth is not null && width < minBoxWidth) width = minBoxWidth.Value;

        List<string> lines = msg.Wrap(width - (hPadding * 2));
        if (string.IsNullOrWhiteSpace(lines[^1])) lines.Remove(lines[^1]);
        StringBuilder box = new();

        title = string.IsNullOrWhiteSpace(title) ? "──" : '[' + title + ']';

        // Draw box:
        box.AppendLine("┌─" + title.Truncate(width - 2) + new string('─', width - title.Length - 2) + "─┐");
        for (int i = vPadding; i > 0; i--) box.AppendLine('│' + new string(' ', width) + '│');
        foreach (string l in lines) box.AppendLine('│' + new string(' ', hPadding) + l.PadRight(width - (hPadding * 2)) + new string(' ', hPadding) + '│');
        for (int i = vPadding; i > 0; i--) box.AppendLine('│' + new string(' ', width) + '│');
        box.AppendLine('└' + new string('─', width) + '┘');

        return box.ToString();
    }

    /// <summary>
    /// Draws a box around msg using double box characters, with optional title which appears in the top left.
    /// </summary>
    /// <param name="msg"></param>
    /// <param name="boxWidth">Manually set width of the box</param>
    /// <param name="minBoxWidth">Minimum box size</param>
    /// <param name="maxBoxWidth">Maximum box size</param>
    /// <param name="vPadding">Blank space above and below the body</param>
    /// <param name="hPadding">Blank space to left and right of the body</param>
    /// <param name="title">String which appears in the top left</param>
    /// <returns></returns>
    public static string ToDoubleBox(this string msg, int? boxWidth = null, int? minBoxWidth = null, int? maxBoxWidth = null, int vPadding = 0, int hPadding = 0, string title = "")
    {
        // Determine the width:
        int width;
        string[] lineArray = msg.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

        if (boxWidth is not null) width = boxWidth.Value;
        else width = Math.Max(title.Length + 4, lineArray.Max(s => s.Length));
        if (maxBoxWidth is not null && width > maxBoxWidth) width = maxBoxWidth.Value;
        if (minBoxWidth is not null && width < minBoxWidth) width = minBoxWidth.Value;

        List<string> lines = msg.Wrap(width - (hPadding * 2));
        StringBuilder box = new();

        title = string.IsNullOrWhiteSpace(title) ? "══" : '╣' + title + '╠';

        // Draw box:
        if (title.Length > 2) box.AppendLine("  ╔" + new string('═', Math.Min(title.Length - 2, width - 4)) + '╗');
        box.AppendLine("╔═" + title.Truncate(width - 2) + new string('═', width - title.Length - 2) + "═╗");
        if (title.Length > 2) box.AppendLine("║ ╚" + new string('═', Math.Min(title.Length - 2, width - 4)) + '╝' + new string(' ', width - title.Length - 1) + '║');
        for (int i = vPadding; i > 0; i--) box.AppendLine('║' + new string(' ', width) + '║');
        foreach (string l in lines) box.AppendLine('║' + new string(' ', hPadding) + l.PadRight(width - (hPadding * 2)) + new string(' ', hPadding) + '║');
        for (int i = vPadding; i > 0; i--) box.AppendLine('║' + new string(' ', width) + '║');
        box.AppendLine('╚' + new string('═', width) + '╝');

        return box.ToString();
    }

    /*
    public static string ToDoubleBox(this string msg)
    {
        if (string.IsNullOrWhiteSpace(msg)) return "╔═╗\n╚═╝";

        string[] lines = msg.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
        int msgWidth = lines.Max(s => s.Length);
        int msgHeight = lines.Length;

        string vert = new string('═', msgWidth + 2);
        StringBuilder sb = new();
        sb.AppendLine('╔' + vert + '╗');
        foreach (string l in lines) sb.AppendLine("║ " + l.PadRight(msgWidth) + " ║");
        sb.AppendLine('╚' + vert + '╝');
        return sb.ToString();
    }*/

    /* Method to print tables. Will print out all the box things:
     ─ │ ┌ ┐ └ ┘ ├ ┼ ┤ ┴ ┬

    ┌─┬─┐ 218 196 194 196 191       │ = 179     ┤ = 180     ┐ = 191     └ = 192
    │ │ │ 179     179     179       
    ├─┼─┤ 195 196 197 196 180       ┴ = 193     ┬ = 194     ├ = 195     ─ = 196
    │ │ │ 179     179     179       
    └─┴─┘ 192 196 193 196 217       ┼ = 197     ┘ = 217     ┌ = 218

    ╔═╦═╗ 201 205 203 205 187       ║ = 186     ╣ = 185     ╗ = 187     ╚ = 200
    ║ ║ ║ 186     186     186       
    ╠═╬═╣ 204 205 206 205 185       ╩ = 202     ╦ = 203     ╠ = 204     ═ = 205
    ║ ║ ║ 186     186     186       
    ╚═╩═╝ 200 205 202 205 188       ╬ = 206     ╝ = 188     ╔ = 201

    */


}

/// <summary>
/// Definition for a row which can be used in a <see cref="PrintTable"/>
/// </summary>
/// <example>
/// public static List<PrintTableColumn> Columns =>
/// [
///     new("Id", 4, false),
///     new("Name", 24, false),
///     new("Date of Birth", 15, true)
/// ];
/// </example>
public sealed record PrintTableColumn
{
    public string Header { get; init; }
    public int Width { get; init; }
    public bool AlignRight { get; init; }

    // New
    public PrintTableColumn(string header, int width, bool alignRight)
    {
        Header = header;
        Width = width;
        AlignRight = alignRight;
    }

    public PrintTableColumn(string header, int width)
    {
        Header = header;
        Width = width;
        AlignRight = false;
    }
}

/// <summary>
/// Table which can be rendered using box characters. Columns defined with <see cref="PrintTableColumn"/>
/// </summary>
/// <example>
/// // This could be put in a class:
/// public static List<PrintTableColumn> Columns =>
/// [
///     new("Id", 4, false),
///     new("Name", 24, false),
///     new("Date of Birth", 15, true)
/// ];
/// 
/// public string?[] Items =>
/// [
///     Id,
///     Name,
///     DateOfBirth.ToString("d")
/// ];
/// 
/// // And then called like:
/// List<Profile> profiles;
/// List<string?[]> itemList;
/// foreach (Profile p in profiles) itemList.Add(p.Items);
/// PrintTable table = new(Profile.Columns, itemList);
/// </example>
public sealed record PrintTable
{
    public List<PrintTableColumn> Columns { get; init; }
    public List<string?[]> Items { get; set; }

    // New:
    public PrintTable(List<PrintTableColumn> columns, List<string?[]> items)
    {
        Columns = columns;
        Items = items;
        Validate();
    }

    public PrintTable(List<PrintTableColumn> columns) 
    {
        Columns = columns;
        Items = [];
        Validate();
    }

    public void AddItem(string?[] line)
    {
        if (line.Length != Columns.Count) throw new ReplException($"Table and line '{Items.Count + 1}' have different columns amount, must be identical. Columns.Count='{Columns.Count}', line length='{line.Length}'.");
        Items.Add(line);
    }

    public void AddItems(List<string?[]> lines)
    {
        foreach (string?[] l in lines) if (l.Length != Columns.Count) throw new ReplException($"Table and an item line have different column amount, must be identical. Columns.Count='{Columns.Count}', line length='{l.Length}'.\nLine: '{string.Join(" / ", l)}'.");
        Items.AddRange(lines);
    }

    private void Validate()
    {
        foreach (string?[] l in Items) if (l.Length != Columns.Count) throw new ReplException($"Table and an item line have different column amount, must be identical. Columns.Count='{Columns.Count}', line length='{l.Length}'.\nLine: '{string.Join(" / ", l)}'.");
    }

    public string Print()
    {
        // Ensure data lines up:
        Validate();
        // Max item length also

        StringBuilder sb = new();

        // Draw Banner, top:
        sb.Append('┌');
        for (int i = 0; i < Columns.Count; i++)
        {
            sb.Append(new string('─', Columns[i].Width));
            if (i + 1 != Columns.Count) sb.Append('┬');
        }
        sb.Append('┐');

        // Text:
        sb.Append('\n');
        sb.Append('│');
        for (int i = 0; i < Columns.Count; i++)
        {
            sb.Append(Columns[i].Header.TruncatePadRight(Columns[i].Width));
            sb.Append('│');
        }

        // Bottom:
        sb.Append('\n');
        sb.Append('├');
        for (int i = 0; i < Columns.Count; i++)
        {
            sb.Append(new string('─', Columns[i].Width));
            if (i + 1 != Columns.Count) sb.Append('┼');
        }
        sb.Append('┤');

        // Next, draw items:
        foreach (string?[] line in Items)
        {
            sb.Append('\n');
            sb.Append('│');
            for (int i = 0; i < Columns.Count; i++)
            {
                if (Columns[i].AlignRight) sb.Append((line[i] ?? "-").TruncatePadLeft(Columns[i].Width));
                else sb.Append((line[i] ?? "-").TruncatePadRight(Columns[i].Width));
                sb.Append('│');
            }
        }

        // Finally, draw the bottom of the table:
        sb.Append('\n');
        sb.Append('└');
        for (int i = 0; i < Columns.Count; i++)
        {
            sb.Append(new string('─', Columns[i].Width));
            if (i + 1 != Columns.Count) sb.Append('┴');
        }
        sb.Append('┘');

        // Return
        return (sb.ToString());
    }
}