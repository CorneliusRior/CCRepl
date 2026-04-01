
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

        public WPFReplSurface(Repl repl, TextBox input, TextBox output, Dispatcher dispatcher)
        {
            _repl = repl;
            _inp = input;
            _otp = output;
            _dispatcher = dispatcher;

            repl.ReqWrite += Write;
            repl.ReqWriteLine += WriteLine;
            repl.ReqWriteStatus += WriteStatus;
            repl.ReqClearStatus += ClearStatus;
            repl.ReqInputAsync += ReqInputAsync;
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
    }

}
