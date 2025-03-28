using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Vic3FlagDesigner
{
    /// <summary>
    /// Interaction logic for LoadingWindow.xaml
    /// </summary>
    public partial class LoadingWindow : Window
    {
        private CancellationTokenSource _cancellationTokenSource;

        public LoadingWindow()
        {
            InitializeComponent();
        }

        public void SetCancellationTokenSource(CancellationTokenSource cancellationTokenSource)
        {
            _cancellationTokenSource = cancellationTokenSource;
        }

        public void UpdateProgress(int processed, int total)
        {
            Dispatcher.Invoke(() =>
            {
                ProgressBar.Value = (double)processed / total * 100;
                ProgressText.Text = $"Loading {processed}/{total} images...";
            });
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            _cancellationTokenSource?.Cancel();
            Close();
        }
    }
}

