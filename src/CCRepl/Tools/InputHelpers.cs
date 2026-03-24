using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;

namespace CCRepl.Tools
{
    public static class InputHelpers
    {
        public static Task ConsoleCancelKeyWatcher(CancellationTokenSource cts, ConsoleKey cancelKey = ConsoleKey.Escape, int pollDelayMs = 25) =>
            Task.Run(async () =>
            {
                while (!cts.IsCancellationRequested)
                {
                    if (!Console.KeyAvailable)
                    {
                        await Task.Delay(pollDelayMs, cts.Token);
                        continue;
                    }

                    ConsoleKeyInfo key = Console.ReadKey(intercept: true);
                    if (key.Key == cancelKey)
                    {
                        cts.Cancel();
                        break;
                    }
                }
            });
    }

    public sealed class ConsoleInputEditor
    {
        private readonly string _prompt;
        private readonly List<string> _history;
        private readonly StringBuilder _sb = new();

        private int _caret;
        private int _row;
        private int _previous;
        private int _displayStart;

        private int _historyIndex;
        private string _currentDraft = "";
        private bool _browsingHistory;

        public ConsoleInputEditor(string prompt, List<string>? history = null)
        {
            _prompt = prompt;
            _history = history ?? [];
        }

        private void Render()
        {
            int lineWidth = Console.WindowWidth;
            int displayWidth = Math.Max(1, lineWidth - _prompt.Length - 1);
            if (_caret < _displayStart) _displayStart = _caret;
            if (_caret > _displayStart + displayWidth) _displayStart = _caret - displayWidth;

            string fullText = _sb.ToString();
            string displayText = fullText.Length <= _displayStart ? fullText : fullText.Substring(_displayStart, Math.Min(displayWidth, fullText.Length - _displayStart));

            string text = _prompt + displayText;
            int clearLength = Math.Max(_previous, text.Length);
            Console.SetCursorPosition(0, _row);
            Console.Write(text);
            if (clearLength > text.Length) Console.Write(new string(' ', clearLength - text.Length));

            _previous = text.Length;
            int caret = _prompt.Length + (_caret - _displayStart);
            Console.SetCursorPosition(caret, _row);
        }

        private void ClearRenderedLine()
        {
            int clearLength = Math.Max(_previous, _prompt.Length + _sb.Length);
            Console.SetCursorPosition(0, _row);
            Console.Write(new string(' ', clearLength));
            Console.SetCursorPosition(0, _row);
            _previous = 0;
        }

