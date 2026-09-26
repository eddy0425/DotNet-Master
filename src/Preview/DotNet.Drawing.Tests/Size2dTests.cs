using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace DotNet.Drawing.Tests
{
    [TestClass]
    public class Size2dTests
    {
        [TestMethod]
        public void Json_RoundTrips()
        {
            var s = new Size2d(3.0, 4.25);
            var back = JsonConvert.DeserializeObject<Size2d>(JsonConvert.SerializeObject(s));
            Assert.AreEqual(3.0, back.Width, 1e-12);
            Assert.AreEqual(4.25, back.Height, 1e-12);
        }

        [TestMethod]
        public void Json_NegativeSize_IsRejected()
        {
            // [JsonConstructor] 走构造函数校验，落盘数据也不能造出非法尺寸；
            // Newtonsoft 不包装构造函数抛出的异常，调用方收到的是原始 ArgumentOutOfRangeException
            Assert.ThrowsException<ArgumentOutOfRangeException>(
                () => JsonConvert.DeserializeObject<Size2d>("{\"Width\":-1,\"Height\":1}"));
        }
    }
}
