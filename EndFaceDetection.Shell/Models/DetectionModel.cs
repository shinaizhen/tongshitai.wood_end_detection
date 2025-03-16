using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EndFaceDetection.Shell.Models
{
    /// <summary>
    /// 检测显示控件binding的数据
    /// </summary>
    public partial class DetectionModel:ObservableObject
    {
        [ObservableProperty]
        private string? key;
        [ObservableProperty]
        private bool? isOK;

        public int ID;
    }
}
