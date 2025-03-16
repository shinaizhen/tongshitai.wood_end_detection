using EndFaceDetection.Common.Converters;
using EndFaceDetection.LogModule;
using System.Net;
using System.Net.NetworkInformation;
using System.Windows;

namespace EndFaceDetection.Common
{
    public static class CheckNetworkByPing
    {
        public static bool Check(string ipAddress)
        {
            try
            {
                using (Ping pingSender = new Ping())
                {
                    PingReply reply = pingSender.Send(ipAddress);
                    return reply.Status == IPStatus.Success;
                }
            }
            catch (Exception ex)
            {
                LoggerHelper._.Error("检查IP连接状态发生错误！", ex);
                return false;
            }
        }

        public static bool Check(UInt32 IP)
        {
            try
            {
                using (Ping pingSender = new Ping())
                {
                    string ipAddress=IPConverter.UInt32ToIPAddress(IP);
                    PingReply reply = pingSender.Send(ipAddress);
                    return reply.Status == IPStatus.Success;
                }
            }
            catch(Exception ex) 
            {
                LoggerHelper._.Error("检查IP连接状态发生错误！",ex);
                return false;
            }
        }

    }
}
