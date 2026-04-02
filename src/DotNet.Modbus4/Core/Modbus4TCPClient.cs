using System;
using System.Net.Sockets;
using Modbus.Device;

namespace DotNet.Modbus4
{
    public class Modbus4TCPClient : BasicModbus, ModbusDao
    {
        TcpClient tcpClient;
        ModbusIpMaster master;
        byte slaveAddress;

        /// <summary>
        /// 处理异常并清理资源
        /// </summary>
        /// <param name="ex">异常对象</param>
        /// <param name="message">错误消息</param>
        /// <returns>始终返回false</returns>
        private bool HandleException(Exception ex, string message)
        {
            try
            {
                // 先记录连接状态（在释放资源之前）
                bool isConnected = false;
                try
                {
                    isConnected = tcpClient?.Connected ?? false;
                }
                catch { }

                // 根据异常类型生成详细消息
                string errorMessage = message;
                if (ex is TimeoutException)
                {
                    errorMessage = $"Modbus超时异常: {message}";
                }
                else if (ex is UnauthorizedAccessException)
                {
                    errorMessage = $"端口拒绝访问异常: {message}";
                }
                else if (ex is SocketException socketEx)
                {
                    errorMessage = $"网络异常: {socketEx.Message} - {message}";
                }
                else if (ex is InvalidOperationException)
                {
                    errorMessage = $"操作异常: {ex.Message} - {message}";
                }

                // 记录异常日志
                Modbus4Log.Exception("Modbus4TCPClient", ex, errorMessage);

                // 触发断开事件
                SetDisconnected(isConnected, errorMessage);

                // 释放资源
                this.Dispose();
            }
            catch (Exception hanEx)
            {
                Modbus4Log.Exception("Modbus4RTUClient", hanEx, "异常处理过程中发生错误");
            }

            return false;
        }

        /// <summary>
        /// 判断端口是否占用
        /// </summary>
        /// <param name="port">端口号</param>
        /// <returns></returns>
        public bool IsPortUsed(int port)
        {
            try
            {
                var iproperties = System.Net.NetworkInformation.IPGlobalProperties.GetIPGlobalProperties();
                var ipEndPoints = iproperties.GetActiveTcpListeners();

                foreach (var con in ipEndPoints)
                {
                    if (con.Port == port)
                    {
                        return true;
                    }
                }
                return false;
            }
            catch (Exception ex)
            {
                return HandleException(ex, "判断端口占用异常");
            }
        }

        /// <summary>
        /// 打开连接
        /// </summary>
        /// <param name="ip">IP地址</param>
        /// <param name="port">端口号</param>
        /// <param name="slaveAddress">站号</param>
        /// <returns></returns>
        public bool open_connect(string ip, int port, byte slaveAddress)
        {
            try
            {
                m_mutex.WaitOne();
                //if (IsPortUsed(port)) throw new SlaveException($"TCP端口[{port}]被占用!!!");

                tcpClient = new TcpClient(ip, port);
                master = ModbusIpMaster.CreateIp(tcpClient);
                master.Transport.ReadTimeout = 500;
                master.Transport.WriteTimeout = 500;
                master.Transport.Retries = 3;
                master.Transport.WaitToRetryMilliseconds = 250;
                this.slaveAddress = slaveAddress;

                bool isConnected = tcpClient.Connected;
                SetConnectState(isConnected);
                
                return isConnected;
            }
            catch (Exception ex)
            {
                return HandleException(ex, "连接失败");
            }
            finally
            {
                m_mutex.ReleaseMutex();
            }
        }

        /// <summary>
        /// 关闭连接
        /// </summary>
        /// <returns></returns>
        public bool close_connect()
        {
            try
            {
                m_mutex.WaitOne();
                if (master != null)
                {
                    this.Dispose();
                    SetDisconnected(false);
                }
                
                return true;
            }
            catch (Exception ex)
            {
                return HandleException(ex, "关闭连接失败");
            }
            finally
            {
                m_mutex.ReleaseMutex();
            }
        }

        #region 寄存器M

