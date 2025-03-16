using EndFaceDetection.Shell.Views;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace EndFaceDetection.Shell
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // 获取工作区的大小
            var workArea = SystemParameters.WorkArea;

            // 设置窗口大小和位置
            this.Left = workArea.Left;
            this.Top = workArea.Top;
            this.Width = workArea.Width;
            this.Height = workArea.Height;

            // 使窗口最大化
            this.WindowStyle = WindowStyle.None;
            this.WindowState = WindowState.Normal;
            this.ResizeMode = ResizeMode.NoResize;
            this.Dispatcher.BeginInvoke(new Action(() => 
            {
                App app = (App)Application.Current;
                LoadingView loadingView = new LoadingView();
                loadingView.Owner = this;
                Task.Run(() =>
                {
                    
                    app.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        loadingView.Show();
                    }));
                    app.PLCService.Init();
                    app.CameraService.InitCamera();
                    Task.Factory.StartNew(() =>
                    {
                        string bat_path = @"C:\Users\dlpu\Desktop\ultralytics-main\run_python.bat";
                        app.DetectionService.Init(bat_path);
                    });
                    app.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        loadingView.Close();
                        this.radiobtn_DetectionView.IsChecked = true;
                    }));
                    
                });
            }));
        }

        private void Window_Unloaded(object sender, RoutedEventArgs e)
        {
            
        }

        PLCView? PLCView;
        DetectionView? DetectionView;

        private void RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            if(PLCView == null) 
                PLCView = new PLCView();
            if(DetectionView == null)
                DetectionView = new DetectionView();
            if (radiobtn_DetectionView.IsChecked == true)
            {
                this.view_content.Content = DetectionView;
            }
            else if (radiobtn_PLCMonitor.IsChecked == true)
            {
                this.view_content.Content = PLCView;
            }
            else if (radiobtn_WoodInfoView.IsChecked == true) 
            {
                this.view_content.Content = new WoodInfoConfigView();
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            App app = (App)Application.Current;
            app.PLCService.Uninit();
            app.CameraService.CloseAll();
            app.DetectionService.UnInit();
        }
    }
}