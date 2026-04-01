using CCRepl;
using CCRepl.WPF;
using ReadingList.Commands;
using ReadingList.Services;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;



namespace ReadingList.WPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Repl _repl;
        private readonly WPFReplSurface _surface;
        private CancellationTokenSource? _cts;
        private TaskCompletionSource<string>? _pendingInput;
        private bool _inputRequested;
        private int _tabDepth = 0;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;

            string dataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CCRepl_ReadingList");
            Directory.CreateDirectory(dataDir);
            string dataPath = Path.Combine(dataDir, "ReadingList.db");
            MediaService service = new($"Data Source={dataPath}");
            
            _repl = new Repl(new Commands.MediaCommands(service));
            _surface = new WPFReplSurface(tbInput, tbOutput, Dispatcher);
            _surface.Bind(_repl);

            /*
            _repl.ReqWrite += msg => Dispatcher.Invoke(() => tbOutput.AppendText(msg));
            _repl.ReqWriteLine += msg => Dispatcher.Invoke(() => tbOutput.AppendText(Environment.NewLine + msg));
            _repl.ReqInputAsync = async (prompt, ct) =>
            {
                TaskCompletionSource<string> tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
                await Dispatcher.InvokeAsync(() =>
                {
                    tbOutput.AppendText(Environment.NewLine + prompt);
                    tbOutput.ScrollToEnd();
                    tbInput.Focus();

                    _inputRequested = true;
                    _pendingInput = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
                    tcs = _pendingInput;
                });

                using (ct.Register(() => tcs.TrySetCanceled(ct)))
                {
                    try { return await tcs.Task; }
                    finally
                    {
                        await Dispatcher.InvokeAsync(() =>
                        {
                            _inputRequested = false;
                            _pendingInput = null;
                        });
                    }
                }
            };*/
        }

        private async Task SubmitAsync()
        {
            string input = tbInput.Text;
            tbInput.Clear();

            if (_surface.TrySubmitInput(input)) return;            
            if (string.IsNullOrWhiteSpace(input)) return;

            if (input.Equals("clear", StringComparison.OrdinalIgnoreCase))
            {
                tbOutput.Clear();
                return;
            }

            _cts?.Dispose();
            _cts = new CancellationTokenSource();
            
            try { await _surface.ExecuteAsync(_repl, input, _cts.Token); }
            catch (OperationCanceledException) { tbOutput.AppendText(Environment.NewLine + "[Cancelled]"); }
            finally
            {
                _cts.Dispose();
                _cts = null;
            }
        }

        private async void btEnter_Click(object sender, RoutedEventArgs e)
        {
            await SubmitAsync();
        }

        private void InputInsert(string str)
        {
            int ci = tbInput.CaretIndex;
            tbInput.Text = tbInput.Text.Insert(ci, str);
            tbInput.CaretIndex = ci + str.Length;
        }

        private void InputBackSpace()
        {
            int ci = tbInput.CaretIndex;
            tbInput.Text = tbInput.Text.Remove(ci - 1, 1);
            tbInput.CaretIndex = ci - 1;
        }

        private void InputInsertPair(string open, string close)
        {
            int start = tbInput.SelectionStart;
            int length = tbInput.SelectionLength;
            string selected = tbInput.SelectedText;

            if (!string.IsNullOrEmpty(selected))
            {
                tbInput.Text = tbInput.Text.Remove(start, length)
                    .Insert(start, open + selected + close);
                tbInput.SelectionStart = start + open.Length + selected.Length + close.Length;
            }
            else
            {
                tbInput.Text = tbInput.Text.Insert(start, open + close);
                tbInput.SelectionStart = start + open.Length;
            }
        }

        private bool InputCheckNext(string str)
        {
            if (tbInput.CaretIndex > tbInput.Text.Length) return false;
            int len = Math.Clamp(str.Length, 0, tbInput.Text.Length - tbInput.CaretIndex);
            if (tbInput.Text.Substring(tbInput.CaretIndex, len).Equals(str, StringComparison.OrdinalIgnoreCase)) return true;
            else return false;
        }

        private async void tbInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                _cts?.Cancel();
                if (_inputRequested) _pendingInput?.TrySetCanceled();
            }

            if (e.Key == Key.Enter)
            {
                if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control) || Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
                {
                    InputInsert(Environment.NewLine + new string('\t', _tabDepth));
                }
                else
                {
                    tbOutput.ScrollToEnd();
                    await SubmitAsync();
                    e.Handled = true;
                }
            }
            if (e.Key == Key.Tab)
            {
                if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
                {
                    InputBackSpace();
                    _tabDepth--;
                    e.Handled = true;
                }
                else
                {
                    InputInsert("\t");
                    _tabDepth++;
                    e.Handled = true;
                }                
            }
        }

        private void tbInput_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (e.Text is "\"" or "}" or "]" or ")")
            {
                if (InputCheckNext(e.Text))
                {
                    tbInput.CaretIndex += e.Text.Length;
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
                InputInsertPair(e.Text, close);
                e.Handled = true;
            }
        }
    }
}