        /// <summary>
        /// 读寄存器M的值
        /// </summary>
        /// <param name="address">PLC地址</param>
        /// <param name="value">返回值</param>
        /// <returns></returns>
        public bool readM(int address, out bool value)
        {
            value = false;
            try
            {
                m_mutex.WaitOne();
                value = master.ReadCoils(slaveAddress, (ushort)address, 1)[0];
                return true;
            }
            catch (Exception ex)
            {
                return HandleException(ex, "读寄存器M的值失败");
            }
            finally
            {
                m_mutex.ReleaseMutex();
            }
        }

        /// <summary>
        /// 写入寄存器M的值
        /// </summary>
        /// <param name="address">PLC地址</param>
        /// <param name="value">值</param>
        /// <returns></returns>
        public bool writeM(int address, bool value)
        {
            try
            {
                m_mutex.WaitOne();
                master.WriteSingleCoil(slaveAddress, (ushort)address, value);
                return true;
            }
            catch (Exception ex)
            {
                return HandleException(ex, "写入寄存器M的值失败");
            }
            finally
            {
                m_mutex.ReleaseMutex();
            }
        }

        /// <summary>
        /// 批量读取寄存器M的值
        /// </summary>
        /// <param name="startAddress">PLC开始地址</param>
        /// <param name="numberOfPoints">读取数量</param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool batch_readM(int startAddress, int numberOfPoints, out bool[] value)
        {
            value = null;
            try
            {
                m_mutex.WaitOne();
                value = master.ReadCoils(slaveAddress, (ushort)startAddress, (ushort)numberOfPoints);
                return true;
            }
            catch (Exception ex)
            {
                return HandleException(ex, "批量读取寄存器M的值失败");
            }
            finally
            {
                m_mutex.ReleaseMutex();
            }
        }

        /// <summary>
        /// 批量写入寄存器M的值
        /// </summary>
        /// <param name="startAddress">PLC开始地址</param>
        /// <param name="numberOfPoints">写入个数</param>
        /// <param name="value">值</param>
        /// <returns></returns>
        public bool batch_writeM(int startAddress, int numberOfPoints, bool[] value)
        {
            try
            {
                m_mutex.WaitOne();
                if (!setNuberMax(numberOfPoints, value.Length))
                {
                    return false;
                }

                master.WriteMultipleCoils(slaveAddress, (ushort)startAddress, value);
                return true;
            }
            catch (Exception ex)
            {
                return HandleException(ex, "批量写入寄存器M的值失败");
            }
            finally
            {
                m_mutex.ReleaseMutex();
            }
        }

        #endregion
        #region 16位D寄存器

        /// <summary>
        /// 读取寄存器16位D的值
        /// </summary>
        /// <param name="address">PLC地址</param>
        /// <param name="value">返回值</param>
        /// <returns></returns>
        public bool read_ushort16(int address, out ushort value)
        {
            value = 0;
            try
            {
                m_mutex.WaitOne();
                ushort[] inputs = master.ReadHoldingRegisters(slaveAddress, (ushort)address, 1);
                value = inputs[0];
                return true;
            }
            catch (Exception ex)
            {
                return HandleException(ex, "读取寄存器16位D的值失败");
            }
            finally
            {
                m_mutex.ReleaseMutex();
            }
        }

        /// <summary>
        /// 写入寄存器16位D的值
        /// </summary>
        /// <param name="address">PLC地址</param>
        /// <param name="value">值</param>
        /// <returns></returns>
        public bool write_ushort16(int address, ushort value)
        {
            try
            {
                m_mutex.WaitOne();
                master.WriteSingleRegister(slaveAddress, (ushort)address, value);
                return true;
            }
            catch (Exception ex)
            {
                return HandleException(ex, "写入寄存器16位D的值失败");
            }
            finally
            {
                m_mutex.ReleaseMutex();
            }
        }

        /// <summary>
        /// 批量读取寄存器16位D的值
        /// </summary>
        /// <param name="startAddress">PLC开始地址</param>
        /// <param name="numberOfPoints">读取个数</param>
        /// <param name="value">返回值</param>
        /// <returns></returns>
        public bool batch_read_ushort16(int startAddress, int numberOfPoints, out ushort[] value)
        {
            value = null;
            try
            {
                m_mutex.WaitOne();
                value = master.ReadHoldingRegisters(slaveAddress, (ushort)startAddress, (ushort)numberOfPoints);
                return true;
            }
            catch (Exception ex)
            {
                return HandleException(ex, "批量读取寄存器16位D的值失败");
            }
            finally
            {
                m_mutex.ReleaseMutex();
            }
        }

