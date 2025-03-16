using SkiaSharp.Views.Desktop;
using SkiaSharp;
using System.IO;
using System.Windows.Media.Imaging;

namespace EndFaceDetection.Services.Common
{
    public static class SKBitmapExtensions
    {
        public static BitmapImage ToBitmapSource(this SKBitmap skBitmap)
        {
            // 创建一个流来存储位图数据
            using (var stream = new MemoryStream())
            {
                // 将 SKBitmap 转换为 PNG 格式并写入流中
                skBitmap.Encode(stream, SKEncodedImageFormat.Jpeg, 100);
                stream.Seek(0, SeekOrigin.Begin);

                // 创建一个 WPF BitmapImage 对象
                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = stream;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();

                return bitmapImage;
            }
        }
    }
}
