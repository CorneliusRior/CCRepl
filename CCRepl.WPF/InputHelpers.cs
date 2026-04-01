using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Shell;

namespace CCRepl.WPF
{
    public static class InputHelpers
    {
        public static void Insert(this TextBox input, string str)
        {
            int ci = input.CaretIndex;
            input.Text = input.Text.Insert(ci, str);
            input.CaretIndex = ci + str.Length;
        }

        public static void InsertPair(this TextBox input, string open, string close)
        {
            int start = input.SelectionStart;
            int length = input.SelectionLength;
            string selected = input.SelectedText;

            if (!string.IsNullOrEmpty(selected))
            {
                input.Text = input.Text.Remove(start, length).Insert(start, open + selected + close);
                input.SelectionStart = start + open.Length;
                input.SelectionLength = length;
            }
            else
            {
                input.Text = input.Text.Insert(start, open + close);
                input.SelectionStart = start + open.Length;
            }
        }

        public static bool CheckNext(this TextBox input, string str)
        {
            if (input.CaretIndex > input.Text.Length) return false;
            int len = Math.Clamp(str.Length, 0, input.Text.Length - input.CaretIndex);
            if (input.Text.Substring(input.CaretIndex, len).Equals(str, StringComparison.Ordinal)) return true;
            else return false;
        }

        public static bool RemoveTab(this TextBox input)
        {
            int ci = input.CaretIndex;
            if (ci == 0) return false;
            if (input.Text[ci - 1] == '\t')
            {
                input.Text = input.Text.Remove(ci - 1, 1);
                input.CaretIndex = ci - 1;
                return true;
            }
            return false;
        }
    }
}
