using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EndFaceDetection.Common.Converters
{
    public static class IPConverter
    {
        public static string UInt32ToIPAddress(uint ipAsUInt32)
        {
            byte[] bytes = BitConverter.GetBytes(ipAsUInt32);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes);
            }
            return $"{bytes[0]}.{bytes[1]}.{bytes[2]}.{bytes[3]}";
        }

        public static uint IPAddressToUInt32(string ipAddress)
        {
            string[] parts = ipAddress.Split('.');
            if (parts.Length != 4)
            {
                throw new ArgumentException("Invalid IP address format.");
            }
            byte[] bytes = new byte[4];
            for (int i = 0; i < 4; i++)
            {
                bytes[i] = byte.Parse(parts[i]);
            }
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes);
            }
            return BitConverter.ToUInt32(bytes, 0);
        }
    }
}
