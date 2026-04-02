using System;
using static DotNet.Modbus4.BasicModbus;

namespace DotNet.Modbus4
{
    /// <summary>
    /// modbus通讯接口
    /// </summary>
    public interface ModbusDao : IDisposable
    {
        event ConnectHandler _Connected;

        event DisconnectedHandler _Disconnected;

        /// <summary>
        /// 关闭连接
        /// </summary>
        /// <returns></returns>
        bool close_connect();

        /// <summary>
        /// 读取M的值
        /// </summary>
        /// <param name="address">PLC地址</param>
        /// <param name="value">返回值</param>
        /// <returns></returns>
        bool readM(int address, out bool value);

        /// <summary>
        /// 写入M的值
        /// </summary>
        /// <param name="address">PLC地址</param>
        /// <param name="value">值</param>
        /// <returns></returns>
        bool writeM(int address, bool value);

        /// <summary>
        /// 批量读取M的值
        /// </summary>
        /// <param name="startAddress">PLC开始地址</param>
        /// <param name="numberOfPoints">读取个数</param>
        /// <param name="value">返回值</param>
        /// <returns></returns>
        bool batch_readM(int startAddress, int numberOfPoints, out bool[] value);

        /// <summary>
        /// 批量写入M的值
        /// </summary>
        /// <param name="startAddress">PLC开始地址</param>
        /// <param name="numberOfPoints">写入个数</param>
        /// <param name="value">值</param>
        /// <returns></returns>
        bool batch_writeM(int startAddress, int numberOfPoints, bool[] value);


        /// <summary>
        /// 读取单个16位D的值
        /// </summary>
        /// <param name="address">PLC地址</param>
        /// <param name="value">返回值</param>
        /// <returns></returns>
        bool read_ushort16(int address, out ushort value);

        /// <summary>
        /// 写入单个16位D的值
        /// </summary>
        /// <param name="address">PLC地址</param>
        /// <param name="value">值</param>
        /// <returns></returns>
        bool write_ushort16(int address, ushort value);

        /// <summary>
        /// 批量读取单个16位D的值
        /// </summary>
        /// <param name="startAddress">PLC开始地址</param>
        /// <param name="numberOfPoints">读取个数</param>
        /// <param name="value">返回值</param>
        /// <returns></returns>
        bool batch_read_ushort16(int startAddress, int numberOfPoints, out ushort[] value);

        /// <summary>
        /// 批量写入单个16位D的值
        /// </summary>
        /// <param name="startAddress">PLC开始个数</param>
        /// <param name="numberOfPoints">写入个数</param>
        /// <param name="value">值</param>
        /// <returns></returns>
        bool batch_write_ushort16(int startAddress, int numberOfPoints, ushort[] value);


        /// <summary>
        /// 获取32位D的值
        /// </summary>
        /// <param name="address">PLC地址</param>
        /// <param name="value">返回值</param>
        /// <returns></returns>
        bool read_int32(int address, out int value);

        /// <summary>
        /// 写入32位D的值
        /// </summary>
        /// <param name="address">PLC地址</param>
        /// <param name="value">值</param>
        /// <returns></returns>
        bool write_int32(int address, int value);

        /// <summary>
        /// 批量读取32位D的值
        /// </summary>
        /// <param name="startAddress">PLC开始地址</param>
        /// <param name="numberOfPoints">读取格式</param>
        /// <param name="value">返回值</param>
        /// <returns></returns>
        bool batch_read_int32(int startAddress, int numberOfPoints, out int[] value);

        /// <summary>
        /// 批量写入32位D的值
        /// </summary>
        /// <param name="startAddress">PLC开始地址</param>
        /// <param name="numberOfPoints">写入个数</param>
        /// <param name="value">值</param>
        /// <returns></returns>
        bool batch_write_int32(int startAddress, int numberOfPoints, int[] value);


        /// <summary>
        /// 获取32位D的值 - 浮点数
        /// </summary>
        /// <param name="address">PLC地址</param>
        /// <param name="value">返回值</param>
        /// <returns></returns>
        bool read_float32(int address, out float value);

        /// <summary>
        /// 写入32位D的值 - 浮点数
        /// </summary>
        /// <param name="address">PLC地址</param>
        /// <param name="value">值</param>
        /// <returns></returns>
        bool write_float32(int address, float value);

        /// <summary>
        /// 批量读取32位D的值 - 浮点数
        /// </summary>
        /// <param name="startAddress">PLC开始地址</param>
        /// <param name="numberOfPoints">读取格式</param>
        /// <param name="value">返回值</param>
        /// <returns></returns>
        bool batch_read_float32(int startAddress, int numberOfPoints, out float[] value);

        /// <summary>
        /// 批量写入32位D的值 - 浮点数
        /// </summary>
        /// <param name="startAddress">PLC开始地址</param>
        /// <param name="numberOfPoints">写入个数</param>
        /// <param name="value">值</param>
        /// <returns></returns>
        bool batch_write_float32(int startAddress, int numberOfPoints, float[] value);

    }
}
