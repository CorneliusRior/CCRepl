using CCRepl;
using CCRepl.WPF;
using static CCRepl.WPF.InputHelpers;
using ReadingList.Services;
using System.IO;
using System.Windows;
using System.Windows.Input;



namespace ReadingList.WPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly WPFReplSurface _surface;
        private CancellationTokenSource? _cts;

        private int _tabDepth = 0;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;

            string dataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CCRepl_ReadingList");
            Directory.CreateDirectory(dataDir);
            string dataPath = Path.Combine(dataDir, "ReadingList.db");
            MediaService service = new($"Data Source={dataPath}");
            
            _surface = new WPFReplSurface(tbInput, tbOutput, Dispatcher, new Commands.MediaCommands(service));
        }

        private async Task SubmitAsync()
        {
            string input = tbInput.Text;
            tbInput.Clear();

            if (_surface.TrySubmitInput(input)) return;            
            if (string.IsNullOrWhiteSpace(input)) return;
            _surface.AddToHistory(input);

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

       
        private async void tbInput_KeyDown(object sender, KeyEventArgs e)
        {
            /*
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
            }*/
            switch (WPFHandleKeyDown(sender, e, ref _tabDepth))
            {
                case KeyAction.Cancel:
                    _cts?.Cancel();
                    _surface.Cancel();
                    break;
                case KeyAction.Submit:
                    await SubmitAsync();
                    break;
                case KeyAction.HistoryUp:
                    _surface.HistoryUp();
                    break;
                case KeyAction.HistoryDown:
                    _surface.HistoryDown();
                    break;
            }
        }

        private void tbInput_PreviewTextInput(object sender, TextCompositionEventArgs e) => WPFHandlePreviewTextInput(sender, e);

        private void tbInput_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Up) _surface.HistoryUp();
            if (e.Key == Key.Down) _surface.HistoryDown();
        }        
    }
}