using System;
using static DotNet.Modbus4.BasicModbus;

namespace DotNet.Modbus4
{
    /// <summary>
    /// Modbus通信工具类
    /// 提供便捷的方法创建和管理Modbus客户端和服务器
    /// </summary>
    public sealed class ModbusHelper : IDisposable
    {
        private readonly ModbusDao _modbusDao;
        private bool _disposed;

        /// <summary>
        /// 连接事件
        /// </summary>
        public event ConnectHandler Connected
        {
            add => _modbusDao._Connected += value;
            remove => _modbusDao._Connected -= value;
        }

        /// <summary>
        /// 断开连接事件
        /// </summary>
        public event DisconnectedHandler Disconnected
        {
            add => _modbusDao._Disconnected += value;
            remove => _modbusDao._Disconnected -= value;
        }

        /// <summary>
        /// 私有构造函数，使用静态工厂方法创建实例
        /// </summary>
        private ModbusHelper(ModbusDao modbusDao)
        {
            _modbusDao = modbusDao ?? throw new ArgumentNullException(nameof(modbusDao));
        }

        #region 静态工厂方法

        /// <summary>
        /// 创建RTU客户端
        /// </summary>
        /// <param name="portName">串口名称（如 "COM1"）</param>
        /// <param name="baudRate">波特率（如 9600, 19200）</param>
        /// <param name="parity">校验位（"None", "Odd", "Even"）</param>
        /// <param name="dataBits">数据位（通常为 8）</param>
        /// <param name="stopBits">停止位（通常为 1）</param>
        /// <param name="slaveId">从站地址（1-247）</param>
        /// <returns>ModbusHelper实例</returns>
        /// <exception cref="ArgumentException">参数无效时抛出</exception>
        public static ModbusHelper CreateRtuClient(
            string portName,
            int baudRate,
            string parity,
            int dataBits,
            int stopBits,
            byte slaveId = 1)
        {
            ValidateRtuParameters(portName, baudRate, parity, dataBits, stopBits, slaveId);

            var client = new Modbus4RTUClient();
            client.open_connect(portName, baudRate, parity, dataBits, stopBits, slaveId);
            return new ModbusHelper(client);
        }

        /// <summary>
        /// 创建RTU服务器
        /// </summary>
        /// <param name="portName">串口名称（如 "COM1"）</param>
        /// <param name="baudRate">波特率（如 9600, 19200）</param>
        /// <param name="parity">校验位（"None", "Odd", "Even"）</param>
        /// <param name="dataBits">数据位（通常为 8）</param>
        /// <param name="stopBits">停止位（通常为 1）</param>
        /// <param name="slaveId">从站地址（1-247）</param>
        /// <returns>ModbusHelper实例</returns>
        /// <exception cref="ArgumentException">参数无效时抛出</exception>
        public static ModbusHelper CreateRtuServer(
            string portName,
            int baudRate,
            string parity,
            int dataBits,
            int stopBits,
            byte slaveId = 1)
        {
            ValidateRtuParameters(portName, baudRate, parity, dataBits, stopBits, slaveId);

            var server = new Modbus4RTUServer();
            server.open_connect(portName, baudRate, parity, dataBits, stopBits, slaveId);
            return new ModbusHelper(server);
        }

        /// <summary>
        /// 创建TCP客户端
        /// </summary>
        /// <param name="ip">IP地址（如 "192.168.1.100"）</param>
        /// <param name="port">端口号（通常为 502）</param>
        /// <param name="slaveAddress">从站地址（0-255）</param>
        /// <returns>ModbusHelper实例</returns>
        /// <exception cref="ArgumentException">参数无效时抛出</exception>
        public static ModbusHelper CreateTcpClient(string ip, int port, byte slaveAddress = 1)
        {
            ValidateTcpParameters(ip, port);

            var client = new Modbus4TCPClient();
            client.open_connect(ip, port, slaveAddress);
            return new ModbusHelper(client);
        }

        /// <summary>
        /// 创建TCP服务器
        /// </summary>
        /// <param name="ip">IP地址（如 "192.168.1.100" 或 "0.0.0.0" 监听所有接口）</param>
        /// <param name="port">端口号（通常为 502）</param>
        /// <param name="slaveAddress">从站地址（0-255）</param>
        /// <returns>ModbusHelper实例</returns>
        /// <exception cref="ArgumentException">参数无效时抛出</exception>
        public static ModbusHelper CreateTcpServer(string ip, int port, byte slaveAddress = 1)
        {
            ValidateTcpParameters(ip, port);

            var server = new Modbus4TCPServer();
            server.open_connect(ip, port, slaveAddress);
            return new ModbusHelper(server);
        }

        #endregion

        #region 参数验证

