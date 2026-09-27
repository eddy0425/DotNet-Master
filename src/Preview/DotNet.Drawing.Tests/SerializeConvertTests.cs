using System;
using System.IO;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNet.Drawing.Tests
{
    [TestClass]
    public class SerializeConvertTests
    {
        private sealed class Payload
        {
            public string Name { get; set; }
            public CvCoord Coord { get; set; }
        }

        private string _dir;

        [TestInitialize]
        public void CreateTempDir()
        {
            _dir = Path.Combine(Path.GetTempPath(), "DotNet.Drawing.Tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_dir);
        }

        [TestCleanup]
        public void DeleteTempDir()
        {
            if (Directory.Exists(_dir)) Directory.Delete(_dir, true);
        }

        [TestMethod]
        public void String_RoundTrips()
        {
            var p = new Payload { Name = "roi", Coord = CvCoord.FromRadians(1, 2, 0.5) };
            string json = p.ToJson();

            var back = json.FromJson<Payload>();
            Assert.AreEqual("roi", back.Name);
            Assert.AreEqual(p.Coord, back.Coord);

            var untyped = (Payload)json.FromJson(typeof(Payload));
            Assert.AreEqual(p.Coord, untyped.Coord);
            Assert.AreEqual(p.Coord, SerializeConvert.JsonDeserializeFromString<Payload>(json).Coord);
        }

        [TestMethod]
        public void Bytes_AreUtf8AndRoundTrip()
        {
            var p = new Payload { Name = "区域", Coord = CvCoord.Identity };
            byte[] bytes = SerializeConvert.JsonSerializeToBytes(p);
            Assert.AreEqual(p.ToJson(), Encoding.UTF8.GetString(bytes));
            Assert.AreEqual("区域", SerializeConvert.JsonDeserializeFromBytes<Payload>(bytes).Name);
        }

        [TestMethod]
        public void NewtonsoftJsonFirst_False_StillWorksOnNetFramework()
        {
            bool saved = SerializeConvert.NewtonsoftJsonFirst;
            try
            {
                var r = new Rect2d(1.0, 2.0, 3.0, 4.0);
                string expected = r.ToJson();
                SerializeConvert.NewtonsoftJsonFirst = false;
                Assert.AreEqual(expected, r.ToJson());
                Assert.AreEqual(r, expected.FromJson<Rect2d>());
            }
            finally
            {
                SerializeConvert.NewtonsoftJsonFirst = saved;
            }
        }

        [TestMethod]
        public void File_CreatesNewFile_AndRoundTrips()
        {
            string path = Path.Combine(_dir, "a.json");
            SerializeConvert.JsonSerializeToFile(new Payload { Name = "n" }, path);
            Assert.AreEqual("n", SerializeConvert.JsonDeserializeFromFile<Payload>(path).Name);
            Assert.IsFalse(File.Exists(path + ".tmp"), "临时文件应被替换掉");
        }

        [TestMethod]
        public void File_OverwritingLongerContent_LeavesNoResidue()
        {
            string path = Path.Combine(_dir, "a.json");
            File.WriteAllText(path, new string('x', 4096));

            SerializeConvert.JsonSerializeToFile(new Payload { Name = "short" }, path);

            Assert.AreEqual(new Payload { Name = "short" }.ToJson(), File.ReadAllText(path));
            Assert.IsFalse(File.Exists(path + ".tmp"));
        }

        [TestMethod]
        public void File_FailedWrite_CleansUpTempFile()
        {
            // 目标路径是一个目录：临时文件能写出，但 Move 必然失败
            string path = Path.Combine(_dir, "target");
            Directory.CreateDirectory(path);

            try
            {
                SerializeConvert.JsonSerializeToFile(new Payload(), path);
                Assert.Fail("目标是目录时写入应失败");
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                // 具体异常类型取决于文件系统，这里只关心失败后不留临时文件
            }
            Assert.IsFalse(File.Exists(path + ".tmp"));
        }
    }
}
