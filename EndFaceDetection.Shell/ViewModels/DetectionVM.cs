using CommunityToolkit.Mvvm.ComponentModel;
using EndFaceDetection.Services;
using EndFaceDetection.Shell.Models;
using Image = SixLabors.ImageSharp.Image;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using EndFaceDetection.Common.Converters;
using System.Collections;
using System.IO;
using EndFaceDetection.LogModule;
using SkiaSharp;
using EndFaceDetection.Services.Common;
using CommunityToolkit.Mvvm.Input;
using EndFaceDetection.Shell.Views;

namespace EndFaceDetection.Shell.ViewModels
{
    public partial class DetectionVM:ObservableObject
    {
        
        // 服务器
        CameraService m_cameraService {  get; set; }
        PLCService m_PLCService {  get; set; }
        DetectionService m_DetectionService { get; set; }

        // 存储检测结果
        List<DetectionReaultInfo> cameraResultInfo= new List<DetectionReaultInfo>();
        List<DetectionReaultInfo> CameraResultInfo
        {
            get
            {
                return cameraResultInfo;
            }
            set
            {
                cameraResultInfo = value;
                ImageCount= CameraResultInfo.Count()+ C16C25ResultInfo.Count();
            }
        }
        List<DetectionReaultInfo> c16C25ResultInfo = new List<DetectionReaultInfo>();
        List<DetectionReaultInfo> C16C25ResultInfo
        {
            get
            {
                return c16C25ResultInfo;
            }
            set
            {
                c16C25ResultInfo= value;
                ImageCount = CameraResultInfo.Count() + C16C25ResultInfo.Count();
            }
        }

        // 图像缓存区域 count<3
        List<Image> m_CameraImages = new List<Image>();
        List<Image> m_C16C25Images = new List<Image>();
        // 存储木板型号等信息
        WoodInfo m_WoodInfo { get; set; } = new WoodInfo();
        // 只允许一个线程进入检测程序
        static SemaphoreSlim slim = new SemaphoreSlim(1);

        public DetectionVM() 
        {
            var app = (App)Application.Current;
            m_cameraService = app.CameraService;
            m_PLCService = app.PLCService;
            m_PLCService.PopImageAction += new Action(PopImage);
            m_cameraService.StateChanged += new Action<string, bool>(UpdatedStatus);
            m_cameraService.CBImage += new Action<Services.ImageInfo>(CBImage);
            app.PLCService.GrabbedAction += new Action<WoodInfo>(SetWoodInfo);
            m_DetectionService = app.DetectionService;
            Task.Run(() =>
            {
                m_cameraService.InitCamera();
            });
        }
        ~DetectionVM()
        {
            m_cameraService.CloseAll();
        }

        

        #region property
        //相机Camera显示图像
        [ObservableProperty]
        private BitmapImage? imageCamera;
        //相机C16C25显示图像
        [ObservableProperty]
        private BitmapImage? imageC16C25;

        //瑕疵 类型
        [ObservableProperty]
        private ObservableCollection<DetectionModel> detectionModels = new ObservableCollection<DetectionModel>()
        {
            new DetectionModel() {Key="00:空洞缺陷",IsOK=true},
            new DetectionModel() {Key="03:空洞有无",IsOK=true},
            new DetectionModel() {Key="04:开胶检测",IsOK=true},
            new DetectionModel() {Key="05:PB缝隙",IsOK=true},
            new DetectionModel() {Key="06:翘边检测",IsOK=true},
            new DetectionModel() {Key="07:缺肉检测",IsOK=true},
        };
        //检测 结果
        [ObservableProperty]
        private DetectionModel okModel = new DetectionModel { Key = "OK", IsOK = true };
        //Camera 连接状态
        [ObservableProperty]
        private bool cameraConnected = false;
        // c16 connected status
        [ObservableProperty]
        private bool c1Connected = false;
        // cameraname=C25 connected status
        [ObservableProperty]
        private bool c2Connected = false;

        [ObservableProperty]
        private int imageCount=0;

        #endregion

        #region command
        [RelayCommand]
        private void InitCamera()
        {
            m_cameraService.InitCamera();
        }

        [RelayCommand]
        private void PopImage()
        {
            try
            {
                InitDetectionResult();
                LoadingView loadingView = new LoadingView();
                loadingView.ShowMsg("弹出图像。。。");

                loadingView.Owner = App.Current.MainWindow;
                Task.Run(() =>
                {
                    App.Current.Dispatcher.BeginInvoke(() =>
                    {
                        loadingView.Show();
                    });
                    //Thread.Sleep(1000);
                    if (CameraResultInfo.Count > 0)
                    {
                        // 弹出结果
                        var resultInfo = CameraResultInfo[0];
                        CameraResultInfo.RemoveAt(0);

                        SKBitmap bitmap = SKBitmap.Decode(resultInfo.ImagePath);
                        if (resultInfo.Labels.Count>0)
                        {
                            m_DetectionService.DrawImageLabels(bitmap,resultInfo.Labels);
                        }
                        App.Current.Dispatcher.BeginInvoke(() =>
                        {
                            ImageCamera = SKBitmapExtensions.ToBitmapSource(bitmap);
                        });
                    }
                    
                    if (C16C25ResultInfo.Count > 0)
                    {
                        // 弹出结果
                        var resultInfo = C16C25ResultInfo[0];
                        C16C25ResultInfo.RemoveAt(0);

                        // 标签分类
                        var _kongs = resultInfo.Labels.Where((label) =>label.Id==0||label.Id==1);
                        var _labels = resultInfo.Labels.Where((label) => label.Id != 0 && label.Id != 1);
                        List<DetectionReaultLabel> kongs = new List<DetectionReaultLabel>();
                        kongs.AddRange(_kongs);
                        List<DetectionReaultLabel> labels = new List<DetectionReaultLabel>();
                        labels.AddRange(_labels);

                        // 在图像上绘出结果
                        SKBitmap bitmap = SKBitmap.Decode(resultInfo.ImagePath);
                        m_DetectionService.DrawImageKongs(bitmap,kongs);
                        if(labels.Count>0)
                        {
                            m_DetectionService.DrawImageLabels(bitmap,labels);
                        }
                        App.Current.Dispatcher.BeginInvoke(() =>
                        {
                            ImageC16C25 = SKBitmapExtensions.ToBitmapSource(bitmap);
                        });
                    }
                    App.Current.Dispatcher.BeginInvoke(() =>
                    {
                        loadingView.Close();
                    });
                });
            }catch(Exception ex)
            {
                MessageBox.Show($"弹出图像时发生错误：{ex.Message}");
            }
        }
        #endregion