        /// <summary>
        /// 批量写入寄存器16位D的值
        /// </summary>
        /// <param name="startAddress">PLC开始地址</param>
        /// <param name="numberOfPoints">写入个数</param>
        /// <param name="value">值</param>
        /// <returns></returns>
        public bool batch_write_ushort16(int startAddress, int numberOfPoints, ushort[] value)
        {
            try
            {
                m_mutex.WaitOne();
                if (!setNuberMax(numberOfPoints, value.Length))
                {
                    return false;
                }

                master.WriteMultipleRegisters(slaveAddress, (ushort)startAddress, value);
                return true;
            }
            catch (Exception ex)
            {
                return HandleException(ex, "批量写入寄存器16位D的值失败");
            }
            finally
            {
                m_mutex.ReleaseMutex();
            }
        }

        #endregion
        #region 32位寄存器

        /// <summary>
        /// 读取寄存器32位D的值
        /// </summary>
        /// <param name="address">PLC地址</param>
        /// <param name="value">返回值</param>
        /// <returns></returns>
        public bool read_int32(int address, out int value)
        {
            value = 0;
            try
            {
                m_mutex.WaitOne();
                ushort[] inputs = master.ReadHoldingRegisters(1, (ushort)address, 2);
                value = Int16ConvTo32(inputs[0], inputs[1]);
                return true;
            }
            catch (Exception ex)
            {
                return HandleException(ex, "读取寄存器32位D的值失败");
            }
            finally
            {
                m_mutex.ReleaseMutex();
            }
        }

        /// <summary>
        /// 写入寄存器32位D的值
        /// </summary>
        /// <param name="address">PLC地址</param>
        /// <param name="value">值</param>
        /// <returns></returns>
        public bool write_int32(int address, int value)
        {
            try
            {
                m_mutex.WaitOne();
                ushort lowOrder = 0;  //低字
                ushort highOrder = 0; //高字
                Int32ConvTo16(value, ref lowOrder, ref highOrder);
                master.WriteMultipleRegisters(slaveAddress, (ushort)address, new ushort[] { lowOrder, highOrder });
                return true;
            }
            catch (Exception ex)
            {
                return HandleException(ex, "写入寄存器32位D的值失败");
            }
            finally
            {
                m_mutex.ReleaseMutex();
            }
        }

        /// <summary>
        /// 批量读取寄存器32位D的值
        /// </summary>
        /// <param name="startAddress">PLC开始地址</param>
        /// <param name="numberOfPoints">读取个数</param>
        /// <param name="value">返回值</param>
        /// <returns></returns>
        public bool batch_read_int32(int startAddress, int numberOfPoints, out int[] value)
        {
            value = null;
            try
            {
                m_mutex.WaitOne();
                ushort[] inputs = master.ReadHoldingRegisters(1, (ushort)startAddress, (ushort)(numberOfPoints * 2));
                value = new int[numberOfPoints];
                for (int i = 0; i < numberOfPoints; i++)
                {
                    value[i] = Int16ConvTo32(inputs[0 + i * 2], inputs[1 + i * 2]);
                }
                return true;
            }
            catch (Exception ex)
            {
                return HandleException(ex, "批量读取寄存器32位D的值失败");
            }
            finally
            {
                m_mutex.ReleaseMutex();
            }
        }

        /// <summary>
        /// 批量写入寄存器32位D的值
        /// </summary>
        /// <param name="startAddress">PLC开始地址</param>
        /// <param name="numberOfPoints">写入个数</param>
        /// <param name="value">值</param>
        /// <returns></returns>
        public bool batch_write_int32(int startAddress, int numberOfPoints, int[] value)
        {
            try
            {
                m_mutex.WaitOne();
                if (!setNuberMax(numberOfPoints, value.Length))
                {
                    return false;
                }

                ushort[] inputs = new ushort[numberOfPoints * 2];
                for (int i = 0; i < numberOfPoints; i++)
                {
                    Int32ConvTo16(value[i], ref inputs[0 + i * 2], ref inputs[1 + i * 2]);
                }
                master.WriteMultipleRegisters(slaveAddress, (ushort)startAddress, inputs);
                return true;
            }
            catch (Exception ex)
            {
                return HandleException(ex, "批量写入寄存器32位D的值失败");
            }
            finally
            {
                m_mutex.ReleaseMutex();
            }
        }

