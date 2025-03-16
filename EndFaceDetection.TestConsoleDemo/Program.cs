
using MiniExcelLibs;
using SkiaSharp;
using System.IO;

namespace EndFaceDetection.TestConsoleDemo
{
    internal class Program
    {
        static void Main(string[] args)
        {
            //List<WoodInfo> list = new List<WoodInfo>();
            //string path = @"C:\Users\Administrator\Desktop\tongshitai\WpfApp.TongShiTaiV2\通世泰木材型号.xlsx";
            //var rows = MiniExcel.Query<WoodInfoV1>(path);
            //foreach (var row in rows) 
            //{
            //    list.Add(row.Copy());
            //    Console.WriteLine('{');
            //    Console.WriteLine($"    二维码：{row.ORCode}");
            //    Console.WriteLine($"    相机名称：{row.ORCode}");
            //    Console.WriteLine('}');
            //}
            //JsonHelper.WriteLargeJsonFile("wood_info.json",list);

            LoggerHelper._.Warn("zheiasdfiosa ");

            //try
            //{
            //    string path = "D:\\通世泰项目文件\\木板端面瑕疵检测\\wood01\\images\\2023-09-18-084036.jpg";
            //    // 假设从文件加载图像
            //    using var originalImage = SKBitmap.Decode(path);

            //    // 创建一个新的 SKCanvas，将原始图像作为目标
            //    using var surface = SKSurface.Create(new SKImageInfo(originalImage.Width, originalImage.Height), originalImage.GetPixels(), originalImage.RowBytes);
            //    var canvas = surface.Canvas;

            //    // 设置矩形的参数
            //    int x = 500;
            //    int y = 500;
            //    int width = 200;
            //    int height = 150;

            //    DrawRectangleOnImage(originalImage, x, y, width, height);

            //    // 保存修改后的图像
            //    using var image = surface.Snapshot();
            //    using var data = image.Encode(SKEncodedImageFormat.Jpeg, 100);
            //    using var stream = new FileStream("output_image.jpg", FileMode.Create);
            //    data.SaveTo(stream);
            //}
            //catch (Exception ex) 
            //{
            //    LoggerHelper._.Error("",ex);
            //}

            Console.ReadKey();   
        }

        static void DrawRectangleOnImage(SKBitmap originalImage, int x, int y, int width, int height)
        {
            using (var canvas = new SKCanvas(originalImage))
            {
                using (var paint = new SKPaint())
                {
                    paint.Style = SKPaintStyle.Stroke;
                    paint.Color = SKColors.Red;
                    paint.StrokeWidth = 10;
                    canvas.DrawRect(x, y, x + width, y + height, paint);

                    // 绘制文本框
                    string textToDraw = "kong_none";
                    int txt_x = x;
                    int txt_y = y-10;
                    var textPaint = new SKPaint
                    {
                        TextSize = 150,
                        Color = SKColors.Red,
                        IsAntialias = true
                    };
                    canvas.DrawText(textToDraw, txt_x, txt_y, textPaint);
                }
            }

            //return originalImage;
        }
    }
}
