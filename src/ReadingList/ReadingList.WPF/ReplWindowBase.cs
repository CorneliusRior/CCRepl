using CCRepl;
using CCRepl.WPF;
using static CCRepl.WPF.InputHelpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace ReadingList.WPF
{
    public abstract class ReplWindowBase : Window
    {
        protected WPFReplSurface _surface = null!;
        private CancellationTokenSource? _cts;
        private int _tabDepth = 0;
        protected abstract TextBox Input { get; }
        protected abstract TextBox Output { get; }

        public ReplWindowBase(WPFReplSurface surface)
        {
            _surface = surface;
        }

        public virtual async Task SubmitAsync()
        {
            string input = Input.Text;
            Input.Clear();
            if (_surface.TrySubmitInput(input)) return;
            if (string.IsNullOrWhiteSpace(input)) return;
            _surface.AddToHistory(input);

            _cts?.Dispose();
            _cts = new CancellationTokenSource();

            try { await _surface.ExecuteAsync(input, _cts.Token); }
            catch (OperationCanceledException) { Output.AppendText(Environment.NewLine + "[Cancelled]"); }
            finally
            {
                _cts.Dispose();
                _cts = null;
            }
        }

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
