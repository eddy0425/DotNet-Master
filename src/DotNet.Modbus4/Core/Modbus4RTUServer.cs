using System;
using System.IO.Ports;
using Modbus.Data;
using Modbus.Device;
using Modbus.Serial;

namespace DotNet.Modbus4
{
    public class Modbus4RTUServer : BasicModbus, ModbusDao
    {
        ModbusSlave slave;
        SerialPort port;
        byte slaveAddress = 1;

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
                    isConnected = port?.IsOpen ?? false;
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
                else if (ex is InvalidOperationException)
                {
                    errorMessage = $"操作异常: {ex.Message} - {message}";
                }

                // 记录异常日志
                Modbus4Log.Exception("Modbus4RTUServer", ex, errorMessage);

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
        ///  1、获取当前计算机上可用的端口列表【只读】
        /// </summary>
        /// <returns></returns>
        public string[] get_port_names()
        {
            return SerialPort.GetPortNames();
        }

        /// <summary>
        /// 打开端口
        /// </summary>
        /// <param name="portName">端口号</param>
        /// <param name="baudRate">波特率默认9600</param>
        /// <param name="parity">校验位默认None</param>
        /// <param name="dataBits">数据位默认8位</param>
        /// <param name="stopBits">停止位默认1</param>
        /// <param name="slaveId">站号</param>
        /// <returns></returns>
        public bool open_connect(string portName, int baudRate, string parity, int dataBits, int stopBits, byte slaveId=1)
        {
            try
            {
                m_mutex.WaitOne();
                port = new SerialPort(portName);
                slaveAddress = slaveId;
                port.BaudRate = baudRate;                              //波特率
                if (parity == "NONE") port.Parity = Parity.None;       //校验位
                else if (parity == "0DD") port.Parity = Parity.Odd;    //校验位
                else if (parity == "EVEN") port.Parity = Parity.Even;  //校验位
                port.DataBits = dataBits;                              //数据位
                if (stopBits == 1) port.StopBits = StopBits.One;       //停止位
                else if (stopBits == 2) port.StopBits = StopBits.Two;  //停止位
                port.Open();

                var adapter = new SerialPortAdapter(port);
                // create modbus slave
                slave = ModbusSerialSlave.CreateRtu(slaveAddress, adapter);
                slave.DataStore = DataStoreFactory.CreateDefaultDataStore();

                slave.ListenAsync().GetAwaiter();

                bool isOpen = port.IsOpen;
                SetConnectState(isOpen);
                
                return isOpen;
            }
            catch (Exception ex)
            {
                return HandleException(ex, "打开端口失败");
            }
            finally
            {
                m_mutex.ReleaseMutex();
            }
        }

        /// <summary>
        /// 4、关闭连接
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
                slave.DataStore.HoldingRegisters[address + 1] = value;
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
                    value[i] = Int16ConvTo32(holdingRegisters[0 + i * 2 + startAddress + 1], holdingRegisters[1 + i * 2 + startAddress + 1]);
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
        ~Modbus4RTUServer()
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
                port?.Dispose();
            }
        }
    }
}
