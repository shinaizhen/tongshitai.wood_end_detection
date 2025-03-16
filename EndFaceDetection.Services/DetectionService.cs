using EndFaceDetection.Common.Converters;
using EndFaceDetection.LogModule;
using EndFaceDetection.Services.Common;
using SixLabors.ImageSharp;
using SkiaSharp;
using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Text;
using System.Threading;
using Image = SixLabors.ImageSharp.Image;

namespace EndFaceDetection.Services
{
    public class DetectionService
    {
        const long CAPACITY_CMD = 1;
        static MemoryMappedFile statusMMF = MemoryMappedFile.CreateOrOpen("status", CAPACITY_CMD);//通信状态命令
        const long CAPACITY_IMAGE = 5120 * 5120 * 3 + 4;
        static MemoryMappedFile imageMMF = MemoryMappedFile.CreateOrOpen("image", CAPACITY_IMAGE);//用于进程间传递图像数据
        static MemoryMappedFile imgLenMMF = MemoryMappedFile.CreateOrOpen("imagelen", 4);//图像数据的大小

        public static bool RunStatus { get; private set; }=false;
        List<string> classes = new List<string>();

        public DetectionService()
        {
            //classes = GetText();
        }

        /// <summary>
        /// 初始化下次检测服务
        /// </summary>
        /// <param name="bat_path">启用python脚本的bat文件</param>
        /// <returns></returns>
        public bool Init(string bat_path)
        {
            classes.Clear();
            classes = GetText();
            Task.Run(() =>
            {
                int nRet = RunByBAT(bat_path);
                if(nRet != 0)
                {
                    LoggerHelper._.Error($"python程序异常退出，退出码：{nRet}");
                    m_bPythonStatus = false;
                }
            });
            return true;
        }
        
        public void UnInit()
        {
            mmf2cmd("9");
        }

        public bool CheckStatus()
        {
            return  RunStatus;
        }

        bool m_bPythonStatus = true;

        /// <summary>
        /// 运行python脚本
        /// </summary>
        /// <param name="bat_path"></param>
        /// <returns></returns>
        public int RunByBAT(string bat_path)
        {
            try
            {
                using (Process pro = new Process())
                {
                    FileInfo file = new FileInfo(bat_path);
                    pro.StartInfo.WorkingDirectory = file.Directory.FullName;
                    pro.StartInfo.FileName = bat_path;
                    pro.StartInfo.UseShellExecute = true;
                    pro.StartInfo.CreateNoWindow = true;
                    pro.Start();
                    pro.WaitForExit();
                    return pro.ExitCode;
                }
            }
            catch (Exception ex)
            {
                LoggerHelper._.Error($"开始运行python程序时发生错误：{ex.Message}");
                return -1;
            }
        }

        /// <summary>
        /// 获取瑕疵类别信息
        /// </summary>
        /// <returns></returns>
        public List<string> GetText()
        {
            string filePath = "Resources/classes.txt";
            List<string> lines = new List<string>();
            string? line;
            using (StreamReader reader = new StreamReader(filePath))
            {
                line = reader.ReadLine();
                while (line != null)
                {
                    lines.Add(line);
                    line = reader.ReadLine();
                }
            }
            return lines;
        }

        static Mutex mutex=new Mutex();

        #region C#《==》python通讯模块
        /// <summary>
        /// 写入状态命令
        /// </summary>
        /// <param name="cmd"></param>
        private void mmf2cmd(string cmd)
        {
            try
            {
                mutex.WaitOne();
                byte[] bytes = Encoding.ASCII.GetBytes(cmd);
                using (var cmdAccesor = statusMMF.CreateViewAccessor(0, CAPACITY_CMD, MemoryMappedFileAccess.Write))
                {
                    cmdAccesor.WriteArray<byte>(0, bytes, 0, bytes.Length);
                }
            }
            catch (Exception ex)
            {
                LoggerHelper._.Error($"读写共享内存时发生错误：{ex.Message}");
            }finally { mutex.ReleaseMutex(); }
        }

        /// <summary>
        /// 读取状态命令
        /// </summary>
        /// <returns></returns>
        private string mmffromcmd()
        {
            try
            {
                mutex.WaitOne();
                byte[] bytes = new byte[1];
                using (var cmdAccesor = statusMMF.CreateViewAccessor(0, CAPACITY_CMD, MemoryMappedFileAccess.Read))
                {
                    cmdAccesor.ReadArray<byte>(0, bytes, 0, bytes.Length);
                }
                return Encoding.ASCII.GetString(bytes);
            }
            catch (Exception ex)
            {
                LoggerHelper._.Error($"读写共享内存时发生错误：{ex.Message}");
                return null;
            }finally { mutex.ReleaseMutex(); }
        }