        #endregion

        #region 32位寄存器 - 浮点数

        /// <summary>
        /// 读取寄存器32位D的值（浮点数）
        /// </summary>
        /// <param name="address">PLC地址</param>
        /// <param name="value">返回值</param>
        /// <returns></returns>
        public bool read_float32(int address, out float value)
        {
            value = 0;
            try
            {
                m_mutex.WaitOne();
                ushort[] inputs = master.ReadHoldingRegisters(1, (ushort)address, 2);
                value = readFloat(inputs[0], inputs[1]);
                return true;
            }
            catch (Exception ex)
            {
                return HandleException(ex, "读取寄存器32位D的值失败");
            }
            finally
            {
                m_mutex.ReleaseMutex();
            }
        }

        /// <summary>
        /// 写入寄存器32位D的值（浮点数）
        /// </summary>
        /// <param name="address">PLC地址</param>
        /// <param name="value">值</param>
        /// <returns></returns>
        public bool write_float32(int address, float value)
        {
            try
            {
                m_mutex.WaitOne();
                ushort lowOrder = 0;  //低字
                ushort highOrder = 0; //高字
                writeFloat(value, out lowOrder, out highOrder);
                master.WriteMultipleRegisters(slaveAddress, (ushort)address, new ushort[] { lowOrder, highOrder });
                return true;
            }
            catch (Exception ex)
            {
                return HandleException(ex, "写入寄存器32位D的值失败");
            }
            finally
            {
                m_mutex.ReleaseMutex();
            }
        }

        /// <summary>
        /// 批量读取寄存器32位D的值（浮点数）
        /// </summary>
        /// <param name="startAddress">PLC开始地址</param>
        /// <param name="numberOfPoints">读取个数</param>
        /// <param name="value">返回值</param>
        /// <returns></returns>
        public bool batch_read_float32(int startAddress, int numberOfPoints, out float[] value)
        {
            value = null;
            try
            {
                m_mutex.WaitOne();
                ushort[] inputs = master.ReadHoldingRegisters(1, (ushort)startAddress, (ushort)(numberOfPoints * 2));
                value = new float[numberOfPoints];
                for (int i = 0; i < numberOfPoints; i++)
                {
                    value[i] = readFloat(inputs[0 + i * 2], inputs[1 + i * 2]);
                }
                return true;
            }
            catch (Exception ex)
            {
                return HandleException(ex, "批量读取寄存器32位D的值失败");
            }
            finally
            {
                m_mutex.ReleaseMutex();
            }
        }

        /// <summary>
        /// 批量写入寄存器32位D的值（浮点数）
        /// </summary>
        /// <param name="startAddress">PLC开始地址</param>
        /// <param name="numberOfPoints">写入个数</param>
        /// <param name="value">值</param>
        /// <returns></returns>
        public bool batch_write_float32(int startAddress, int numberOfPoints, float[] value)
        {
            try
            {
                m_mutex.WaitOne();
                if (!setNuberMax(numberOfPoints, value.Length))
                {
                    return false;
                }

                ushort[] inputs = new ushort[numberOfPoints * 2];
                for (int i = 0; i < numberOfPoints; i++)
                {
                    writeFloat(value[i], out inputs[0 + i * 2], out inputs[1 + i * 2]);
                }
                master.WriteMultipleRegisters(slaveAddress, (ushort)startAddress, inputs);
                return true;
            }
            catch (Exception ex)
            {
                return HandleException(ex, "批量写入寄存器32位D的值失败");
            }
            finally
            {
                m_mutex.ReleaseMutex();
            }
        }

        #endregion

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        ~Modbus4TCPClient()
        {
            Dispose(false);
        }

        /// <summary> 
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                tcpClient?.Close();
                master?.Dispose();
            }
        }
    }
}
