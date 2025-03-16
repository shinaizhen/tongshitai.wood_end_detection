using EndFaceDetection.LogModule;
using NModbus;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace EndFaceDetection.Services.Common
{
    public class PLCTcpHelper
    {
        IModbusMaster? master;
        TcpClient? tcpClient;

        public PLCTcpHelper(string ip, int port)
        {
            CurrentIP = ip;
            CurrentPort = port;
        }

        public string CurrentIP;
        public int CurrentPort;

        public static string PrintErroMSG(Exception exception)
        {
            string str = exception.Message;
            string msg = "";
            int FunctionCode;
            string ExceptionCode;

            str = str.Remove(0, str.IndexOf("\r\n") + 17);
            FunctionCode = Int16.Parse(str.Remove(str.IndexOf("\r\n")));
            msg += ("Function Code: " + FunctionCode.ToString("X"));

            str = str.Remove(0, str.IndexOf("\r\n") + 17);
            ExceptionCode = str.Remove(str.IndexOf("-"));
            switch (ExceptionCode.Trim())
            {
                case "1":
                    msg += ("Exception Code: " + ExceptionCode.Trim() + "----> Illegal function!");
                    break;
                case "2":
                    msg += ("Exception Code: " + ExceptionCode.Trim() + "----> Illegal data address!");
                    break;
                case "3":
                    msg += ("Exception Code: " + ExceptionCode.Trim() + "----> Illegal data value!");
                    break;
                case "4":
                    msg += ("Exception Code: " + ExceptionCode.Trim() + "----> Slave device failure!");
                    break;
            }
            /*
               //Modbus exception codes definition

               * Code   * Name                                      * Meaning
                 01       ILLEGAL FUNCTION                            The function code received in the query is not an allowable action for the server.

                 02       ILLEGAL DATA ADDRESS                        The data addrdss received in the query is not an allowable address for the server.

                 03       ILLEGAL DATA VALUE                          A value contained in the query data field is not an allowable value for the server.

                 04       SLAVE DEVICE FAILURE                        An unrecoverable error occurred while the server attempting to perform the requested action.

                 05       ACKNOWLEDGE                                 This response is returned to prevent a timeout error from occurring in the client (or master)
                                                                      when the server (or slave) needs a long duration of time to process accepted request.

                 06       SLAVE DEVICE BUSY                           The server (or slave) is engaged in processing a longduration program command , and the 
                                                                      client (or master) should retransmit the message later when the server (or slave) is free.

                 08       MEMORY PARITY ERROR                         The server (or slave) attempted to read record file, but detected a parity error in the memory.

                 0A       GATEWAY PATH UNAVAILABLE                    The gateway is misconfigured or overloaded.

                 0B       GATEWAY TARGET DEVICE FAILED TO RESPOND     No response was obtained from the target device. Usually means that the device is not present on the network.

             */
            return msg;
        }

        object lockObject = new object();

        /// <summary>
        /// PLCTCP连接
        /// </summary>
        /// <param name="ip">IP地址</param>
        /// <param name="port">端口号|默认：500</param>
        public bool PLCTCPIPConnect()
        {
            lock (lockObject)
            {
                try
                {
                    master?.Dispose();
                    tcpClient?.Dispose();
                    tcpClient = new TcpClient();
                    tcpClient.Connect(IPAddress.Parse(CurrentIP), CurrentPort);
                    var factory = new ModbusFactory();
                    master = factory.CreateMaster(tcpClient);
                    master.Transport.Retries = 0;   //don't have to do retries||不需要重试
                    master.Transport.ReadTimeout = 3000;
                    return true;
                }
                catch (Exception ex)
                {
                    LoggerHelper._.Error("modbus连接时发生错误：", ex);
                    return false;
                }
            }
        }

        /// <summary>
        /// plc断开连接
        /// </summary>
        public void DisConnect()
        {
            lock (lockObject)
            {
                tcpClient?.Close();
                master?.Dispose();
            }
        }

        /// <summary>
        /// 读取holdingregisters
        /// </summary>
        /// <param name="startAdress">开始地址</param>
        /// <param name="length">读取长度|default：1VW</param>
        /// <param name="slaveAdress">从站地址|default：1</param>
        /// <returns></returns>
        public ushort[]? ReadHoldingRegisters(ushort startAdress, ushort length = 1, byte slaveAdress = 1)
        {
            try
            {
                return master?.ReadHoldingRegisters(slaveAdress, startAdress, length);
            }
            catch (TimeoutException ex)
            {
                LoggerHelper._.Error("modbus_master读写操作超时异常: " + ex.Message);
                return null;
            }
            catch (IOException ex)
            {
                LoggerHelper._.Error("modbus_master输入输出异常: " + ex.Message);
                return null;
            }
            catch (Exception ex)
            {
                LoggerHelper._.Error("modbus_master读写时发生异常: " + ex.Message);
                return null;
            }
        }

        /// <summary>
        /// 读取线圈
        /// </summary>
        /// <param name="startAdress"></param>
        /// <param name="length"></param>
        /// <param name="slaveAdress">default：1</param>
        /// <returns></returns>
        public bool[]? ReadCoils(ushort startAdress, ushort length, byte slaveAdress = 1)
        {
            try
            {
                return master?.ReadCoils(slaveAdress, startAdress, length);
            }
            catch (TimeoutException ex)
            {
                LoggerHelper._.Error("modbus_master读写操作超时异常: " + ex.Message);
                return null;
            }
            catch (IOException ex)
            {
                LoggerHelper._.Error("modbus_master输入输出异常: " + ex.Message);
                return null;
            }
            catch (Exception ex)
            {
                LoggerHelper._.Error("modbus_master读写时发生异常: " + ex.Message);
                return null;
            }
        }

        /// <summary>
        /// 写入线圈
        /// </summary>
        /// <param name="coilAddress"></param>
        /// <param name="value"></param>
        /// <param name="slaveAdress">default：1</param>
        /// <returns></returns>
        public bool WriteSingleCoil(ushort coilAddress, bool value, byte slaveAdress = 1)
        {
            try
            {
                if (master == null)
                    return false;
                master.WriteSingleCoil(slaveAdress, coilAddress, value);
                return true;
            }
            catch (TimeoutException ex)
            {
                LoggerHelper._.Error("modbus_master读写操作超时异常: " + ex.Message);
                return false;
            }
            catch (IOException ex)
            {
                LoggerHelper._.Error("modbus_master输入输出异常: " + ex.Message);
                return false;
            }
            catch (Exception ex)
            {
                LoggerHelper._.Error("modbus_master读写时发生异常: " + ex.Message);
                return false;
            }

        }

        /// <summary>
        /// 读取多个线圈
        /// </summary>
        /// <param name="startAddress"></param>
        /// <param name="data"></param>
        /// <param name="slaveAdress"></param>
        /// <returns></returns>
        public bool WriteCoils(ushort startAddress, bool[] data, byte slaveAdress = 1)
        {
            try
            {
                if (master == null)
                    return false;
                master.WriteMultipleCoils(slaveAdress, startAddress, data);
                return true;
            }
            catch (TimeoutException ex)
            {
                LoggerHelper._.Error("modbus_master读写操作超时异常: " + ex.Message);
                return false;
            }
            catch (IOException ex)
            {
                LoggerHelper._.Error("modbus_master输入输出异常: " + ex.Message);
                return false;
            }
            catch (Exception ex)
            {
                LoggerHelper._.Error("modbus_master读写时发生异常: " + ex.Message);
                return false;
            }
        }

        /// <summary>
        /// 写入单个保持寄存器的值
        /// </summary>
        /// <param name="registerAddress"></param>
        /// <param name="value"></param>
        /// <param name="slaveAdress"></param>
        /// <returns></returns>
        public bool WriteSingleHoldingRegiset(ushort registerAddress, ushort value, byte slaveAdress = 1)
        {
            try
            {
                if (master == null)
                    return false;
                master.WriteSingleRegister(slaveAdress, registerAddress, value);
                return true;
            }
            catch (TimeoutException ex)
            {
                LoggerHelper._.Error("modbus_master读写操作超时异常: " + ex.Message);
                return false;
            }
            catch (IOException ex)
            {
                LoggerHelper._.Error("modbus_master输入输出异常: " + ex.Message);
                return false;
            }
            catch (Exception ex)
            {
                LoggerHelper._.Error("modbus_master读写时发生异常: " + ex.Message);
                return false;
            }
        }

        /// <summary>
        /// 写入多个寄存器的值
        /// </summary>
        /// <param name="registerAddress"></param>
        /// <param name="value"></param>
        /// <param name="slaveAdress"></param>
        /// <returns></returns>
        public bool WriteMultipleRegisters(ushort registerAddress, ushort[] data, byte slaveAdress = 1)
        {
            try
            {
                if (master == null)
                    return false;
                master.WriteMultipleRegisters(slaveAdress, registerAddress, data);
                return true;
            }
            catch (TimeoutException ex)
            {
                LoggerHelper._.Error("modbus_master读写操作超时异常: " + ex.Message);
                return false;
            }
            catch (IOException ex)
            {
                LoggerHelper._.Error("modbus_master输入输出异常: " + ex.Message);
                return false;
            }
            catch (Exception ex)
            {
                LoggerHelper._.Error("modbus_master读写时发生异常: " + ex.Message);
                return false;
            }
        }
    }
}
