using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EndFaceDetection.Services
{
    public static class WoodPosition
    {
        static object object_lock=new object();
        static WoodInfo WoodInfo = new WoodInfo();
        /// <summary>
        /// 获取拍照节点
        /// </summary>
        /// <returns></returns>
        public static ushort[] GetPositions()
        {
            lock (object_lock) 
            {
                // 写入拍照点位
                ushort[] positions = new ushort[] {
                                Convert.ToUInt16(WoodInfo.Positon1),
                                Convert.ToUInt16(WoodInfo.Positon2),
                                Convert.ToUInt16(WoodInfo.Positon3)
                            };
                return positions;
            }
        }

        public static WoodInfo GetWoodInfo()
        {
            lock (object_lock)
            {
                return new WoodInfo() {  CameraName=WoodInfo.CameraName,ORCode=WoodInfo.ORCode,Positon1=WoodInfo.Positon1,Positon2=WoodInfo.Positon2,Positon3=WoodInfo.Positon3};
            }
        }
        /// <summary>
        /// 设置拍照节点
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <param name="p3"></param>
        public static void SetPositions(WoodInfo info) 
        {
            lock (object_lock) 
            {
                WoodInfo = info;
            }
        }
    }
}
