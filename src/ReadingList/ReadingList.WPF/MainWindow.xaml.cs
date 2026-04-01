using CCRepl;
using CCRepl.WPF;
using static CCRepl.WPF.InputHelpers;
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
        //private readonly Repl _repl;
        private readonly WPFReplSurface _surface;
        private CancellationTokenSource? _cts;

        private List<string> _history;
        bool _browsingHistory = false;
        private int _historyIndex;
        private string _currentDraft = "";

        private int _tabDepth = 0;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;

            string dataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CCRepl_ReadingList");
            Directory.CreateDirectory(dataDir);
            string dataPath = Path.Combine(dataDir, "ReadingList.db");
            MediaService service = new($"Data Source={dataPath}");
            
            //_repl = new Repl(new Commands.MediaCommands(service));
            _surface = new WPFReplSurface(tbInput, tbOutput, Dispatcher, new Commands.MediaCommands(service));

            _history = new();
        }

        private async Task SubmitAsync()
        {
            string input = tbInput.Text;
            AddToHistory(input);
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
            
            try { await _surface.ExecuteAsync(input, _cts.Token); }
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

        private void AddToHistory(string str)
        {
            if (_history.Count == 0 || !string.Equals(_history[^1], str, StringComparison.Ordinal)) _history.Add(str);
        }

        private void LoadHistory(int index)
        {
            tbInput.Clear();
            tbInput.AppendText(_history[index]);
            tbInput.CaretIndex = tbInput.Text.Length;
        }

        private void HistoryUp()
        {
            if (_history.Count == 0) return;
            if (!_browsingHistory)
            {
                _currentDraft = tbInput.Text;
                _browsingHistory = true;
                _historyIndex = _history.Count;
            }
            if (_historyIndex > 0) _historyIndex--;
            LoadHistory(_historyIndex);
        }

        private void HistoryDown()
        {
            if (!_browsingHistory) return;
            if (_historyIndex < _history.Count -1)
            {
                _historyIndex++;
                LoadHistory(_historyIndex);
                return;
            }

            _historyIndex = _history.Count;
            tbInput.Clear();
            tbInput.AppendText(_currentDraft);
            _browsingHistory = false;
        }

        private async void tbInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                _cts?.Cancel();
                _surface.Cancel();
            }

            if (e.Key == Key.Enter)
            {
                if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control) || Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
                {
                    tbInput.Insert(Environment.NewLine + new string('\t', _tabDepth));
                }
                else
                {
                    e.Handled = true;
                    await SubmitAsync();
                }
            }
            if (e.Key == Key.Tab)
            {
                if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
                {
                    if (tbInput.RemoveTab())
                    {
                        _tabDepth--;
                        _tabDepth = Math.Max(_tabDepth, 0);
                    }
                    e.Handled = true;
                }
                else
                {
                    tbInput.Insert("\t");
                    _tabDepth++;
                    e.Handled = true;
                }                
            }
        }

        private void tbInput_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (e.Text is "\"" or "}" or "]" or ")")
            {
                if (tbInput.CheckNext(e.Text))
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
                tbInput.InsertPair(e.Text, close);
                //InputInsertPair(e.Text, close);
                e.Handled = true;
            }
        }

        private void tbInput_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Up) HistoryUp();
            if (e.Key == Key.Down) HistoryDown();
        }
    }
}