using CommunityToolkit.Mvvm.ComponentModel;
using EndFaceDetection.Shell.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace EndFaceDetection.Shell.ViewModels
{
    public partial class DiskVM:ObservableObject
    {
        System.Timers.Timer _timer=new System.Timers.Timer(100);
        /// <summary>
        /// 存储最新一次更新磁盘空间的时间
        /// </summary>
        DateTime _lastUpdateTime = DateTime.Now;
        int minimumAvailableSpace = 50;//最低可用空间低于该值报警
        public DiskVM() 
        {
            _timer.Elapsed += Date_Update;
            _timer.Enabled = true;
            _timer.Start();
            CheckDisk();
        }



        private void Date_Update(object? sender, ElapsedEventArgs e)
        {
            this.TimeStr = DateTime.Now.ToString();
            if(DateTime.Now-_lastUpdateTime>TimeSpan.FromMinutes(60))
            {
                CheckDisk();
            }
        }

        #region 属性
        [ObservableProperty]
        private string timeStr;
        [ObservableProperty]
        private ObservableCollection<DiskModel> allDrives;
        #endregion

        #region 私有方法
        private void CheckDisk()
        {
            _lastUpdateTime = DateTime.Now;
            AllDrives = DiskModel.GetAllDrives();
            //if (!DiskModel.IsDiskSpaceSufficient(AppSettings.SAVE_IMGS_DIR, minimumAvailableSpace))
            //{
            //    MessageBoxResult result = MessageBox.Show($"磁盘{AppSettings.SAVE_IMGS_DIR}空间不够，请及时更换磁盘。。。", "提醒", MessageBoxButton.YesNoCancel, MessageBoxImage.Warning);
            //}
        }
        #endregion
    }
}