        private static void ValidateRtuParameters(
            string portName,
            int baudRate,
            string parity,
            int dataBits,
            int stopBits,
            byte slaveId)
        {
            if (string.IsNullOrWhiteSpace(portName))
                throw new ArgumentException("串口名称不能为空", nameof(portName));

            if (baudRate <= 0)
                throw new ArgumentException("波特率必须大于0", nameof(baudRate));

            if (string.IsNullOrWhiteSpace(parity))
                throw new ArgumentException("校验位不能为空", nameof(parity));

            if (dataBits < 5 || dataBits > 8)
                throw new ArgumentException("数据位必须在5-8之间", nameof(dataBits));

            if (stopBits < 1 || stopBits > 2)
                throw new ArgumentException("停止位必须为1或2", nameof(stopBits));

            if (slaveId == 0 || slaveId > 247)
                throw new ArgumentException("从站地址必须在1-247之间", nameof(slaveId));
        }

        private static void ValidateTcpParameters(string ip, int port)
        {
            if (string.IsNullOrWhiteSpace(ip))
                throw new ArgumentException("IP地址不能为空", nameof(ip));

            if (port < 1 || port > 65535)
                throw new ArgumentException("端口号必须在1-65535之间", nameof(port));
        }

        #endregion

        #region 连接管理

        /// <summary>
        /// 关闭连接
        /// </summary>
        /// <returns>成功返回true，失败返回false</returns>
        public bool CloseConnect()
        {
            ThrowIfDisposed();
            return _modbusDao.close_connect();
        }

        #endregion

        #region 读写M寄存器（布尔值）

        /// <summary>
        /// 读取单个M寄存器的值
        /// </summary>
        /// <param name="address">寄存器地址</param>
        /// <param name="value">读取到的布尔值</param>
        /// <returns>成功返回true，失败返回false</returns>
        public bool ReadCoil(int address, out bool value)
        {
            ThrowIfDisposed();
            return _modbusDao.readM(address, out value);
        }

        /// <summary>
        /// 写入单个M寄存器的值
        /// </summary>
        /// <param name="address">寄存器地址</param>
        /// <param name="value">要写入的布尔值</param>
        /// <returns>成功返回true，失败返回false</returns>
        public bool WriteCoil(int address, bool value)
        {
            ThrowIfDisposed();
            return _modbusDao.writeM(address, value);
        }

        /// <summary>
        /// 批量读取M寄存器的值
        /// </summary>
        /// <param name="startAddress">起始地址</param>
        /// <param name="numberOfPoints">读取数量</param>
        /// <param name="values">读取到的布尔值数组</param>
        /// <returns>成功返回true，失败返回false</returns>
        public bool ReadCoils(int startAddress, int numberOfPoints, out bool[] values)
        {
            ThrowIfDisposed();
            return _modbusDao.batch_readM(startAddress, numberOfPoints, out values);
        }

        /// <summary>
        /// 批量写入M寄存器的值
        /// </summary>
        /// <param name="startAddress">起始地址</param>
        /// <param name="values">要写入的布尔值数组</param>
        /// <returns>成功返回true，失败返回false</returns>
        public bool WriteCoils(int startAddress, bool[] values)
        {
            ThrowIfDisposed();
            if (values == null)
                throw new ArgumentNullException(nameof(values));

            return _modbusDao.batch_writeM(startAddress, values.Length, values);
        }

        #endregion

        #region 读写16位保持寄存器

        /// <summary>
        /// 读取单个16位无符号整数寄存器
        /// </summary>
        /// <param name="address">寄存器地址</param>
        /// <param name="value">读取到的16位无符号整数</param>
        /// <returns>成功返回true，失败返回false</returns>
        public bool ReadHoldingRegister(int address, out ushort value)
        {
            ThrowIfDisposed();
            return _modbusDao.read_ushort16(address, out value);
        }

        /// <summary>
        /// 写入单个16位无符号整数寄存器
        /// </summary>
        /// <param name="address">寄存器地址</param>
        /// <param name="value">要写入的16位无符号整数</param>
        /// <returns>成功返回true，失败返回false</returns>
        public bool WriteHoldingRegister(int address, ushort value)
        {
            ThrowIfDisposed();
            return _modbusDao.write_ushort16(address, value);
        }

        /// <summary>
        /// 批量读取16位无符号整数寄存器
        /// </summary>
        /// <param name="startAddress">起始地址</param>
        /// <param name="numberOfPoints">读取数量</param>
        /// <param name="values">读取到的16位无符号整数数组</param>
        /// <returns>成功返回true，失败返回false</returns>
        public bool ReadHoldingRegisters(int startAddress, int numberOfPoints, out ushort[] values)
        {
            ThrowIfDisposed();
            return _modbusDao.batch_read_ushort16(startAddress, numberOfPoints, out values);
        }

