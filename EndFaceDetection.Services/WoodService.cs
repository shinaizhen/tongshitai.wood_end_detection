using EndFaceDetection.Services.Common;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EndFaceDetection.Services
{
    public class WoodService
    {
        string _path="wood_info.json";

        /// <summary>
        /// 根据二维码设置拍照位置
        /// </summary>
        /// <param name="orCode">二维码必须全部大写</param>
        public void SetWoodPosition(WoodInfo woodInfo)
        {
            if (woodInfo != null)
            {
                WoodPosition.SetPositions(woodInfo);
            }
        }

        /// <summary>
        /// 获取木板型号信息
        /// </summary>
        public IEnumerable<WoodInfo> GetWoodInfos()
        {
            return JsonHelper.ReadJsonFile<List<WoodInfo>>(_path)?? new List<WoodInfo>();
        }

        /// <summary>
        /// 获取木板型号信息
        /// </summary>
        public IEnumerable<WoodInfo> GetWoodInfos(string path)
        {
            return JsonHelper.ReadJsonFile<List<WoodInfo>>(path) ?? new List<WoodInfo>();
        }

        /// <summary>
        /// 保存木板型号信息
        /// </summary>
        /// <param name="data"></param>
        public void SaveWoodInfos(IEnumerable<WoodInfo> data)
        {
            if (data != null)
            {
                JsonHelper.WriteJsonFile(_path, data);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        /// <param name="path"></param>
        public void SaveWoodInfos(IEnumerable<WoodInfo> data,string path)
        {
            if (data != null)
            {
                JsonHelper.WriteLargeJsonFile(path, data);
            }
        }
    }

    /// <summary>
    /// 存储木板型号和拍照节点信息
    /// </summary>
    public class WoodInfo
    {
        public string ORCode { get; set; } = "AA08K";    //  存储二维码信息
        public string CameraName { get; set; } = "C1";     //  存储需要拍照相机名称（相机需要切换）
        public ushort Positon1 {  get; set; }
        public ushort Positon2 {  get; set; }
        public ushort Positon3 {  get; set; }
    }
}
