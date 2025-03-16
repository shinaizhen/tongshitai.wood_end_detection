using EndFaceDetection.Services.MInterfaces;
using MvCamCtrl.NET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using EndFaceDetection.LogModule;
using Image = SixLabors.ImageSharp.Image;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using EndFaceDetection.Common;

namespace EndFaceDetection.Services
{
    public class CameraService : ICamera
    {
        //回调函数委托
        static MyCamera.cbOutputExdelegate? ImageCallback;
        static MyCamera.cbExceptiondelegate? pCallBackFunc;

        //用于展示设备列表
        MyCamera.MV_CC_DEVICE_INFO_LIST m_stDeviceList = new MyCamera.MV_CC_DEVICE_INFO_LIST();

        //线程同步，用于控制保存图片操作的线程
        static ManualResetEvent _event = new ManualResetEvent(true);

        //用于保护线程的资源
        static object _lock = new object();

        /// <summary>
        /// 用于当相机状态发生改变时，反应到前端
        /// </summary>
        public Action<string, bool> StateChanged { get; set; }

        /// <summary>
        /// 当采集到图像时进行操作
        /// </summary>
        public Action<ImageInfo> CBImage { get; set; }

        /// <summary>
        /// 用于相机名称和相机设备信息类的映射
        /// </summary>
        public  Dictionary<string, HIK_GIG_CameraInfo> MapOfCameraInfo { get; set; } = new Dictionary<string, HIK_GIG_CameraInfo>();

        /// <summary>
        /// 相机名称列表
        /// </summary>
        static List<string> CameraNames { get; set; }= new List<string>() { "Camera","C16","C25"};

        public CameraService()
        {
            pCallBackFunc = new MyCamera.cbExceptiondelegate(cbExceptiondelegate);
            ImageCallback = new MyCamera.cbOutputExdelegate(ImageCallbackFunc);
            GC.KeepAlive(ImageCallback);
            GC.KeepAlive(pCallBackFunc);
        }

        HIK_GIG_CameraInfo GetCameraInfoByName(string name)
        {
            try
            {
                return MapOfCameraInfo[name];
            }
            catch (Exception ex) 
            {
                LoggerHelper._.Error($"通过相机名称获取相机信息时发生错误：{ex.Message}");
                return null;
            }
        }

        HIK_GIG_CameraInfo GetCameraInfoByID(int id)
        {
            var info = MapOfCameraInfo.FirstOrDefault(c => c.Value.CameraID == id).Value;
            if (info == null)
                throw new ArgumentNullException($"{id}为空值");
            return info;
        }

