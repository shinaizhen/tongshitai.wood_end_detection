using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EndFaceDetection.TestConsoleDemo
{
    public class WoodInfoV1
    {
        public string? ORCode{ get; set; }
        public string? CameraName{ get; set; }
        public ushort Position1 {  get; set; }
        public ushort Position2 {  get; set; }
        public ushort Position3 {  get; set; }
        public ushort Position4 {  get; set; }
        public ushort Position5 {  get; set; }
        public ushort Position6 {  get; set; }
        public WoodInfo Copy()
        {
            return new WoodInfo { ORCode = this.ORCode, CameraName = this.CameraName, Positon1 = this.Position1, Positon2 = this.Position2, Positon3 = this.Position3 };
        }
    }

    public class WoodInfo
    {
        public string ORCode { get; set; } = "AA08K";    //  存储二维码信息
        public string CameraName { get; set; } = "C1";     //  存储需要拍照相机名称（相机需要切换）
        public ushort Positon1 { get; set; }
        public ushort Positon2 { get; set; }
        public ushort Positon3 { get; set; }
    }
}
