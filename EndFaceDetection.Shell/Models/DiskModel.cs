using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EndFaceDetection.Shell.Models
{
    public class DiskModel
    {
        public string Name { get; set; }
        public double TotalSpaceGB { get; set; }
        public double UsedSpaceGB { get; set; }
        public double FreeSpaceGB { get; set; }
        public double UsedSpacePercentage => (UsedSpaceGB / TotalSpaceGB) * 100; // 新增属性


        /// <summary>
        /// 获取包含操作系统的磁盘信息
        /// </summary>
        /// <returns></returns>
        public static DriveInfo GetSystemDriveName()
        {
            // 获取系统盘路径
            string systemDrivePath = Path.GetPathRoot(Environment.SystemDirectory);

            // 获取所有驱动器信息
            DriveInfo[] allDrives = DriveInfo.GetDrives();

            // 查找系统盘的 DriveInfo 对象
            DriveInfo systemDrive = allDrives.FirstOrDefault(drive => drive.Name.Equals(systemDrivePath, StringComparison.OrdinalIgnoreCase));

            return systemDrive;

        }

        /// <summary>
        /// 获取除去操作系统盘外的可用容量最大的磁盘信息
        /// </summary>
        /// <returns></returns>
        public static DriveInfo GetLargestAvailableDriveExcludingSystemDrive()
        {
            // 获取系统盘路径
            string systemDrivePath = Path.GetPathRoot(Environment.SystemDirectory);

            // 获取所有驱动器信息，排除系统盘
            DriveInfo largestDrive = DriveInfo.GetDrives()
                .Where(drive => drive.IsReady && !drive.Name.Equals(systemDrivePath, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(drive => drive.TotalFreeSpace)
                .FirstOrDefault();

            return largestDrive;
        }

        /// <summary>
        /// 判断磁盘空间是否够用
        /// </summary>
        /// <param name="driveName"></param>
        /// <param name="requiredSpace"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public static bool IsDiskSpaceSufficient(string driveName, long requiredSpace)
        {
            DriveInfo drive = new DriveInfo(driveName);

            if (!drive.IsReady)
            {
                throw new ArgumentException($"Drive {driveName} is not ready or does not exist.");
            }

            return drive.AvailableFreeSpace >= requiredSpace * (1024 * 1024 * 1024);
        }


        public static ObservableCollection<DiskModel> GetAllDrives()
        {
            ObservableCollection<DiskModel> DiskDrives = new ObservableCollection<DiskModel>();
            foreach (DriveInfo drive in DriveInfo.GetDrives())
            {
                if (drive.IsReady && drive.DriveType == DriveType.Fixed)
                {
                    DiskDrives.Add(new DiskModel
                    {
                        Name = drive.Name,
                        TotalSpaceGB = Math.Round((double)drive.TotalSize / (1024 * 1024 * 1024), 2), // 转换为GB
                        UsedSpaceGB = Math.Round((double)(drive.TotalSize - drive.TotalFreeSpace) / (1024 * 1024 * 1024), 2),
                        FreeSpaceGB = Math.Round((double)drive.AvailableFreeSpace / (1024 * 1024 * 1024), 2)
                    });
                }
            }
            return DiskDrives;
        }

    }
}
