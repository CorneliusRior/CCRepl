using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Puppet.Tools
{
    public sealed class ConsoleInputEditor
    {
        public Action<string>? ReqExecute;
        public Action<string, int, int, int>? ReqRender { get; set; }
        public Action? ReqCancel { get; set; }

        private readonly List<string> _history = [];
        private readonly StringBuilder _sb = new();

        private string _prompt;
        private string _draft;
        private int _caret;
        private int _row;
        private int _historyIndex;

        public ConsoleInputEditor(string prompt, int row)
        {
            _prompt = prompt;
            _row = row;
            _caret = 0;
            _draft = "";
            Render();
        }

        private void Render()
        {
            ReqRender?.Invoke(_prompt + _sb.ToString(), _draft.Length + _prompt.Length, _caret + _prompt.Length, _row);
            _draft = _sb.ToString();
        }

        public void HandleKey(ConsoleKeyInfo key)
        {
            switch (key.Key)
            {
                case ConsoleKey.Escape:
                    Cancel(); break;
                case ConsoleKey.Backspace:
                    BackSpace(); break;
                case ConsoleKey.Enter:
                    Enter(key); break;
                case ConsoleKey.Home:
                    Home(); break;
                case ConsoleKey.End:
                    End(); break;
                case ConsoleKey.LeftArrow:
                    Left(); break;
                case ConsoleKey.RightArrow:
                    Right(); break;

                default:
                    CharKey(key); break;
            }
            Render();
        }

        private void Cancel()
        {
            if (_sb.Length > 0)
            {
                _sb.Clear();
                _caret = 0;
            }
            else
            {
                _sb.Clear();
                _caret = 0;
                Render();
                ReqCancel?.Invoke();
            }
        }

        private void BackSpace()
        {
            if (_caret > 0) _sb.Remove(_caret-- - 1, 1);
        }

        private void Enter(ConsoleKeyInfo key)
        {
            if (key.Modifiers.HasFlag(ConsoleModifiers.Control) || key.Modifiers.HasFlag(ConsoleModifiers.Shift))
            {
                _sb.Insert(_caret++, '\n');
            }
            else ReqExecute?.Invoke(_sb.ToString());
        }

        private void Home() => _caret = 0;

        private void End() => _caret = _draft.Length;

        private void Left() => _caret--;

        private void Right() => _caret++;

        private void CharKey(ConsoleKeyInfo key)
        {
            if (!char.IsControl(key.KeyChar)) _sb.Insert(_caret++, key.KeyChar);
        }
    }

}