        public async Task<ConsoleResult> ReadLineAsync(CancellationToken ct = default)
        {
            _sb.Clear();
            _caret = 0;
            _previous = 0;

            _historyIndex = _history.Count;
            _currentDraft = "";
            _browsingHistory = false;

            _row = Console.CursorTop;
            Render();

            while (true)
            {
                ct.ThrowIfCancellationRequested();
                ConsoleKeyInfo key = Console.ReadKey(intercept: true);

                switch (key.Key)
                {
                    case ConsoleKey.Escape:
                        if (_sb.Length > 0)
                        {
                            _sb.Clear();
                            _caret = 0;
                            _browsingHistory = false;
                            Render();
                            continue;
                        }
                        ClearRenderedLine();
                        Console.WriteLine();
                        return ConsoleResult.Cancel();
                    case ConsoleKey.Enter:
                        if (key.Modifiers.HasFlag(ConsoleModifiers.Control) || key.Modifiers.HasFlag(ConsoleModifiers.Shift))
                        {
                            _sb.Insert(_caret++, '¶');
                            Render();
                            continue;
                        }
                        string submit = _sb.ToString();
                        ClearRenderedLine();
                        //Console.WriteLine();
                        if (!string.IsNullOrWhiteSpace(submit)) AddToHistory(submit);
                        return ConsoleResult.Submit(submit.Replace('¶', '\n'));
                    case ConsoleKey.Backspace:
                        if (_caret > 0)
                        {
                            if (key.Modifiers.HasFlag(ConsoleModifiers.Control))
                            {
                                if (key.Modifiers.HasFlag(ConsoleModifiers.Shift))
                                {
                                    _sb.Remove(0, _caret);
                                    _caret = 0;
                                    _browsingHistory = false;
                                    Render();
                                    continue;
                                }
                                int wl = 0;

                                // Get everything in the last word:
                                for (int i = _caret - 1; i >= 0; i--)
                                {
                                    if (_sb[i] == ' ') break;
                                    if (_sb[i] == '.' && wl > 0) break;
                                    else wl++;
                                }

                                /* Remove any remaining spaces:
                                for (int i = _caret - wl - 1; i >= 0; i--)
                                {
                                    if (_sb[i] == ' ') wl++;
                                    else break;
                                } */ // Got rid of this, put it back if you like. 

                                _sb.Remove(_caret - wl, wl);
                                _caret -= wl;
                                _browsingHistory = false;
                                Render();
                            }
                            else
                            {
                                _sb.Remove(_caret - 1, 1);
                                _caret--;
                                _browsingHistory = false;
                                Render();
                            }                            
                        }
                        continue;
                    case ConsoleKey.Delete:
                        if (_caret < _sb.Length)
                        {
                            if (key.Modifiers.HasFlag(ConsoleModifiers.Control))
                            {
                                if (key.Modifiers.HasFlag(ConsoleModifiers.Shift))
                                {
                                    _sb.Remove(_caret, _sb.Length - _caret);
                                    _browsingHistory = false;
                                    Render();
                                    continue;
                                }
                                int wl = 0;

                                // Get length of next word:
                                for (int i = _caret; i < _sb.Length; i++)
                                {
                                    if (_sb[i] == ' ') break;
                                    if (_sb[i] == '.' && wl > 0)
                                    {
                                        wl++;
                                        break;
                                    }
                                    else wl++;
                                }

                                // Get length of remaining spaces:
                                for (int i = _caret + wl; i < _sb.Length; i++)
                                {
                                    if (_sb[i] == ' ') wl++;
                                    else break;
                                }

                                _sb.Remove(_caret, wl);
                                _browsingHistory = false;
                                Render();
                            }
                            else
                            {
                                _sb.Remove(_caret, 1);
                                _browsingHistory = false;
                                Render();
                            }
                        }
                        continue;
                    case ConsoleKey.LeftArrow:
                        if (_caret > 0)
                        {
                            if (key.Modifiers.HasFlag(ConsoleModifiers.Control))
                            {
                                int wl = 0;
                                for (int i = _caret - 1; i >= 0; i--)
                                {
                                    if (_sb[i] == ' ') break;
                                    if (_sb[i] == '.' && wl > 0) break;
                                    else wl++;
                                }
                                for (int i = _caret - wl - 1; i >= 0; i--)
                                {
                                    if (_sb[i] == ' ') wl++;
                                    else break;
                                }
                                _caret -= wl;
                                Render();
                                continue;
                            }
                            _caret--;
                            Render();
                        }
                        continue;
                    case ConsoleKey.RightArrow:
                        if (_caret < _sb.Length)
                        {
                            if (key.Modifiers.HasFlag(ConsoleModifiers.Control))
                            {
                                int wl = 0;
                                for (int i = _caret; i < _sb.Length; i++)
                                {
                                    if (_sb[i] == ' ') break;
                                    if (_sb[i] == '.' && wl > 0)
                                    {
                                        wl++;
                                        break;
                                    }
                                    else wl++;
                                }
                                for (int i = _caret + wl; i < _sb.Length; i++)
                                {
                                    if (_sb[i] == ' ') wl++;
                                    else break;
                                }
                                _caret += wl;
                                Render();
                                continue;
                            }
                            _caret++;
                            Render();
                        }
                        continue;
                    case ConsoleKey.Home:
                        if (_caret != 0)
                        {
                            _caret = 0;
                            Render();
                        }
                        continue;
                    case ConsoleKey.End:
                        if (_caret != _sb.Length)
                        {
                            _caret = _sb.Length;
                            Render();
                        }
                        continue;
                    case ConsoleKey.UpArrow:
                        HistoryUp();
                        Render();
                        continue;
                    case ConsoleKey.DownArrow:
                        HistoryDown();
                        Render();
                        continue;

                    default:
                        if (!char.IsControl(key.KeyChar))
                        {
                            _sb.Insert(_caret, key.KeyChar);
                            _caret++;
                            _browsingHistory = false;
                            Render();
                        }
                        continue;
                }
            }
        }

        private void AddToHistory(string value)
        {
            if (_history.Count == 0 || !string.Equals(_history[^1], value, StringComparison.Ordinal)) _history.Add(value);
        }

        private void HistoryUp()
        {
            if (_history.Count == 0) return;
            if (!_browsingHistory)
            {
                _currentDraft = _sb.ToString();
                _browsingHistory = true;
                _historyIndex = _history.Count;
            }
            if (_historyIndex > 0) _historyIndex--;
            LoadHistory(_historyIndex);
        }

        private void HistoryDown()
        {
            if (!_browsingHistory) return;
            if (_historyIndex < _history.Count - 1)
            {
                _historyIndex++;
                LoadHistory(_historyIndex);
                return;
            }

            _historyIndex = _history.Count;
            _sb.Clear();
            _sb.Append(_currentDraft);
            _caret = _sb.Length;
            _browsingHistory = false;
        }

        private void LoadHistory(int index)
        {
            _sb.Clear();
            _sb.Append(_history[index]);
            _caret = _sb.Length;
        }
    }

    public sealed record ConsoleResult(bool Cancelled, string Text)
    {
        public static ConsoleResult Submit(string text) => new(false, text);
        public static ConsoleResult Cancel() => new(true, "");
    }
}
