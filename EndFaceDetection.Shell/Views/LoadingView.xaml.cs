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
using System.Windows.Threading;

namespace EndFaceDetection.Shell.Views
{
    /// <summary>
    /// LoadingView.xaml 的交互逻辑
    /// </summary>
    public partial class LoadingView : Window
    {
        DispatcherTimer timer;
        public LoadingView()
        {
            InitializeComponent();
            timer = new DispatcherTimer();
            timer.Interval =TimeSpan.FromMilliseconds(100);
            timer.Tick += ChangBackground;
            timer.Start();
        }
        int indx = 0;
        private void ChangBackground(object? sender, EventArgs e)
        {
            if (indx%2 == 0)
            {
                this.back.Background = Brushes.Red;
            }else
            {
                this.back.Background = Brushes.Green;
            }
            indx++;
        }

        public void ShowMsg(string msg)
        {
            this.Dispatcher.Invoke(new Action(() =>
            {
                this.txtblock_Loading.Text = msg;
            }));
        }

        private void Window_Unloaded(object sender, RoutedEventArgs e)
        {
            timer.Stop();
        }
    }
}
