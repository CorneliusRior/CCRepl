
using CCRepl.Models;
using CCRepl.Tools;
using System.Text;
using System.Windows.Controls;
using System.Windows.Threading;

namespace CCRepl.WPF
{
    public sealed class WPFReplSurface
    {
        private readonly Repl _repl;
        private readonly TextBox _otp; // output
        private readonly TextBox _inp; // input
        private readonly Dispatcher _dispatcher;

        private readonly StringBuilder sb = new(); // transcript
        private string _status = "";

        private TaskCompletionSource<string>? _pendingInput;

        private List<string> _history;
        bool _browsingHistory = false;
        private int _historyIndex;
        private string _currentDraft = "";

        public WPFReplSurface(Repl repl, TextBox input, TextBox output, Dispatcher dispatcher, List<string>? history = null)
        {
            _repl = repl;
            _inp = input;
            _otp = output;
            _dispatcher = dispatcher;

            _repl.ReqClose += CloseAsync;
            _repl.ReqClear += Clear;
            _repl.ReqWrite += Write;
            _repl.ReqWriteLine += WriteLine;
            _repl.ReqWriteStatus += WriteStatus;
            _repl.ReqClearStatus += ClearStatus;
            _repl.ReqInputAsync += ReqInputAsync;

            _history = history ?? [];
        }

        public WPFReplSurface(TextBox input, TextBox output, Dispatcher dispatcher, params ICommandSet[] commandSets)
        {
            _repl = new Repl(commandSets);
            _inp = input;
            _otp = output;
            _dispatcher = dispatcher;

            _repl.ReqClose += CloseAsync;
            _repl.ReqClear += Clear;
            _repl.ReqWrite += Write;
            _repl.ReqWriteLine += WriteLine;
            _repl.ReqWriteStatus += WriteStatus;
            _repl.ReqClearStatus += ClearStatus;
            _repl.ReqInputAsync += ReqInputAsync;

            _history = [];
        }

        public WPFReplSurface(TextBox input, TextBox output, Dispatcher dispatcher, List<string> history, params ICommandSet[] commandSets)
        {
            _repl = new Repl(commandSets);
            _inp = input;
            _otp = output;
            _dispatcher = dispatcher;

            _repl.ReqClose += CloseAsync;
            _repl.ReqClear += Clear;
            _repl.ReqWrite += Write;
            _repl.ReqWriteLine += WriteLine;
            _repl.ReqWriteStatus += WriteStatus;
            _repl.ReqClearStatus += ClearStatus;
            _repl.ReqInputAsync += ReqInputAsync;

            _history = history;
        }


        public async void CloseAsync(string msg)
        {
            await _dispatcher.Invoke(async () =>
            {
                Write((string.IsNullOrWhiteSpace(msg) ? "(No Message)" : msg).ToBox(vPadding: 1, hPadding: 3, title: "Application Closing"));
                await Task.Delay(TimeSpan.FromSeconds(5));
                System.Windows.Application.Current.Shutdown();
            });
        }

        public void Clear(string msg)
        {
            _dispatcher.Invoke(() =>
            {
                sb.Clear();
                if (!string.IsNullOrWhiteSpace(msg)) sb.Append(msg);
                RefreshOutput();
            });
        }
        public void Write(string msg)
        {
            _dispatcher.Invoke(() =>
            {
                sb.Append(msg);
                RefreshOutput();
            });
        }

        public void WriteLine(string msg)
        {
            _dispatcher.Invoke(() =>
            {
                sb.AppendLine(msg);
                RefreshOutput();
            });
        }

        public void WriteStatus(string msg)
        {
            _dispatcher.Invoke(() =>
            {
                _status = msg;
                RefreshOutput();
            });
        }

        public void ClearStatus(string msg)
        {
            _dispatcher.Invoke(() =>
            {
                if (!string.IsNullOrWhiteSpace(msg)) sb.AppendLine(msg);
                _status = "";
                RefreshOutput();
            });
        }

        public async Task<string> ReqInputAsync(string msg, CancellationToken ct)
        {
            WriteLine(msg);
            TaskCompletionSource<string> tcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);

            _dispatcher.Invoke(() =>
            {
                _pendingInput = tcs;
                _inp.Focus();
            });

            if (ct.CanBeCanceled) ct.Register(() => tcs.TrySetCanceled(ct));
            return await tcs.Task;
        }

        // Input helpers:
        public bool TrySubmitInput(string text)
        {
            if (_pendingInput is null) return false;
            var tcs = _pendingInput;
            _pendingInput = null;
            tcs.TrySetResult(text);
            return true;
        }

        public async Task ExecuteAsync(string input, CancellationToken ct)
        {
            WriteLine($"> {input}");
            await _repl.ExecuteAsync(input, ct);
        }

        public void Cancel()
        {
            _pendingInput?.TrySetCanceled();
            if (!string.IsNullOrWhiteSpace(_status)) ClearStatus("Cancalled");
        }

        private void RefreshOutput()
        {
            _otp.Text = sb.ToString() + _status;
            _otp.CaretIndex = _otp.Text.Length;
            _otp.ScrollToEnd();
        }

        // History functions:
        public void AddToHistory(string str)
        {
            if (_history.Count == 0 || !string.Equals(_history[^1], str, StringComparison.Ordinal)) _history.Add(str);
        }

        private void LoadHistory(int index)
        {
            _inp.Clear();
            _inp.AppendText(_history[index]);
            _inp.CaretIndex = _inp.Text.Length;
        }

        public void HistoryUp()
        {
            if (_history.Count == 0) return;
            if (!_browsingHistory)
            {
                _currentDraft = _inp.Text;
                _browsingHistory = true;
                _historyIndex = _history.Count;
            }
            if (_historyIndex > 0) _historyIndex--;
            LoadHistory(_historyIndex);
        }

        public void HistoryDown()
        {
            if (!_browsingHistory) return;
            if (_historyIndex < _history.Count - 1)
            {
                _historyIndex++;
                LoadHistory(_historyIndex);
                return;
            }

            _historyIndex = _history.Count;
            _inp.Clear();
            _inp.AppendText(_currentDraft);
            _browsingHistory = false;
        }
    }

}
