using CCRepl.Models;
using System.Text;

namespace CCRepl.Tools;

public static class StringHelpers
{
    public static string ToSingleLine(this string input) => input.Replace("\r\n", " ").Replace("\r", " ").Replace("\n", " ");
    public static string? ToSingleLineNullable(this string? input) => input is null ? null : input.Replace("\r\n", " ").Replace("\r", " ").Replace("\n", " ");
    public static string Unindent(this string input) => input.Replace("\t", "");

    /// <summary>
    /// Truncates strength to desired length, adding truncateString to the end if cut off.
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

    
    public static string TruncatePadRight(this string input, int length, string truncateString = "…") => input.Truncate(length, truncateString).PadRight(length);

    public static string TruncatePadLeft(this string input, int length, string truncateString = "…") => input.Truncate(length, truncateString).PadLeft(length);

    public static string? TruncateNullable(this string? input, int length, string truncateString = "…")
    {
        if (input is null) return null;
        return input.Truncate(length, truncateString);
    }

    public static string ToStringTruncate(this double input, int length, string format = "0.#", string prefix = "", string suffix = "", string truncateString = "…")
    {
        string output = prefix + input.ToString(format) + suffix;
        output = output.Truncate(length, truncateString);
        return output;
    }

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
    }      
    
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
    }

    /* Method to print tables. Will print out all the box things:
     ─ │ ┌ ┐ └ ┘ ├ ┼ ┤ ┴ ┬

    ┌─┬─┐ 218 196 194 196 191       │ = 179     ┤ = 180     ┐ = 191     └ = 192
    │ │ │ 179     179     179       
    ├─┼─┤ 195 196 197 196 180       ┴ = 193     ┬ = 194     ├ = 195     ─ = 196
    │ │ │ 179     179     179       
    └─┴─┘ 192 196 193 196 217       ┼ = 197     ┘ = 217     ┌ = 218

    */

    
}

public sealed record PrintTable
{
    public string[] Headers { get; init; }
    public List<string?[]> Items { get; set; }
    public int[] ColumnWidths { get; init; }
    public bool[] AlignRight { get; init; }

    // New:
    public PrintTable(string[] headers, List<string?[]> items, int[] columnWidths, bool[] alignRight)
    {
        Headers = headers;
        Items = items;
        ColumnWidths = columnWidths;
        AlignRight = alignRight;
        Validate();
    }

    public PrintTable(string[] headers, int[] columnWidths, bool[] alignRight) //: this(headers, [], columnWidths, alignRight)
    {
        Headers = headers;
        ColumnWidths = columnWidths;
        AlignRight = alignRight;
        Items = [];
        Validate();
    }
    
    public PrintTable(string[] headers, int[] columnWidths) //: this(headers, [], columnWidths, [])
    {
        Headers = headers;
        ColumnWidths = columnWidths;
        AlignRight = [];
        Items = [];
        Validate();
    }

    public void AddItem(string?[] line)
    {
        if (line.Length != Headers.Length) throw new ReplException($"Table Headers and line '{Items.Count + 1}' are different lengths, must be identical. Headers.Length='{Headers.Length}', line length='{line.Length}'.");
        Items.Add(line);
    }

    public void AddItems(List<string?[]> lines)
    {
        foreach (string?[] l in lines) if (l.Length != Headers.Length) throw new ReplException($"Table Headers and an item line are different lengths, must be identical. Headers.Length='{Headers.Length}', line length='{l.Length}'.\nLine: '{string.Join(" / ", l)}'.");
        Items.AddRange(lines);
    }

    private void Validate()
    {
        if (Headers.Length != ColumnWidths.Length) throw new ReplException($"Table Header and ColumnWidths are different lengths, must be identical. Headers.Length='{Headers.Length}', ColumnWidths.Length='{ColumnWidths.Length}'.");
        if (AlignRight.Length > Headers.Length) throw new ReplException($"AlignRight array too large, Headers.Length='{Headers.Length}', AlignRight.Length='{AlignRight.Length}'.");
        while (AlignRight.Length < Headers.Length) AlignRight.Append(false);
    }

    public string Print()
    {
        // Ensure data lines up:
        Validate();
        // Max item length also

        StringBuilder sb = new();

        // Draw Banner, top:
        sb.Append('┌');
        for (int i = 0; i < ColumnWidths.Length; i++)
        {
            sb.Append(new string('─', ColumnWidths[i]));
            if (i + 1 != ColumnWidths.Length) sb.Append('┬');
        }
        sb.Append('┐');

        // Text:
        sb.Append('\n');
        sb.Append('│');
        for (int i = 0; i < ColumnWidths.Length; i++)
        {
            sb.Append(Headers[i].TruncatePadRight(ColumnWidths[i]));
            sb.Append('│');
        }

        // Bottom:
        sb.Append('\n');
        sb.Append('├');
        for (int i = 0; i < ColumnWidths.Length; i++)
        {
            sb.Append(new string('─', ColumnWidths[i]));
            if (i + 1 != ColumnWidths.Length) sb.Append('┼');
        }
        sb.Append('┤');

        // Next, draw items:
        foreach (string?[] line in Items)
        {
            if (line.Length != Headers.Length) throw new ReplException($"Table line and table header are different lengths, must be identical (fill in blanks with 'null'):\nHeaders='{string.Join(" / ", Headers)}'\nItems='{string.Join(" / ", line)}'");
            sb.Append('\n');
            sb.Append('│');
            for (int i = 0; i < ColumnWidths.Length; i++)
            {
                if (AlignRight[i]) sb.Append((line[i] ?? "-").TruncatePadLeft(ColumnWidths[i]));
                else sb.Append((line[i] ?? "-").TruncatePadRight(ColumnWidths[i]));
                sb.Append('│');
            }
        }

        // Finally, draw the bottom of the table:
        sb.Append('\n');
        sb.Append('└');
        for (int i = 0; i < ColumnWidths.Length; i++)
        {
            sb.Append(new string('─', ColumnWidths[i]));
            if (i + 1 != ColumnWidths.Length) sb.Append('┴');
        }
        sb.Append('┘');

        // Return
        return (sb.ToString());
    }
}