        /// <summary>
        /// 批量写入16位无符号整数寄存器
        /// </summary>
        /// <param name="startAddress">起始地址</param>
        /// <param name="values">要写入的16位无符号整数数组</param>
        /// <returns>成功返回true，失败返回false</returns>
        public bool WriteHoldingRegisters(int startAddress, ushort[] values)
        {
            ThrowIfDisposed();
            if (values == null)
                throw new ArgumentNullException(nameof(values));

            return _modbusDao.batch_write_ushort16(startAddress, values.Length, values);
        }

        #endregion

        #region 读写32位整数

        /// <summary>
        /// 读取32位有符号整数（占用2个连续寄存器）
        /// </summary>
        /// <param name="address">起始寄存器地址</param>
        /// <param name="value">读取到的32位有符号整数</param>
        /// <returns>成功返回true，失败返回false</returns>
        public bool ReadInt32(int address, out int value)
        {
            ThrowIfDisposed();
            return _modbusDao.read_int32(address, out value);
        }

        /// <summary>
        /// 写入32位有符号整数（占用2个连续寄存器）
        /// </summary>
        /// <param name="address">起始寄存器地址</param>
        /// <param name="value">要写入的32位有符号整数</param>
        /// <returns>成功返回true，失败返回false</returns>
        public bool WriteInt32(int address, int value)
        {
            ThrowIfDisposed();
            return _modbusDao.write_int32(address, value);
        }

        /// <summary>
        /// 批量读取32位有符号整数
        /// </summary>
        /// <param name="startAddress">起始地址</param>
        /// <param name="numberOfPoints">读取数量</param>
        /// <param name="values">读取到的32位有符号整数数组</param>
        /// <returns>成功返回true，失败返回false</returns>
        public bool ReadInt32Array(int startAddress, int numberOfPoints, out int[] values)
        {
            ThrowIfDisposed();
            return _modbusDao.batch_read_int32(startAddress, numberOfPoints, out values);
        }

        /// <summary>
        /// 批量写入32位有符号整数
        /// </summary>
        /// <param name="startAddress">起始地址</param>
        /// <param name="values">要写入的32位有符号整数数组</param>
        /// <returns>成功返回true，失败返回false</returns>
        public bool WriteInt32Array(int startAddress, int[] values)
        {
            ThrowIfDisposed();
            if (values == null)
                throw new ArgumentNullException(nameof(values));

            return _modbusDao.batch_write_int32(startAddress, values.Length, values);
        }

        #endregion

        #region 读写32位浮点数

        /// <summary>
        /// 读取32位单精度浮点数（占用2个连续寄存器）
        /// </summary>
        /// <param name="address">起始寄存器地址</param>
        /// <param name="value">读取到的32位浮点数</param>
        /// <returns>成功返回true，失败返回false</returns>
        public bool ReadFloat(int address, out float value)
        {
            ThrowIfDisposed();
            return _modbusDao.read_float32(address, out value);
        }

        /// <summary>
        /// 写入32位单精度浮点数（占用2个连续寄存器）
        /// </summary>
        /// <param name="address">起始寄存器地址</param>
        /// <param name="value">要写入的32位浮点数</param>
        /// <returns>成功返回true，失败返回false</returns>
        public bool WriteFloat(int address, float value)
        {
            ThrowIfDisposed();
            return _modbusDao.write_float32(address, value);
        }

        /// <summary>
        /// 批量读取32位单精度浮点数
        /// </summary>
        /// <param name="startAddress">起始地址</param>
        /// <param name="numberOfPoints">读取数量</param>
        /// <param name="values">读取到的32位浮点数数组</param>
        /// <returns>成功返回true，失败返回false</returns>
        public bool ReadFloatArray(int startAddress, int numberOfPoints, out float[] values)
        {
            ThrowIfDisposed();
            return _modbusDao.batch_read_float32(startAddress, numberOfPoints, out values);
        }

        /// <summary>
        /// 批量写入32位单精度浮点数
        /// </summary>
        /// <param name="startAddress">起始地址</param>
        /// <param name="values">要写入的32位浮点数数组</param>
        /// <returns>成功返回true，失败返回false</returns>
        public bool WriteFloatArray(int startAddress, float[] values)
        {
            ThrowIfDisposed();
            if (values == null)
                throw new ArgumentNullException(nameof(values));

            return _modbusDao.batch_write_float32(startAddress, values.Length, values);
        }

        #endregion

        #region IDisposable实现

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        /// <param name="disposing">是否释放托管资源</param>
        private void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                _modbusDao?.Dispose();
            }

            _disposed = true;
        }

        /// <summary>
        /// 检查对象是否已释放
        /// </summary>
        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(ModbusHelper));
        }

        #endregion
    }
}