        private void mmf2img(byte[] bytes)
        {
            try
            {
                using (var lenAccessor = imgLenMMF.CreateViewAccessor(0, 4, MemoryMappedFileAccess.Write))
                {
                    int len = bytes.Length;
                    lenAccessor.Write(0, len);
                    using (var imgAccessor = imageMMF.CreateViewAccessor(0, len, MemoryMappedFileAccess.Write))
                    {
                        imgAccessor.WriteArray<byte>(0, bytes, 0, len);
                    }
                }
            }
            catch (Exception ex)
            {
                LoggerHelper._.Error($"读写共享内存时发生错误：{ex.Message}");
                return;
            }


        }
        #endregion

        #region 检测模块

        List<DetectionReaultLabel> GetLabels(string path, int[] ids)
        {
            List<DetectionReaultLabel> labels = new List<DetectionReaultLabel>();
            if (!File.Exists(path))
            {
                LoggerHelper._.Error($"{path} does not exist");
                return labels;
            }
            else
            {
                var lines = File.ReadAllLines(path);
                foreach (var line in lines)
                {
                    var res = line.Split(',');
                    var label = new DetectionReaultLabel()
                    {
                        Id = Convert.ToInt32(res[0]),
                        X = (float)Convert.ToDecimal(res[2]),
                        Y = (float)Convert.ToDecimal(res[3]),
                        Score = (float)Convert.ToDecimal(res[1]),
                        Area = (float)Convert.ToDecimal(res[6]),
                        Width = (float)Convert.ToDecimal(res[4]),
                        Height = (float)Convert.ToDecimal(res[5])
                    };
                    label.ClassName = classes[label.Id];
                    if (label.Id == 0 || label.Id == 1)
                    {
                        if (label.Area < 80000)
                        {
                            continue;
                        }
                        var list_kong = labels.Where(x => (x.Id == 0 || x.Id == 1) && Math.Abs(x.X - label.X) < 500).ToList();
                        if (list_kong.Count > 0)
                            continue;
                        if (label.Id == 1)
                            label.Color = SKColors.Blue;

                    }
                    if (ids?.Contains(label.Id) == true)
                        labels.Add(label);
                }
                return labels;
            }
        }

        public DetectionReaultInfo StartDetecting(Image img,string cameraName,string imgPath,string labels_path)
        {
            if(img == null)//图像为空值
                throw new ArgumentNullException(nameof(img));
            img.SaveAsJpeg(imgPath);
            DetectionReaultInfo detectionReaultInfo = new DetectionReaultInfo() { ImagePath = imgPath, CameraName = cameraName };
            Stopwatch sw = Stopwatch.StartNew();
            sw.Start();
            byte[] datas = ConvertImageType.ConvertSixLaborsImage2ByteArray(img);
            mmf2img(datas);
            mmf2cmd("1");
            while (true)
            {
                Thread.Sleep(10);
                var cmd = mmffromcmd();
                if (cmd == "2")
                    break;
            }

            string path = @"D:\Temp\res01.txt";
            // 获取标签
            var labels = GetLabels(path, [0, 1, 2, 3, 4, 5, 6]);
            detectionReaultInfo.Labels.AddRange(labels);
            if (cameraName == "Camera")
            {
                if (labels.Count > 0)
                    detectionReaultInfo.IsOK = false;
            }
            else
            {
                var kongs = labels.Where((l) => l.Id == 1).ToList();
                if (kongs.Count != 2)
                {
                    detectionReaultInfo.IsOK = false;
                }
            }
            JsonHelper.GrowJsonFile(labels_path, detectionReaultInfo);
            return detectionReaultInfo;
        }

