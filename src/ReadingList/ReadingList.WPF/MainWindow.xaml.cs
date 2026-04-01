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
            
            // Input, Output & History handled by _surface:
            _surface = new WPFReplSurface(tbInput, tbOutput, Dispatcher, new Commands.MediaCommands(service));
        }

        private async Task SubmitAsync()
        {
            // Receive input:
            string input = tbInput.Text;
            tbInput.Clear();

            // If Repl is awaiting prompt, send, otherwise, continue to a new command.
            if (_surface.TrySubmitInput(input)) return;            
            if (string.IsNullOrWhiteSpace(input)) return;
            _surface.AddToHistory(input);

            // Declare new CancellationTokenSource:
            _cts?.Dispose();
            _cts = new CancellationTokenSource();
            
            // Execite, watching for cancellation, then dispose of CancellationTokenSource:
            try { await _surface.ExecuteAsync(input, _cts.Token); }
            catch (OperationCanceledException) { tbOutput.AppendText(Environment.NewLine + "[Cancelled]"); }
            finally
            {
                _cts.Dispose();
                _cts = null;
            }
        }

        // Input handlers using CCRepl.WPF.InputHelpers:
        private async void btEnter_Click(object sender, RoutedEventArgs e) => await SubmitAsync();
               
        private async void tbInput_KeyDown(object sender, KeyEventArgs e)
        {
            switch (WPFHandleKeyDown(sender, e, ref _tabDepth))
            {
                case KeyAction.Cancel:
                    _cts?.Cancel();
                    _surface.Cancel();
                    break;
                case KeyAction.Submit:
                    await SubmitAsync();
                    break;
            }
        }

        private void tbInput_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Up) _surface.HistoryUp();
            if (e.Key == Key.Down) _surface.HistoryDown();
        }

        private void tbInput_PreviewTextInput(object sender, TextCompositionEventArgs e) => WPFHandlePreviewTextInput(sender, e);
    }
}