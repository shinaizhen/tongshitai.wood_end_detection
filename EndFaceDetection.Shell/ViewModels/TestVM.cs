using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EndFaceDetection.Services;
using System.Windows;
using EndFaceDetection.Common;
using EndFaceDetection.Common.Converters;
using System.Windows.Media.Imaging;
using System.IO;
using EndFaceDetection.Services.Common;
using SkiaSharp;
using EndFaceDetection.Shell.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using ImageInfo = EndFaceDetection.Services.ImageInfo;
using System.Windows.Shapes;
using EndFaceDetection.LogModule;
using EndFaceDetection.Shell.Views;

namespace EndFaceDetection.Shell.ViewModels
{
    public partial class TestVM:ObservableObject
    {
        [ObservableProperty]
        private BitmapImage bitmapImage;

        public TestVM() { }

        [RelayCommand]
        private void GetText()
        {
            DetectionService detectionService = new DetectionService();
            var list = detectionService.GetText();
            string text = "";
            if (list != null)
            {
                foreach (var item in list)
                {
                    text += item.ToString();
                }
                
            }
            MessageBox.Show(text);
        }

        [RelayCommand]
        private void RunPython()
        {
            App app = (App)Application.Current;
            DetectionService detectionService = app.DetectionService;
            string bat_path = @"C:\Users\dlpu\Desktop\ultralytics-main\run_python.bat";
            int nRet=0;
            Task.Factory.StartNew(() =>
            {

                nRet = detectionService.RunByBAT(bat_path);
                app.Dispatcher.BeginInvoke(() =>
                {
                    MessageBox.Show(nRet.ToString());
                });

            });
            
        }

        [RelayCommand]
        private void TestImageConverter()
        {
            string path = @"Resources/Images/Image231206094252.jpg";
            var image = ConvertImageType.LoadImage(path);
            MessageBox.Show("读取图片");
            var byteArray = ConvertImageType.ConvertSixLaborsImage2ByteArray(image);
            MessageBox.Show("转换为字节数组成功");

            var image2 = ConvertImageType.ConvertSixLaborsImage2ByteArray(image);
            LoggerHelper._.Debug("转换为图片成功");

            using (var memoryStream = new MemoryStream(byteArray))
            {
                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.StreamSource = memoryStream;
                bitmapImage.EndInit();
                BitmapImage = bitmapImage;
            }
        }

        [RelayCommand]
        public void SkBitmapDemo()
        {
            string path = "D:\\TongShiTai\\2024-09-06\\06K\\2024-09-06-044148.jpg";
            // 假设从文件加载图像
            using var originalImage = SKBitmap.Decode(path);
            BitmapImage = SKBitmapExtensions.ToBitmapSource(originalImage);
        }

        [ObservableProperty]
        private BitmapSource bitmapSource;

        [RelayCommand]
        private void StopPython()
        {
            App app = (App)Application.Current;
            DetectionService detectionService = app.DetectionService;
            detectionService.UnInit();
        }

        [RelayCommand]
        public void DectionDemo()
        {
            Task.Run(() =>
            {
                string img_path = "C:\\Users\\dlpu\\Desktop\\通世泰端面瑕疵检测系统\\EndFaceDetection.Shell\\Resources\\Images\\Image231130150536.jpg";
                string or = "ORCode";
                string dateTimeStr = DateTime.Now.ToString("yyyy-MM-dd") + "\\";
                string save_img_dir = ConfigModel.SAVE_IMGS_DIR + dateTimeStr + or + "\\";
                if(Directory.Exists(save_img_dir)==false)
                    Directory.CreateDirectory(save_img_dir);
                string save_img_path= save_img_dir + DateTime.Now.ToString("yyyy-MM-dd-hhmmss") + ".jpg";
                string save_json_path = ConfigModel.SAVE_IMGS_DIR + dateTimeStr + "detection_result.json";
                App app = (App)Application.Current;
                DetectionService detectionService = app.DetectionService;
                var res = detectionService.StartDetecting(Image.Load<L8>(img_path), "C16", save_img_path, save_json_path);
                if(res==null)
                    return;
                var kongs = res.Labels.Where((k) => k.Id == 0 || k.Id == 1).ToList();
                var other_labels = res.Labels.Where((l) => l.Id != 0 && l.Id != 1).ToList();
                SKBitmap bitmap = SKBitmap.Decode(res.ImagePath);

                detectionService.DrawImageKongs(bitmap, kongs);
                detectionService.DrawImageLabels(bitmap, other_labels);
                app.Dispatcher.BeginInvoke(() =>
                {
                    BitmapImage = SKBitmapExtensions.ToBitmapSource(bitmap);
                });
            });
            
        }
        LoadingView loadingView;
        [RelayCommand]
        public void Loading()
        {
            App app = (App)Application.Current;
            loadingView = new LoadingView();
            app.Dispatcher.BeginInvoke(() =>
            {
                loadingView.Show();
            });
        }
        [RelayCommand]
        private void Unload()
        {
            loadingView?.Close();
        }
    }
}