        public void DrawImageKongs(SKBitmap originalImage, List<DetectionReaultLabel> kongs)
        {
            if (originalImage == null) 
                throw new ArgumentNullException(nameof(originalImage));
            // 创建一个新的 SKCanvas，将原始图像作为目标
            int img_width = originalImage.Width;
            int img_height = originalImage.Height;
            using var surface = SKSurface.Create(new SKImageInfo(img_width, img_height), originalImage.GetPixels(), originalImage.RowBytes);
            var canvas = surface.Canvas;

            // 第一个矩形
            SKColor color1 = SKColors.Red;
            int x1 = 0;
            int y1 = 0;
            int width1 = 0;
            int height1 = 0;
            //第一个标签
            int txt_x1 = 0;
            int txt_y1 = 0;
            string txt1 = "";

            // 第二个矩形
            SKColor color2 = SKColors.Red;
            int x2 = 0;
            int y2 = 0;
            int width2 = 0;
            int height2 = 0;
            //第二个标签
            int txt_x2 = 0;
            int txt_y2 = 0;
            string txt2 = "";
            if (kongs.Count>0)
            {
                if (kongs.Count == 1)//缺少一个
                {
                    // 第一个矩形
                    var kong = kongs[0];
                    x1 = (int)kong.X;
                    y1 = (int)kong.Y;
                    width1 = (int)kong.Width;
                    height1 = (int)kong.Height;
                    color1 = kong.Color;

                    txt_x1 = (int)x1;
                    txt_y1 = (int)(y1 - 120);
                    txt1 = kong.Id.ToString() ?? "Unkwon";

                    // 第二个矩形
                    x2 = (int)(img_width - kong.X);
                    y2 = (int)kong.Y;
                    width2 = (int)width1;
                    height2 = (int)width1;
                    color2 = kong.Color;

                    txt_x2 = (int)x2;
                    txt_y2 = (int)(y2 - 120);
                    txt2 = "03";

                }
                else
                {
                    foreach (var kong in kongs)
                    {
                        // 绘制矩形
                        using var rectpaint = new SKPaint
                        {
                            Style = SKPaintStyle.Stroke,
                            Color = kong.Color,
                            StrokeWidth = 10
                        };
                        using var textPaint = new SKPaint
                        {
                            TextSize = 150,
                            Color = kong.Color,
                            IsAntialias = true
                        };

                        // 设置矩形的参数
                        int x = (int)kong.X;
                        int y = (int)kong.Y;
                        int width = (int)kong.Width;
                        int height = (int)kong.Height;

                        // 绘制文本框
                        string textToDraw = "03";
                        int txt_x = (int)(kong.X) - 20;
                        int txt_y = (int)(kong.Y - 120);
                        canvas.DrawRect(x, y, width, height, rectpaint);
                        canvas.DrawText(textToDraw, txt_x, txt_y, textPaint);
                    }
                    return;
                }
            }
            else
            {
                // 第一个矩形
                x1 = (int)img_width / 3;
                y1 = (int)(img_height / 3 - 300);
                width1 = 600;
                height1 = 600;
                color1 = SKColors.Red;

                txt_x1 = (int)x1;
                txt_y1 = (int)(y1 - 120);
                txt1 = "03";

                // 第二个矩形
                x2 = (int)(img_width - x1);
                y2 = (int)y1;
                width2 = (int)width1;
                height2 = (int)width1;
                color2 = color1;
            }

            using var rectpaint1 = new SKPaint
            {
                Style = SKPaintStyle.Stroke,
                Color = color1,
                StrokeWidth = 10
            };
            using var textPaint1 = new SKPaint
            {
                TextSize = 150,
                Color = color1,
                IsAntialias = true
            };
            canvas.DrawRect(x1, y1, width1, height1, rectpaint1);
            canvas.DrawText(txt1, txt_x1, txt_y2, textPaint1);

            using var rectpaint2 = new SKPaint
            {
                Style = SKPaintStyle.Stroke,
                Color = color2,
                StrokeWidth = 10
            };
            using var textPaint2 = new SKPaint
            {
                TextSize = 150,
                Color = color2,
                IsAntialias = true
            };
            canvas.DrawRect(x2, y2, width2, height2, rectpaint2);
            canvas.DrawText(txt2, txt_x2, txt_y2, textPaint2);
        }

        public void DrawImageLabels(SKBitmap originalImage, List<DetectionReaultLabel> labels)
        {
            try
            {
                foreach (DetectionReaultLabel label in labels)
                {
                    // 创建一个新的 SKCanvas，将原始图像作为目标
                    using var surface = SKSurface.Create(new SKImageInfo(originalImage.Width, originalImage.Height), originalImage.GetPixels(), originalImage.RowBytes);
                    var canvas = surface.Canvas;
                    // 绘制矩形
                    using var rectpaint = new SKPaint
                    {
                        Style = SKPaintStyle.Stroke,
                        Color = SKColors.Red,
                        StrokeWidth = 10
                    };
                    using var textPaint = new SKPaint
                    {
                        TextSize = 150,
                        Color = SKColors.Red,
                        IsAntialias = true
                    };
                    // 设置矩形的参数
                    int x = (int)(label.X);
                    int y = (int)label.Y;
                    int width = (int)label.Width;
                    int height = (int)label.Height;
                    canvas.DrawRect(x, y, width, height, rectpaint);

                    // 绘制文本框
                    string textToDraw = label.Id.ToString()?? "Unknown";
                    int txt_x = (int)(label.X);
                    int txt_y = (int)(label.Y - 120);

                    canvas.DrawText(textToDraw, txt_x, txt_y, textPaint);
                   
                }

            }
            catch (Exception ex)
            {
                LoggerHelper._.Error("", ex);
            }

        }
        #endregion
    }

    /// <summary>
    /// 检测到错误的类型标签
    /// </summary>
    public class DetectionReaultLabel
    {
        public int Id { get; set; }
        public string? ClassName { get; set; }

        public float Width { get; set; }
        public float Height { get; set; }

        public float X { get; set; }
        public float Y { get; set; }

        public float Area { get; set; }

        public float Score { get; set; }

        public SKColor Color { get; set; } = SKColors.Red;

    }

    /// <summary>
    /// 检测结果信息
    /// </summary>
    public class DetectionReaultInfo
    {
        public string ImagePath { get; set; } = "";
        public string CameraName { get; set; } = "";


        /// <summary>
        /// 检测到的标签
        /// </summary>
        public List<DetectionReaultLabel> Labels { get; set; }= new List<DetectionReaultLabel>();

        public bool? IsOK=true;
    }
}
