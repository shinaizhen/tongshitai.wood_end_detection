using EndFaceDetection.Common.Converters;
using EndFaceDetection.LogModule;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Data;
using Image = SixLabors.ImageSharp.Image;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;
using System.Windows.Media.Imaging;

namespace EndFaceDetection.Shell.Converters
{
    public class SixLaborsImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                Image image = (Image)value;
                var byteArray = ConvertImageType.ConvertSixLaborsImage2ByteArray(image);
                //MessageBox.Show("转换为图片成功");

                using (var memoryStream = new MemoryStream(byteArray))
                {
                    var bitmapImage = new BitmapImage();
                    bitmapImage.BeginInit();
                    bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                    bitmapImage.StreamSource = memoryStream;
                    bitmapImage.EndInit();
                    return bitmapImage;
                }
            }
            catch (Exception ex)
            {
                LoggerHelper._.Error("图片转化失败！", ex);
                return new BitmapImage();
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                BitmapImage image = (BitmapImage)value;
                using var memoryStream = new MemoryStream();
                var bitmapEncoder = new PngBitmapEncoder();
                bitmapEncoder.Frames.Add(BitmapFrame.Create(image));
                bitmapEncoder.Save(memoryStream);
                memoryStream.Seek(0, SeekOrigin.Begin);

                return Image.Load<L8>(memoryStream);
            }
            catch (Exception ex)
            {
                LoggerHelper._.Error("",ex);
                return null;
            }
        }
    }
}