        public bool CheckStatus()
        {
            // 检测是否列举相机
            if(MapOfCameraInfo.Count==0)
                return false;
            //检测相机是否全部获取
            foreach(var item in CameraNames)
            {
                if(MapOfCameraInfo[item] == null) 
                    return false;
            }
            //检测相机是否处于工作状态
            foreach(var item in MapOfCameraInfo)
            {
                var res = item.Value.IsGrabbing&&item.Value.IsConnected;//相机必须打开并开始采集
                if(!res)
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// 相机做初始化
        /// </summary>
        public int InitCamera()
        {
            int nRet = 0;
            if(MapOfCameraInfo.Count>0)
            {
                CloseAll();
            }
            nRet = EnumDevices();
            if (nRet != MyCamera.MV_OK)
            {
                return nRet;
            }
            foreach (var item in MapOfCameraInfo)
            {
                nRet = OpenDeviceBYDevice(item.Value);
                if(nRet !=MyCamera.MV_OK)
                {
                    return nRet;
                }
                nRet = SetParam(item.Value);
                if (nRet != MyCamera.MV_OK)
                {
                    return nRet;
                }
                nRet = StartGrabbingBYDevice(item.Value);
                if (nRet != MyCamera.MV_OK)
                {
                    return nRet;
                }
            }
            return nRet;
        }

        /// 打印相机错误码信息
        /// </summary>
        /// <param name="csMessage">操作信息</param>
        /// <param name="nErrorNum">错误码</param>
        /// <returns></returns>
        public static string PrintError(string csMessage, int nErrorNum)
        {
            string Message;

            if (nErrorNum == 0)
            {
                Message = csMessage;
            }
            else
            {
                Message = csMessage + ": Error =" + String.Format("{0:X}", nErrorNum);
            }

            switch (nErrorNum)
            {
                case MyCamera.MV_E_HANDLE: Message += " Error or invalid handle "; break;
                case MyCamera.MV_E_SUPPORT: Message += " Not supported function "; break;
                case MyCamera.MV_E_BUFOVER: Message += " Cache is full "; break;
                case MyCamera.MV_E_CALLORDER: Message += " Function calling order error "; break;
                case MyCamera.MV_E_PARAMETER: Message += " Incorrect parameter "; break;
                case MyCamera.MV_E_RESOURCE: Message += " Applying resource failed "; break;
                case MyCamera.MV_E_NODATA: Message += " No data "; break;
                case MyCamera.MV_E_PRECONDITION: Message += " Precondition error, or running environment changed "; break;
                case MyCamera.MV_E_VERSION: Message += " Version mismatches "; break;
                case MyCamera.MV_E_NOENOUGH_BUF: Message += " Insufficient memory "; break;
                case MyCamera.MV_E_UNKNOW: Message += " Unknown error "; break;
                case MyCamera.MV_E_GC_GENERIC: Message += " General error "; break;
                case MyCamera.MV_E_GC_ACCESS: Message += " Node accessing condition error "; break;
                case MyCamera.MV_E_ACCESS_DENIED: Message += " No permission "; break;
                case MyCamera.MV_E_BUSY: Message += " Device is busy, or network disconnected "; break;
                case MyCamera.MV_E_NETER: Message += " Network error "; break;
            }
            Message += "PROMPT";
            return Message;
        }

        #region 列举设备列表
        public int EnumDevices()
        {
            // ch:创建设备列表 | en:Create Device List
            System.GC.Collect();
            MapOfCameraInfo.Clear();
            m_stDeviceList.nDeviceNum = 0;
            int nRet = MyCamera.MV_CC_EnumDevices_NET(MyCamera.MV_GIGE_DEVICE | MyCamera.MV_USB_DEVICE, ref m_stDeviceList);//枚举usb或者千兆网口设备列表
            if (0 != nRet)//获取失败
            {
                LoggerHelper._.Debug(PrintError("枚举相机失败！",nRet));
                return nRet;
            }

            // ch:在窗体列表中显示设备名 | en:Display device name in the form list
            for (int i = 0; i < m_stDeviceList.nDeviceNum; i++)
            {
                MyCamera.MV_CC_DEVICE_INFO device = (MyCamera.MV_CC_DEVICE_INFO)Marshal.PtrToStructure(m_stDeviceList.pDeviceInfo[i], typeof(MyCamera.MV_CC_DEVICE_INFO));
                if (device.nTLayerType == MyCamera.MV_GIGE_DEVICE)
                {
                    MyCamera.MV_GIGE_DEVICE_INFO gigeInfo = (MyCamera.MV_GIGE_DEVICE_INFO)MyCamera.ByteToStruct(device.SpecialInfo.stGigEInfo, typeof(MyCamera.MV_GIGE_DEVICE_INFO));

                    if (gigeInfo.chUserDefinedName != "")
                    {
                        var cameraInfo = new HIK_GIG_CameraInfo() { CameraID = i, CameraName =  gigeInfo.chUserDefinedName ,SerialNumber = gigeInfo.chSerialNumber, TCP_IP = gigeInfo.nCurrentIp };
                        if(CameraNames.Contains(cameraInfo.CameraName))
                        {
                            MapOfCameraInfo.Add(cameraInfo.CameraName, cameraInfo);
                        }
                    }
                    else
                    {
                        var cameraInfo = new HIK_GIG_CameraInfo() { CameraID = i, CameraName =  gigeInfo.chManufacturerName + " " + gigeInfo.chModelName, SerialNumber=gigeInfo.chSerialNumber , TCP_IP = gigeInfo.nCurrentIp };
                        if (CameraNames.Contains(cameraInfo.CameraName))
                        {
                            MapOfCameraInfo.Add(cameraInfo.CameraName,cameraInfo);
                        }
                    }
                }
                else if (device.nTLayerType == MyCamera.MV_USB_DEVICE)//未实现部分功能，ip地址对应设备序列号
                {
                    MyCamera.MV_USB3_DEVICE_INFO usbInfo = (MyCamera.MV_USB3_DEVICE_INFO)MyCamera.ByteToStruct(device.SpecialInfo.stUsb3VInfo, typeof(MyCamera.MV_USB3_DEVICE_INFO));
                    if (usbInfo.chUserDefinedName != "")
                    {
                        var cameraInfos = new HIK_GIG_CameraInfo() { CameraName = "U3V: " + usbInfo.chUserDefinedName + " (" + usbInfo.chSerialNumber + ")", CameraID = i, TCP_IP = usbInfo.nDeviceNumber };
                        if(CameraNames.Contains(cameraInfos.CameraName))
                        {
                            MapOfCameraInfo.Add(cameraInfos.CameraName,cameraInfos);
                        }
                    }
                    else
                    {
                        var cameraInfos = new HIK_GIG_CameraInfo() { CameraName = "U3V: " + usbInfo.chManufacturerName + " " + usbInfo.chModelName + " (" + usbInfo.chSerialNumber + ")", CameraID = i, TCP_IP = usbInfo.nDeviceNumber };
                        if (CameraNames.Contains(cameraInfos.CameraName))
                        {
                            MapOfCameraInfo.Add(cameraInfos.CameraName, cameraInfos);
                        }
                    }
                }

            }
            return nRet;
        }
        #endregion

        #region 打开设备
        public int OpenDeviceBYName(string name)
        {
            var cameraInfo=GetCameraInfoByName(name);
            if (cameraInfo == null)
            {
                return -1;
            }
            var nRet=OpenDeviceBYDevice(cameraInfo);
            return nRet;
        }
        public int OpenDeviceBYDevice(HIK_GIG_CameraInfo cameraInfo)
        {
            cameraInfo.IsConnected = false;
            StateChanged?.Invoke(cameraInfo.CameraName, false);
            // ch:获取选择的设备信息 | en:Get selected device information
            //其中CameraID为选择的相机的ID与pDeviceInfo句柄列表一一对应
            cameraInfo.m_pDeviceInfo =
                (MyCamera.MV_CC_DEVICE_INFO)Marshal.PtrToStructure(m_stDeviceList.pDeviceInfo[cameraInfo.CameraID], typeof(MyCamera.MV_CC_DEVICE_INFO));
            if (cameraInfo.m_pMyCamera == null)
                cameraInfo.m_pMyCamera = new MyCamera();
            var nRet = cameraInfo.m_pMyCamera.MV_CC_CreateDevice_NET(ref cameraInfo.m_pDeviceInfo);//创建句柄
            if (MyCamera.MV_OK != nRet)
            {
                LoggerHelper._.Debug(PrintError("创建相机句柄失败!", nRet));
                return nRet;
            }

            nRet = cameraInfo.m_pMyCamera.MV_CC_OpenDevice_NET();//打开设备
            if (MyCamera.MV_OK != nRet)
            {
                cameraInfo.m_pMyCamera.MV_CC_DestroyDevice_NET();
                LoggerHelper._.Debug(PrintError($"打开相机设备{cameraInfo.CameraName}失败!", nRet));
                return nRet;
            }
            // ch:探测网络最佳包大小(只对GigE相机有效) | en:Detection network optimal package size(It only works for the GigE camera)
            if (cameraInfo.m_pDeviceInfo.nTLayerType == MyCamera.MV_GIGE_DEVICE)
            {
                int nPacketSize = cameraInfo.m_pMyCamera.MV_CC_GetOptimalPacketSize_NET();
                if (nPacketSize > 0)
                {
                    nRet = cameraInfo.m_pMyCamera.MV_CC_SetIntValueEx_NET("GevSCPSPacketSize", nPacketSize);
                    if (nRet != MyCamera.MV_OK)
                    {
                        LoggerHelper._.Debug(PrintError($"设备{cameraInfo.CameraName}：网络最佳包设置失败", nRet));
                        return nRet;
                    }
                }
                else
                {
                    LoggerHelper._.Debug(PrintError($"设备{cameraInfo.CameraName}：网络最佳包获取失败！", nPacketSize));
                    return nPacketSize;
                }
            }

            nRet = cameraInfo.m_pMyCamera.MV_CC_RegisterImageCallBackEx_NET(ImageCallback,(IntPtr)cameraInfo.CameraID);
            nRet = cameraInfo.m_pMyCamera.MV_CC_RegisterExceptionCallBack_NET(pCallBackFunc, (IntPtr)cameraInfo.CameraID);
            cameraInfo.IsConnected = true;
            StateChanged?.Invoke(cameraInfo.CameraName, true);
            return nRet;
        }
        #endregion


        #region 关闭设备

        public int CloseCameraBYName(string name)
        {
            var cameraInfo = GetCameraInfoByName(name);
            if (cameraInfo == null)
            {
                return -1;
            }    
            return CloseCameraBYDevice(cameraInfo);
        }

        public int CloseCameraBYDevice(HIK_GIG_CameraInfo cameraInfo)
        {
            var nRet = MyCamera.MV_OK;
            if (cameraInfo.IsGrabbing)
            {
                nRet = StopGrabbingBYName(cameraInfo.CameraName);
                if (nRet != MyCamera.MV_OK)
                {
                    LoggerHelper._.Debug(PrintError($"相机（{cameraInfo.CameraName}）关闭失败！", nRet));
                    return nRet;
                }
                // ch:取流标志位清零 | en:Reset flow flag bit
                cameraInfo.IsGrabbing = false;
            }

            // ch:关闭设备 | en:Close Device
            nRet = cameraInfo.m_pMyCamera.MV_CC_CloseDevice_NET();
            if (nRet != MyCamera.MV_OK)
            {
                LoggerHelper._.Debug(PrintError($"相机（{cameraInfo.CameraName}）关闭失败！", nRet));
                return nRet;
            }
            nRet = cameraInfo.m_pMyCamera.MV_CC_DestroyDevice_NET();
            if (nRet != MyCamera.MV_OK)
            {
                LoggerHelper._.Debug(PrintError($"相机（{cameraInfo.CameraName}）销毁句柄失败！", nRet));
                return nRet;
            }
            // ch:控件操作 | en:Control Operation
            cameraInfo.IsConnected = false;//相机状态关闭
            StateChanged?.Invoke(cameraInfo.CameraName, false);
            GC.Collect();
            LoggerHelper._.Info($"关闭设备（{cameraInfo.CameraName}）。");
            return nRet;
        }

        public void CloseAll()
        {
            foreach (var device in MapOfCameraInfo)
            {
                CloseCameraBYDevice(device.Value);
            }
        }
        #endregion

        #region 设置参数
        public int SetParam(HIK_GIG_CameraInfo cameraInfo)//设置参数
        {
            int nRet = cameraInfo.m_pMyCamera.MV_GIGE_SetGvspTimeout_NET(300);
            if (MyCamera.MV_OK != nRet)
            {
                LoggerHelper._.Debug(PrintError($"相机（{cameraInfo.CameraName}）设置GVSP超时失败", nRet));
                return nRet;
            }
            nRet = cameraInfo.m_pMyCamera.MV_GIGE_SetResendMaxRetryTimes_NET(50);
            if (MyCamera.MV_OK != nRet)
            {
                LoggerHelper._.Debug(PrintError($"相机（{cameraInfo.CameraName}）设置重传命令最大尝试次数失败", nRet));
                return nRet;
            }
            nRet = cameraInfo.m_pMyCamera.MV_GIGE_SetResendTimeInterval_NET(20);
            if (MyCamera.MV_OK != nRet)
            {
                LoggerHelper._.Debug(PrintError($"相机（{cameraInfo.CameraName}）设置同一重传包多次请求之间的时间间隔失败", nRet));
                return nRet;
            }
            nRet = cameraInfo.m_pMyCamera.MV_GIGE_SetRetryGvcpTimes_NET(50);
            if (MyCamera.MV_OK != nRet)
            {
                LoggerHelper._.Debug(PrintError($"相机（{cameraInfo.CameraName}）设置重传GVCP命令次数失败", 0));
                return nRet;
            }
            nRet = cameraInfo.m_pMyCamera.MV_GIGE_SetGvcpTimeout_NET(500);
            if (MyCamera.MV_OK != nRet)
            {
                LoggerHelper._.Debug(PrintError($"相机（{cameraInfo.CameraName}）设置GVCP命令超时时间失败", nRet));
                return nRet;
            }
            nRet = cameraInfo.m_pMyCamera.MV_CC_SetIntValueEx_NET("GevHeartbeatTimeout", 3000);
            if (MyCamera.MV_OK != nRet)
            {
                LoggerHelper._.Debug(PrintError($"相机（{cameraInfo.CameraName}）设置心跳时间失败", nRet));
                return nRet;
            }
            nRet = cameraInfo.m_pMyCamera.MV_CC_SetIntValueEx_NET("Width", 1920);
            if (MyCamera.MV_OK != nRet)
            {
                LoggerHelper._.Debug(PrintError($"相机（{cameraInfo.CameraName}）设置宽度失败", nRet));
                return nRet;
            }

            nRet = cameraInfo.m_pMyCamera.MV_CC_SetIntValueEx_NET("OffsetX", 1920);

            if (MyCamera.MV_OK != nRet)
            {
                LoggerHelper._.Debug(PrintError($"相机（{cameraInfo.CameraName}）设置图像X轴偏移失败", nRet));
                return nRet;
            }
            
            //设置相机的曝光
            if (cameraInfo.CameraName.Contains("Camera"))
            {
                cameraInfo.m_pMyCamera.MV_CC_SetEnumValue_NET("ExposureAuto", 0);
                nRet = cameraInfo.m_pMyCamera.MV_CC_SetFloatValue_NET("ExposureTime", 18000);
                if (nRet != MyCamera.MV_OK)
                {
                    LoggerHelper._.Debug(PrintError($"相机（{cameraInfo.CameraName}）设置曝光时间失败！", nRet));
                    return nRet;
                }
            }else if(cameraInfo.CameraName.Contains("C1"))
            {
                cameraInfo.m_pMyCamera.MV_CC_SetEnumValue_NET("ExposureAuto", 0);
                nRet = cameraInfo.m_pMyCamera.MV_CC_SetFloatValue_NET("ExposureTime", 30000);
                if (nRet != MyCamera.MV_OK)
                {
                    LoggerHelper._.Debug(PrintError($"相机（{cameraInfo.CameraName}）设置曝光时间失败！", nRet));
                    return nRet;
                }
            }
            else if (cameraInfo.CameraName.Contains("C2"))
            {
                cameraInfo.m_pMyCamera.MV_CC_SetEnumValue_NET("ExposureAuto", 0);
                nRet = cameraInfo.m_pMyCamera.MV_CC_SetFloatValue_NET("ExposureTime", 50000);
                if (nRet != MyCamera.MV_OK)
                {
                    LoggerHelper._.Debug(PrintError($"相机（{cameraInfo.CameraName}）设置曝光时间失败！", nRet));
                    return nRet;
                }
            }
            return nRet;
        }

        public int FeatureSave2FileByDevice(HIK_GIG_CameraInfo camera, string fileName)
        {
            var nRet = MyCamera.MV_OK;
            if (!camera.IsConnected)
            {
                nRet = OpenDeviceBYDevice(camera);
                if (nRet != MyCamera.MV_OK)
                    return nRet;
            }
            nRet = camera.m_pMyCamera.MV_CC_FeatureSave_NET(fileName);
            if (MyCamera.MV_OK != nRet)
            {
                return nRet;
            }
            return nRet;
        }

        public int FeatureLoadFromFile(HIK_GIG_CameraInfo camera, string fileName)
        {
            var nRet = MyCamera.MV_OK;
            if (!camera.IsConnected)
            {
                nRet = OpenDeviceBYDevice(camera);
                if (nRet != MyCamera.MV_OK)
                    return nRet;
            }
            nRet = camera.m_pMyCamera.MV_CC_FeatureLoad_NET(fileName);
            return nRet;
        }
        #endregion


        #region 取流控制
        public int StartGrabbingBYDevice(HIK_GIG_CameraInfo cameraInfo)//开始采集
        {
            #region 硬触发
            //打开触发模式
            int nRet = cameraInfo.m_pMyCamera.MV_CC_SetEnumValue_NET("TriggerMode", (uint)MyCamera.MV_CAM_TRIGGER_MODE.MV_TRIGGER_MODE_ON);
            if (MyCamera.MV_OK != nRet)
            {
                LoggerHelper._.Debug(PrintError($"相机（{cameraInfo.CameraName}）打开触发模式失败！", nRet));
                return nRet;
            }
            nRet = cameraInfo.m_pMyCamera.MV_CC_SetEnumValue_NET("TriggerSource", (uint)MyCamera.MV_CAM_TRIGGER_SOURCE.MV_TRIGGER_SOURCE_LINE0);
            if (MyCamera.MV_OK != nRet)
            {
                LoggerHelper._.Debug(PrintError($"相机（{cameraInfo.CameraName}）设置硬触发失败！", nRet));
                return nRet;
            }
            #endregion

            #region 软触发
            // ch:设置采集连续模式 | en:Set Continues Aquisition Mode
            //m_pMyCamera[id].MV_CC_SetEnumValue_NET("AcquisitionMode", (uint)MyCamera.MV_CAM_ACQUISITION_MODE.MV_ACQ_MODE_CONTINUOUS);
            ////关闭触发模式
            //m_pMyCamera[id].MV_CC_SetEnumValue_NET("TriggerMode", (uint)MyCamera.MV_CAM_TRIGGER_MODE.MV_TRIGGER_MODE_OFF);
            #endregion
            cameraInfo.IsGrabbing = false;
            // ch:开始采集 | en:Start Grabbing
            nRet = cameraInfo.m_pMyCamera.MV_CC_StartGrabbing_NET();
            if (MyCamera.MV_OK != nRet)
            {
               LoggerHelper._.Debug(PrintError($"相机（{cameraInfo.CameraName}）设置开始采集失败！", nRet));
                return nRet;
            }
            cameraInfo.IsGrabbing = true;
            return nRet;
        }

        public int StopGrabbingBYName(string name)//停止采集
        {

            var cameraInfo = GetCameraInfoByName(name);
            if (cameraInfo == null)
            {
                LoggerHelper._.Debug($"停止采集时，相机（{name}）获取失败！");
                return -1;
            }
            return StopGrabbingBYDevice(cameraInfo);
           
        }

        public int StopGrabbingBYDevice(HIK_GIG_CameraInfo cameraInfo)
        {
            // ch:标志位设为false | en:Set flag bit false
            cameraInfo.IsGrabbing = false;

            // ch:停止采集 | en:Stop Grabbing
            int nRet = cameraInfo.m_pMyCamera.MV_CC_StopGrabbing_NET();
            if (nRet != MyCamera.MV_OK)
            {
                PrintError("停止采集失败！", nRet);
            }
            return nRet;
        }
        #endregion


        /// <summary>
        /// 掉线重连
        /// </summary>
        /// <param name="nMsgType"></param>
        /// <param name="pUser"></param>
        private void cbExceptiondelegate(uint nMsgType, IntPtr pUser)
        {
            try
            {
                _event.Reset();//线程阻塞
                int id = (int)pUser;
                var cameraInfo = MapOfCameraInfo.FirstOrDefault(c => c.Value.CameraID == id);
                var nRet = MyCamera.MV_OK;
                do
                {
                    nRet = OpenDeviceBYName(cameraInfo.Value.CameraName);
                } while (nRet == MyCamera.MV_OK);
            }
            catch (Exception ex)
            {
                LoggerHelper._.Error($"相机断线重连发生错误：{ex.Message}");
            }finally
            {
                _event.Reset();
            }
        }

        int index_Camera = 0;
        int index_C1_C2 = 0;

        public void SetImageIndex()
        {
            lock(_lock)
            {
                index_Camera = 0;
                index_C1_C2 = 0;
            }
        }

        private void ImageCallbackFunc(IntPtr pData, ref MyCamera.MV_FRAME_OUT_INFO_EX pFrameInfo, IntPtr pUser)//回调取流函数
        {
            try
            {
                int id = (int)pUser;
                var cameraInfo = GetCameraInfoByID(id);
                if (cameraInfo == null)
                {
                    LoggerHelper._.Error("取流时获取不到相机信息");
                    return;
                }
                int index = 0;
                if (pFrameInfo.enPixelType == MyCamera.MvGvspPixelType.PixelType_Gvsp_Mono8)//黑白图像格式
                {
                    //************************Mono8 转 SixLabors.ImageSharp.Image**********************
                    int width = pFrameInfo.nWidth;
                    int height = pFrameInfo.nHeight;
                    // 将 IntPtr 转换为 byte[]
                    byte[] dataBytes = new byte[width * height];
                    Marshal.Copy(pData, dataBytes, 0, dataBytes.Length);
                    // 创建灰度图像对象
                    Image image = Image.LoadPixelData<L8>(dataBytes.AsSpan(), width, height);
                        if (cameraInfo.CameraName.Contains("C25"))
                    {
                        //bmp.RotateFlip(RotateFlipType.Rotate270FlipNone);
                        image.Mutate(ctx => ctx.Rotate(270));
                        lock (_lock)
                        {
                            index = index_C1_C2;
                            index_C1_C2++;
                        }
                    }
                    else if (cameraInfo.CameraName.Contains("C16"))
                    {
                        //bmp.RotateFlip(RotateFlipType.Rotate90FlipNone);
                        // 旋转 90 度
                        image.Mutate(ctx => ctx.Rotate(90));
                        lock (_lock)
                        {
                            index = index_C1_C2;
                            index_C1_C2++;
                        }
                    }
                    else if (cameraInfo.CameraName.Contains("Camera"))
                    {
                        image.Mutate(ctx => ctx.Rotate(90));
                        lock (_lock)
                        {
                            index = index_Camera;
                            index_Camera++;
                        }
                    }

                    CBImage?.Invoke(new ImageInfo { CameaName = cameraInfo.CameraName, Index = index, Image = image });
                }
                else//彩色RGB或者BGR格式
                {
                    //*********************RGB8 转 SixLabors.ImageSharp.Image**************************
                    //RGB与BGR的转换，如果原始数据就是BGR了，也可以省去这个循环，避免浪费时间
                    //for (int i = 0; i < pFrameInfo.nHeight; i++)
                    //{
                    //    for (int j = 0; j < pFrameInfo.nWidth; j++)
                    //    {
                    //        byte chRed = m_pBufForSaveImage[i * pFrameInfo.nWidth * 3 + j * 3];
                    //        m_pBufForSaveImage[i * pFrameInfo.nWidth * 3 + j * 3] = m_pBufForSaveImage[i * pFrameInfo.nWidth * 3 + j * 3 + 2];
                    //        m_pBufForSaveImage[i * pFrameInfo.nWidth * 3 + j * 3 + 2] = chRed;
                    //    }
                    //}

                    return;
                }
            } catch (Exception ex) 
            {
                LoggerHelper._.Error("获取图像时发生错误：",ex);
            }

        }

    }

    /// <summary>
    /// 海康相机千兆网口
    /// 用于对相机设备进行操作、查询
    /// </summary>
    public class HIK_GIG_CameraInfo
    {
        #region 初始化时赋值
        public string SerialNumber { get; set; } = "";
        public string CameraName { get; set; } = "";//相机名称（用户自定义）
        public int CameraID { get; set; }//相机ID
        public UInt32 TCP_IP { get; set; }//相机ip地址
        public bool IsConnected { get; set; } = false;//是否链接
        public bool IsGrabbing { get; set; } = false;//是否正在取流
        #endregion
        #region 主动赋值
        public float ExprosureTime { get; set; }//曝光时间us
        public float TrigerDelayTime { get; set; }//触发延时us
        #endregion
        //用于操作相机 创建句柄，打开设备，关闭设备，释放句柄等
        public MyCamera m_pMyCamera = new MyCamera();
        public MyCamera.MV_CC_DEVICE_INFO m_pDeviceInfo = new MyCamera.MV_CC_DEVICE_INFO();

        public bool CheckIPStatus()
        {
            return CheckNetworkByPing.Check(TCP_IP);
        }
    }

    /// <summary>
    /// 用于存储相机拍照图片信息
    /// </summary>
    public class ImageInfo
    {
        public string CameaName { get; set; } = "";  //  相机名称
        public int Index { get; set; }  //  图片拍照索引因为拍照分三次拍照，故值：0、1、2
        public Image Image { get; set; }   //  图片数据
    }
}
