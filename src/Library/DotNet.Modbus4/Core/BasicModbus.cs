using System;
using System.Linq;
using System.Threading;

namespace DotNet.Modbus4
{
    /// <summary>
    /// 进制转换类
    /// </summary>
    public class BasicModbus
    {
        protected Mutex m_mutex = new Mutex();          //锁，保证多线程安全

        #region ConnectHandler
        public class ConnectArgs : EventArgs
        {
            public ConnectArgs(bool _isConnect, string _message)
            {
                IsConnect = _isConnect;
                Message = _message;
            }
            public bool IsConnect { get; set; }
            public string Message { get; set; }
        }
        public delegate void ConnectHandler(ConnectArgs e);

        public event ConnectHandler _Connected;
        public void SetConnectState(bool _isConnect, string _message = "")
        {
            if (_Connected != null)
            {
                var e = new ConnectArgs(_isConnect, _message);
                _Connected.Invoke(e);
            }
        }

        #endregion

        #region DisconnectedHandler

        public class DisconnectedArgs : EventArgs
        {
            public DisconnectedArgs(bool _isConnect, string _message)
            {
                IsConnect = _isConnect;
                Message = _message;
            }
            public bool IsConnect { get; set; }
            public string Message { get; set; }
        }
        public delegate void DisconnectedHandler(DisconnectedArgs e);

        public event DisconnectedHandler _Disconnected;
        public void SetDisconnected(bool _isConnect, string _message = "")
        {
            if (_Disconnected != null)
            {
                var e = new DisconnectedArgs(_isConnect, _message);
                _Disconnected.Invoke(e);
            }
        }

        #endregion


        /// <summary>
        /// 16位转32位
        /// </summary>
        /// <param name="low16Bit">16位低位</param>
        /// <param name="high16Bit">16位高位</param>
        /// <param name="Int32Bit">输出32进制值</param>
        protected Int32 Int16ConvTo32(ushort low16Bit, ushort high16Bit)
        {
            Int32 Int32Bit = 0;
            Int32Bit |= (high16Bit & 0x0000FFFF);
            return (Int32Bit << 16) | (low16Bit & 0x0000FFFF);
        }

        /// <summary>
        /// 32位转16位
        /// </summary>
        /// <param name="Int32Bit">输入32进制值</param>
        /// <param name="low16Bit">16位低位</param>
        /// <param name="high16Bit">16位高位</param>
        protected void Int32ConvTo16(Int32 Int32Bit, ref ushort low16Bit, ref ushort high16Bit)
        {
            low16Bit = BitConverter.ToUInt16(BitConverter.GetBytes(Int32Bit), 0);
            high16Bit = BitConverter.ToUInt16(BitConverter.GetBytes(Int32Bit), 2);
        }

        /// https://blog.csdn.net/qq_36270361/article/details/115823294
        /// <summary>
        /// 读取PLC的高地位转换成浮点数
        /// </summary>
        /// <param name="s1">低位</param>
        /// <param name="s2">高位</param>
        /// <param name="value">浮点数结果</param>
        protected float readFloat(ushort s1, ushort s2)
        {
            //将输入数值short转化为无符号unsigned short
            int us1 = s2, us2 = s1;
            if (s1 < 0) us1 += 65536;
            if (s2 < 0) us2 += 65536;
            //sign: 符号位, exponent: 阶码, mantissa:尾数
            int sign, exponent;
            float mantissa;
            //计算符号位
            sign = us1 / 32768;
            //去掉符号位
            int emCode = us1 % 32768;
            //计算阶码
            exponent = emCode / 128;
            //计算尾数
            mantissa = (float)(emCode % 128 * 65536 + us2) / 8388608;
            //代入公式 fValue = (-1) ^ S x 2 ^ (E - 127) x (1 + M)
            return (float)Math.Pow(-1, sign) * (float)Math.Pow(2, exponent - 127) * (1 + mantissa);
            //return (float)Math.Pow(-1, sign) * (float)Math.Pow(2, exponent - 127) * (1 + mantissa);
        }

        /// https://wenku.baidu.com/view/1c392be288d63186bceb19e8b8f67c1cfbd6ee72.html
        /// <summary>
        /// 将浮点数转转换后通过Modbus协议发给PLC 
        /// </summary>
        /// <param name="value">要输入PLC的值</param>
        /// <param name="ss1">低字</param>
        /// <param name="ss2">高字</param>
        protected void writeFloat(float value, out ushort ss1, out ushort ss2)
        {
            string d = BitConverter.ToString(BitConverter.GetBytes(value).Reverse().ToArray()).Replace("-", "");
            ss2 = Convert.ToUInt16(d.Substring(0, 4), 16);
            ss1 = Convert.ToUInt16(d.Substring(4, 4), 16);
        }

        /// <summary>
        /// 判断写入个数是否过大
        /// </summary>
        /// <param name="numberOfPoints">设置个数</param>
        /// <param name="value">实际个数</param>
        /// <returns></returns>
        protected bool setNuberMax(int numberOfPoints, int value)
        {
            if (numberOfPoints > value)
            {
                string message = $"设置写入个数过大，设置个数：{numberOfPoints} 实际个数：{value}";
                Modbus4Log.Error("BasicModbus", message);
                return false;
            }
            return true;
        }

    }
}

