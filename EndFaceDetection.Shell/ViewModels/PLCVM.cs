using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EndFaceDetection.Services;
using EndFaceDetection.Services.Events;
using EndFaceDetection.Shell.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EndFaceDetection.Shell.ViewModels
{
    public partial class PLCVM:ObservableObject
    {
        WoodService woodService { get; set; }
        PLCService plcService { get; set; }
        public PLCVM() 
        {
            App app = App._app;
            woodService = app.WoodService;
            plcService = app.PLCService;
            plcService.ConnectedAction += new Action<bool>(UpdateConnected);
            plcService.PLCDataUpdated += new Action<PLCDataUpdatedEvents>(PLCDeviceUpdate);
            Woods = new ObservableCollection<WoodInfo>(woodService.GetWoodInfos());
            woodService.UpdateWoodInfosFunc += new Action(UpdateWoodInfos);
            //Task.Run(() => { plcService.Init(); });
        }

        private void UpdateWoodInfos()
        {
            Woods = new ObservableCollection<WoodInfo>(woodService.GetWoodInfos());
        }

        void PLCDeviceUpdate(PLCDataUpdatedEvents e)
        {
           var device_status=e.DeviceStatus;
            if (device_status != null) 
            {
                switch (device_status.MotorStatus)
                {
                    case Motor_Status.Waiting:
                        MotorStatus = "yellow";
                        break;
                    case Motor_Status.Running:
                        MotorStatus = "green";
                        break;
                    case Motor_Status.Error:
                        MotorStatus = "red";
                        break;
                }
                switch (device_status.WoodStatus) 
                {
                    case Wood_Status.Waiting:
                        WoodStatus = "yellow";
                        break;
                    case Wood_Status.Arrived:
                        WoodStatus = "green";
                        break;
                    case Wood_Status.Unkown:
                        WoodStatus= "red";
                        break;
                }

                switch (device_status.AlarmStatus) 
                {
                    case Alarm_Status.Waiting:
                        AlarmStatus = "yellow";
                        break;
                    case Alarm_Status.Running:
                        AlarmStatus= "green";
                        break;
                    case Alarm_Status.Error:
                        AlarmStatus = "red";
                        break;
                }
            }
            
        }

        private void UpdateConnected(bool obj)
        {
           Connected = obj;
        }

        #region 属性
        [ObservableProperty]
        private bool connected;
        
        [ObservableProperty]
        private string motorStatus="red";

        [ObservableProperty]
        private string alarmStatus="red";

        [ObservableProperty]
        private string woodStatus = "red";

        partial void OnWoodStatusChanging(string? oldValue, string newValue)
        {
            if(oldValue!=newValue)//当值发生变化进行判断
            {
                if(newValue=="green")//木板到来信号
                {
                    ConfigModel.UpdateGuid();
                }
            }
        }

        [ObservableProperty]
        private ObservableCollection<WoodInfo> woods;

        private WoodInfo selectedItem;
        public WoodInfo SelectedItem
        {
            get { return selectedItem; }
            set { SetProperty(ref selectedItem, value);WoodPosition.SetPositions(selectedItem); }
        }
        #endregion

        #region 命令
        /// <summary>
        /// 手动执行一次拍照程序
        /// </summary>
        [RelayCommand]
        private void ManualSelect()
        {
            ConfigModel.UpdateGuid();
            plcService.Grab();
        }

        [RelayCommand]
        private void MaualToZero()
        {
            plcService.ToZero();
        }
        [RelayCommand]
        private void Alarm()
        {
            plcService.Alarm(false);
        }

        [RelayCommand]
        private void DisAlarm()
        {
            plcService.DisAlarm();
        }
        #endregion
    }
}
