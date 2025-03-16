

namespace EndFaceDetection.Services.Events
{
    public class PLCDataUpdatedEvents : EventArgs
    {
        public PLCDeviceStatus? DeviceStatus { get; set; }
    }
    public class PLCDeviceStatus
    {
        public Motor_Status MotorStatus { get; set; }//电机状态
        public Alarm_Status AlarmStatus { get; set; }//警报器状态
        public Wood_Status WoodStatus { get; set; }//等待木板状态
        public bool WoodSwitch { get; set; }//是否切换画面
    }

    public enum Motor_Status
    {
        Running,   // 电机正在运行
        Waiting,   // 电机处于等待状态
        Error    // 电机错误状态 
    }

    public enum Alarm_Status
    {
        Running,    // 报警器正在工作
        Waiting,    // 报警器等待工作
        Error,       //  系统异常挂起
        Unknow
    }

    public enum Wood_Status
    {
        Waiting,    // 等待木板到来
        Arrived,    // 木板到达指定位置
        Unkown      // 未知状态
    }
}
