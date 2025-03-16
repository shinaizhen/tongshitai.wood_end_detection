using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EndFaceDetection.Common.Converters;
using EndFaceDetection.Shell.Views;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace EndFaceDetection.Shell.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        public MainViewModel() 
        {
            //string path = @"Resources/Images/Image231206094252.jpg";
            //var orign_image = ConvertImageType.LoadImage(path);
            //var byteArray = ConvertImageType.ConvertSixLaborsImage2ByteArray(orign_image);

            //using (var memoryStream = new MemoryStream(byteArray))
            //{
            //    var bitmapImage = new BitmapImage();
            //    bitmapImage.BeginInit();
            //    bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
            //    bitmapImage.StreamSource = memoryStream;
            //    bitmapImage.EndInit();
            //    OrignImage = bitmapImage;
            //}
            //string path1 = @"Resources/Images/Image231206094252.jpg";
            //var res_image = ConvertImageType.LoadImage(path1);
            //var byteArray2 = ConvertImageType.ConvertSixLaborsImage2ByteArray(res_image);

            //using (var memoryStream = new MemoryStream(byteArray2))
            //{
            //    var bitmapImage = new BitmapImage();
            //    bitmapImage.BeginInit();
            //    bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
            //    bitmapImage.StreamSource = memoryStream;
            //    bitmapImage.EndInit();
            //    ResultImage = bitmapImage;
            //}
        }

        [ObservableProperty]
        private BitmapImage orignImage;

        [ObservableProperty]
        private BitmapImage resultImage;

        [RelayCommand]
        private void CloseMainView(object main)//关闭主窗体
        {
            var view = (MainWindow)main;
            view.Close();
        }

        [RelayCommand]
        private void MiniMainView(object main) 
        {
            var view = (MainWindow)main;
            view.WindowState=System.Windows.WindowState.Minimized;
        }

        [RelayCommand]
        private void ShowTestView()
        {
            TestView testView = new TestView();
            testView.Show();
        }
    }
}
