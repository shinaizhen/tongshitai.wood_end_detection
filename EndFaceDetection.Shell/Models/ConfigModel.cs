using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EndFaceDetection.Shell.Models
{
    public static class ConfigModel
    {
        /// <summary>
        /// 保存数据的磁盘位置，默认是d盘
        /// </summary>
        public static string? SAVE_IMGS_DIR { get; set; }

        public static string? SYSTEM_DRIVE_NAME { get; set; }

        static string? guidImageFile;

        /// <summary>
        /// 图像保存位置
        /// </summary>
        public static string? SaveImageGuid {get=>guidImageFile;}

        /// <summary>
        /// 更新木板唯一标识
        /// </summary>
        public static void UpdateGuid()
        {
            guidImageFile = Guid.NewGuid().ToString();
        }

        private static readonly object lockObject = new object();
        public static void SetDefalutSettings()
        {
            lock (lockObject)
            {
                SAVE_IMGS_DIR = DiskModel.GetLargestAvailableDriveExcludingSystemDrive().Name;
                SYSTEM_DRIVE_NAME = DiskModel.GetSystemDriveName().Name;
            }
        }

        /// <summary>
        /// 从文件中加载系统设置
        /// </summary>
        /// <param name="settingsFile"></param>
        /// <returns></returns>
        public static int LoadSettingsFromFile(string settingsFile)
        {
            if (!File.Exists(settingsFile))
            {
                //文件不存在！
                return -1;
            }
            else
            {
                return 0;
            }

        }
    }
}
