using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
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

        public static void WPFHandlePreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (sender is not TextBox) return;
            TextBox tb = (TextBox)sender;

            if (e.Text is "\"" or "}" or "]" or ")")
            {
                if (tb.CheckNext(e.Text))
                {
                    tb.CaretIndex += e.Text.Length;
                    e.Handled = true;
                    return;
                }
            }
            if (e.Text is "\"" or "{" or "[" or "(")
            {
                string close = e.Text switch
                {
                    "\"" => "\"",
                    "{" => "}",
                    "[" => "]",
                    "(" => ")",
                    _ => ""
                };
                tb.InsertPair(e.Text, close);
                e.Handled = true;
            }
        }

        public static KeyAction WPFHandleKeyDown(object sender, KeyEventArgs e, ref int tabDepth)
        {
            if (sender is not TextBox) return KeyAction.None;
            TextBox tb = (TextBox)sender;

            if (e.Key == Key.Escape) return KeyAction.Cancel;

            if (e.Key == Key.Enter)
            {
                if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control) || Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
                {
                    tb.Insert(Environment.NewLine + new string('\t', tabDepth));
                    e.Handled = true;
                    return KeyAction.None;
                }
                else
                {
                    e.Handled = true;
                    return KeyAction.Submit;
                }
            }
            if (e.Key == Key.Tab)
            {
                if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
                {
                    if (tb.RemoveTab())
                    {
                        tabDepth--;
                        tabDepth = Math.Max(tabDepth, 0);
                    }
                }
                else
                {
                    tb.Insert("\t");
                    tabDepth++;                    
                }

                e.Handled = true;
                return KeyAction.None;
            }

            if (e.Key == Key.Up)
            {
                e.Handled = true;
                return KeyAction.HistoryUp;
            }

            if (e.Key == Key.Down)
            {
                e.Handled = true;
                return KeyAction.HistoryDown;
            }

            return KeyAction.None;
        }
    }

    public enum KeyAction
    {
        None,
        Cancel,
        Submit,
        HistoryUp,
        HistoryDown
    }

}
