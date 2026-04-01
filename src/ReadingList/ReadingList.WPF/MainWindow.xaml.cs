using CCRepl;
using CCRepl.WPF;
using static CCRepl.WPF.InputHelpers;
using ReadingList.Commands;
using ReadingList.Services;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;



namespace ReadingList.WPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : ReplWindowBase
    {
        protected override TextBox Input => tbInput;
        protected override TextBox Output => tbOutput;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;

            string dataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CCRepl_ReadingList");
            Directory.CreateDirectory(dataDir);
            string dataPath = Path.Combine(dataDir, "ReadingList.db");
            MediaService service = new($"Data Source={dataPath}");
            
            Initialize(new WPFReplSurface(tbInput, tbOutput, Dispatcher, new ReadingListCommands(service)));
        }
    }
}