        #region Func

        void InitDetectionResult()
        {
            OkModel.Key = "OK";
            OkModel.IsOK = true;
            foreach (var item in DetectionModels) 
            {
                item.IsOK = true;
            }
        }

        void UpdateDetectionResult(string name)
        {
            App.Current.Dispatcher.Invoke(() => 
            {
                if (name == null)
                    return;
                var res = DetectionModels.FirstOrDefault((res) => res.Key.Contains(name));
                if (res != null) 
                {
                    res.IsOK = false;
                }
            });
        }

        private void CBImage(Services.ImageInfo info)
        {
            Task.Factory.StartNew(new Action(() =>
            {
                if (info.CameaName == "Camera")
                {
                    if (info.Index == 0)
                        m_CameraImages.Clear();
                    m_CameraImages.Add(info.Image);
                    // 拼接图像
                    if (info.Index == 2)
                    {
                        int width = info.Image.Width;
                        int height = info.Image.Height;
                        using var combinedImage = new Image<L8>(width * 3, height);
                        combinedImage.Mutate(ctx =>
                        {
                            ctx.DrawImage(m_CameraImages[2], new SixLabors.ImageSharp.Point(0, 0), 1f);
                            ctx.DrawImage(m_CameraImages[1], new SixLabors.ImageSharp.Point(width, 0), 0, 1f);
                            ctx.DrawImage(m_CameraImages[0], new SixLabors.ImageSharp.Point(width * 2, 0), 0, 1f);
                        });
                        StarDetecting(combinedImage, info.CameaName);
                    }
                }
                else
                {
                    if (info.Index == 0)
                        m_C16C25Images.Clear();
                    m_C16C25Images.Add(info.Image);
                    // 拼接图像
                    if (info.Index == 2)
                    {
                        int width = info.Image.Width;
                        int height = info.Image.Height;
                        using var combinedImage = new Image<L8>(width * 3, height);
                        combinedImage.Mutate(ctx =>
                        {
                            ctx.DrawImage(m_C16C25Images[0], new SixLabors.ImageSharp.Point(0, 0), 1);
                            ctx.DrawImage(m_C16C25Images[1], new SixLabors.ImageSharp.Point(width, 0), 0, 1);
                            ctx.DrawImage(m_C16C25Images[2], new SixLabors.ImageSharp.Point(width * 2, 0), 0, 1);
                        });
                        try
                        {
                            StarDetecting(combinedImage, info.CameaName);
                        }
                        catch (Exception ex)
                        {
                            LoggerHelper._.Error("", ex);
                        }
                    }
                }
            }));

        }

        private void SetWoodInfo(WoodInfo info)
        {
            m_WoodInfo = info;
            m_cameraService.SetImageIndex();
        }
        private void StarDetecting(Image image, string cameraName)
        {
            try
            {
                /*进行路径拼接 */
                string or = m_WoodInfo.ORCode;
                string dateTimeStr = DateTime.Now.ToString("yyyy-MM-dd") + "\\";
                string save_img_dir = ConfigModel.SAVE_IMGS_DIR + "TongShiTai" + "\\" + dateTimeStr + or + "\\"+ConfigModel.SaveImageGuid+"\\";
                if (Directory.Exists(save_img_dir) == false)
                    Directory.CreateDirectory(save_img_dir);
                // 保存图像路径
                string save_img_path = save_img_dir+cameraName + DateTime.Now.ToString("yyyy-MM-dd-hhmmss") + ".jpg";
                string save_json_path = ConfigModel.SAVE_IMGS_DIR + "TongShiTai" + "\\"+dateTimeStr + "detection_result.json";
                slim.Wait();
                var res = m_DetectionService.StartDetecting(image, cameraName, save_img_path, save_json_path);
                slim.Release();
                if (res != null)
                {
                    //DrawImage(res);
                    if(res.CameraName=="Camera")
                    {
                        CameraResultInfo.Add(res);
                    }
                    else
                    {
                        C16C25ResultInfo.Add(res);
                    }
                }
            }
            catch (ArgumentNullException ex)
            {
                LoggerHelper._.Error("图像为空值",ex);
                slim.Release();
            }
            
        }

        private void UpdatedStatus(string cameraName, bool status)
        {
            if (cameraName.Contains("Camera"))
            {
                CameraConnected = status;
            }
            else if (cameraName.Contains("C16"))
            {
                C1Connected = status;
            }
            else if (cameraName.Contains("C25"))
            {
                C2Connected = status;
            }
        }
        #endregion




    }


}
