using System.IO;
using System.Net.Sockets;
using System.Net;
using NModbus.Device;
using NModbus.Data;
using NModbus;
using EndFaceDetection.LogModule;
using EndFaceDetection.Common;
using System.Timers;
using EndFaceDetection.Services.Common;
using EndFaceDetection.Services.Events;
using System.Diagnostics.Metrics;


namespace EndFaceDetection.Services
{
    public class PLCService
    {
        bool m_bConnected=false;
        PLCTcpHelper? helper;
        System.Timers.Timer m_timer;
        string plc_IP = "192.168.1.79";
        //string plc_IP = "127.0.0.1";
        int plc_Port = 502;

        #region 模块通信

        //plc与相机模块通信
        public Action? PopImageAction;//切换图像
        public Action<WoodInfo>? GrabbedAction;//开始采集后，需要进行的操作

        /// <summary>
        /// plc设备状态更新
        /// </summary>
        public Action<PLCDataUpdatedEvents> PLCDataUpdated;

        public Action<bool> ConnectedAction;
        //包装plc设备状态信息
        PLCDeviceStatus pLCDeviceStatus = new PLCDeviceStatus();
       
        #endregion

        DateTime dtDisconnect;
        DateTime dtNow;
        public PLCService() 
        {
            dtDisconnect = new DateTime();
            dtNow = new DateTime();
            m_timer?.Dispose();
            m_timer = new System.Timers.Timer();
            m_timer.Interval = 100;
            m_timer.Elapsed += Listen_PLC_Status;
            m_timer.Enabled = false;
        }

        public void Uninit() 
        {
            helper?.DisConnect();
        }

        private void Listen_PLC_Status(object? sender, ElapsedEventArgs e)
        {
            try
            {
                if (m_bConnected) 
                {
                    ConnectedAction?.Invoke(m_bConnected);
                    var status = helper?.ReadHoldingRegisters(3, 3);
                    if (status == null)
                    {
                        m_bConnected = false;
                        return;
                    }
                    switch (status[1])  //  报警器状态
                    {
                        case 0:
                            pLCDeviceStatus.AlarmStatus = Alarm_Status.Waiting;
                            break;
                        case 1:
                            pLCDeviceStatus.AlarmStatus = Alarm_Status.Running;
                            break;
                        case 2:
                            pLCDeviceStatus.AlarmStatus = Alarm_Status.Error;
                            break;
                        default:
                            pLCDeviceStatus.AlarmStatus = Alarm_Status.Unknow;
                            break;
                    }

                    switch (status[0])  //  电机状态
                    {
                        case 0:
                            pLCDeviceStatus.MotorStatus = Motor_Status.Waiting;
                            break;
                        case 1:
                            pLCDeviceStatus.MotorStatus = Motor_Status.Running;
                            break;
                        default:
                            pLCDeviceStatus.MotorStatus = Motor_Status.Error;
                            break;
                    }

                    switch (status[2])  //  等待木板状态
                    {
                        case 0:
                            pLCDeviceStatus.WoodStatus = Wood_Status.Waiting;
                            break;
                        case 1:
                            pLCDeviceStatus.WoodStatus = Wood_Status.Arrived;//木板到了，开始执行检测程序
                            m_bConnected = helper?.WriteSingleHoldingRegiset(5, 0) ?? false;
                            if (m_bConnected)
                            {
                                Task.Run(() =>
                                {
                                    Grab();
                                });
                            }
                            break;
                        default:
                            pLCDeviceStatus.WoodStatus = Wood_Status.Unkown;
                            break;
                    }

                    //  检测切换木板传感器
                    var woodChanged = helper?.ReadHoldingRegisters(10, 1);
                    if (woodChanged != null)    
                    {
                        if (woodChanged[0] == 1)//  检测到切换木板
                        {
                            m_bConnected = helper?.WriteSingleHoldingRegiset(10, 0)?? false;
                            PopImageAction?.Invoke();
                        }
                    }
                    else
                    {
                        m_bConnected = false;
                    }
                    PLCDataUpdated?.Invoke(new PLCDataUpdatedEvents { DeviceStatus= pLCDeviceStatus});
                }
                else
                {
                    
                    dtNow = DateTime.Now;
                    if ((dtNow - dtDisconnect) > TimeSpan.FromMilliseconds(500))
                    {
                        m_bConnected = helper?.PLCTCPIPConnect()?? false;
                        
                        if (!m_bConnected) 
                        {
                            dtDisconnect = dtNow;
                        }else
                        {
                            ConnectedAction?.Invoke(m_bConnected);
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                LoggerHelper._.Error("监听plc状态时发生错误！",ex);
                m_bConnected = false;
            }
        }

        public bool CheckIPStatus()
        {
            return CheckNetworkByPing.Check(plc_IP);
        }

        /// <summary>
        /// plc服务模块初始化
        /// </summary>
        public void Init()
        {
            try
            {
                ConnectedAction?.Invoke(m_bConnected);
                helper = new PLCTcpHelper(plc_IP, plc_Port);
                m_bConnected = helper.PLCTCPIPConnect();
                if (m_bConnected)
                {
                    m_timer.Enabled = true;
                    m_timer.Start();
                    ConnectedAction?.Invoke(m_bConnected);
                }
            }
            catch (Exception ex) 
            {
                LoggerHelper._.Error("plc服务器初始化时发生错误！",ex);
            }
        }

        #region plc报警模块
        /// <summary>
        /// 报警
        /// </summary>
        /// <param name="isStop">是否需要plc挂起|true：需要挂起 false：不需要挂起</param>
        public void Alarm(bool isStop)
        {
            if (isStop) //  挂起
            {
                m_bConnected = helper?.WriteSingleHoldingRegiset(16, 2) ?? false;
                pLCDeviceStatus.AlarmStatus = Alarm_Status.Error;
            }
            else  //  不需要挂起
            {
                m_bConnected = helper?.WriteSingleHoldingRegiset(16, 1) ?? false;
                Thread.Sleep(3000);
                m_bConnected = helper?.WriteSingleHoldingRegiset(16, 0) ?? false;
            }
        }

        /// <summary>
        /// 取消报警
        /// </summary>
        public void DisAlarm()
        {
            if (pLCDeviceStatus.AlarmStatus == Alarm_Status.Running)//报警位是可取消报警时可以取消报警
            {
                m_bConnected = helper?.WriteSingleHoldingRegiset(16, 0) ?? false;
            }
        }

        public void ToZero()
        {
            m_bConnected = helper?.WriteSingleHoldingRegiset(1, 8, 1)?? false;
        }
        #endregion

        #region plc取流模块
        public void Grab()
        {
            try
            {
                var woodInfo = WoodPosition.GetWoodInfo();
                GrabbedAction?.Invoke(woodInfo);
                Thread.Sleep(100);
                // 切换相机
                if (woodInfo.CameraName == "C2")
                    helper?.WriteSingleHoldingRegiset(25, 0);
                else
                    helper?.WriteSingleHoldingRegiset(25, 2);
                
                // 写入拍照节点
                var positions = WoodPosition.GetPositions();
                helper?.WriteMultipleRegisters(11, positions);
                helper?.WriteMultipleRegisters(17, positions);

                //开始拍照
                helper?.WriteSingleHoldingRegiset(41, 1);
                
            }
            catch (Exception ex) 
            {
                LoggerHelper._.Error("",ex);
            }
        }

        
        #endregion
    }
}
