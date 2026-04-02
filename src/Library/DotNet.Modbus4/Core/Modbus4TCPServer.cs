using Modbus.Data;
using Modbus.Device;
using System;
using System.Net;
using System.Net.Sockets;

namespace DotNet.Modbus4
{
    public class Modbus4TCPServer : BasicModbus, ModbusDao
    {
        private ModbusSlave slave;
        byte slaveId = 1;

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
                Modbus4Log.Exception("Modbus4TCPServer", ex, errorMessage);

                // 触发断开事件
                SetDisconnected(false, errorMessage);

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
                if (IsPortUsed(port))
                {
                    Modbus4Log.Warning("Modbus4TCPServer", $"TCP端口[{port}]被占用");
                }

                IPAddress address = IPAddress.Parse(ip);
                // create and start the TCP slave
                TcpListener slaveTcpListener = new TcpListener(address, port);
                slaveTcpListener.Start();

                slave = ModbusTcpSlave.CreateTcp(slaveId, slaveTcpListener);
                slave.DataStore = DataStoreFactory.CreateDefaultDataStore();
                //slave.Transport.ReadTimeout = 500;
                //slave.Transport.WriteTimeout = 500;
                //slave.Transport.Retries = 3;
                //slave.Transport.WaitToRetryMilliseconds = 250;

                slave.ListenAsync().GetAwaiter();
                SetConnectState(true);
                
                return true;
            }
            catch(Exception ex)
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
                if (slave != null)
                {
                    this.Dispose();
                    SetDisconnected(false);
                }
                
                return true;
            }
            catch(Exception ex)
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
                value = slave.DataStore.CoilDiscretes[address + 1];
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
                slave.DataStore.CoilDiscretes[address + 1] = value;
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
                value = new bool[numberOfPoints];
                for (int i = 0; i < numberOfPoints; i++)
                {
                    value[i] = slave.DataStore.CoilDiscretes[i + startAddress + 1];
                }
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

                for (int i = 0; i < numberOfPoints; i++)
                {
                    slave.DataStore.CoilDiscretes[i + startAddress + 1] = value[i];
                }
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
                value = slave.DataStore.HoldingRegisters[address + 1];
                return true;
            }
            catch(Exception ex)
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
                slave.DataStore.HoldingRegisters[address + 1] = value;
                return true;
            }
            catch(Exception ex)
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
                ModbusDataCollection<ushort> holdingRegisters = slave.DataStore.HoldingRegisters;
                value = new ushort[numberOfPoints];
                for (int i = 0; i < numberOfPoints; i++)
                {
                    value[i] = holdingRegisters[i + startAddress + 1];
                }
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

                ModbusDataCollection<ushort> holdingRegisters = slave.DataStore.HoldingRegisters;
                for (int i = 0; i < numberOfPoints; i++)
                {
                    holdingRegisters[i + startAddress + 1] = value[i];
                }
                return true;
            }
            catch(Exception ex)
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
                ushort[] values = new ushort[2];
                values[0] = slave.DataStore.HoldingRegisters[0 + address + 1];
                values[1] = slave.DataStore.HoldingRegisters[1 + address + 1];
                value = Int16ConvTo32(values[0], values[1]);
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
                slave.DataStore.HoldingRegisters[0 + address + 1] = lowOrder;
                slave.DataStore.HoldingRegisters[1 + address + 1] = highOrder;
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
                ModbusDataCollection<ushort> holdingRegisters = slave.DataStore.HoldingRegisters;
                
                value = new int[numberOfPoints];
                for (int i = 0; i < numberOfPoints; i++)
                {
                    value[i] = Int16ConvTo32(holdingRegisters[0 + i * 2 + startAddress+1], holdingRegisters[1 + i * 2 + startAddress+1]);
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

                ushort lowOrder = 0;  //低字
                ushort highOrder = 0; //高字
                for (int i = 0; i < numberOfPoints; i++)
                {
                    Int32ConvTo16(value[i], ref lowOrder, ref highOrder);
                    slave.DataStore.HoldingRegisters[0 + i * 2 + startAddress + 1] = lowOrder;
                    slave.DataStore.HoldingRegisters[1 + i * 2 + startAddress + 1] = highOrder;
                }
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
                ushort[] values = new ushort[2];
                values[0] = slave.DataStore.HoldingRegisters[0 + address + 1];
                values[1] = slave.DataStore.HoldingRegisters[1 + address + 1];
                value = readFloat(values[0], values[1]);
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
                slave.DataStore.HoldingRegisters[0 + address + 1] = lowOrder;
                slave.DataStore.HoldingRegisters[1 + address + 1] = highOrder;
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
                ModbusDataCollection<ushort> holdingRegisters = slave.DataStore.HoldingRegisters;
                value = new float[numberOfPoints];
                for (int i = 0; i < numberOfPoints; i++)
                {
                    value[i] = readFloat(holdingRegisters[0 + i * 2 + startAddress + 1], holdingRegisters[1 + i * 2 + startAddress + 1]);
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

                ushort lowOrder = 0;  //低字
                ushort highOrder = 0; //高字
                for (int i = 0; i < numberOfPoints; i++)
                {
                    writeFloat(value[i], out lowOrder, out highOrder);
                    slave.DataStore.HoldingRegisters[0 + i * 2 + startAddress + 1] = lowOrder;
                    slave.DataStore.HoldingRegisters[1 + i * 2 + startAddress + 1] = highOrder;
                }
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
        ~Modbus4TCPServer()
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
                slave?.Dispose();
            }
        }
    }
}
