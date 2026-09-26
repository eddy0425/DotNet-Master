using System;
using System.IO;
using System.Linq;
using DotNet.Drawing;
using DotNet.Vision.Abstractions;
using HalconDotNet;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNet.HalconAlgo.Tests
{
    [TestClass]
    public class FileImageStrategyTests : HalconTestBase
    {
        private string _dir;
        private FileImageStrategy _strategy;
        private FakeDisplay _display;

        [TestInitialize]
        public void SetUp()
        {
            _dir = Path.Combine(Path.GetTempPath(), "DotNet.HalconAlgo.Tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_dir);
            _strategy = new FileImageStrategy();
            _strategy.inPara.ImageFolder = _dir;
            _display = new FakeDisplay();
        }

        [TestCleanup]
        public void TearDown()
        {
            _strategy.inPara.Dispose();
            if (Directory.Exists(_dir)) Directory.Delete(_dir, true);
        }

        /// <summary>写一张灰度恒为 <paramref name="gray"/> 的 bmp，文件名不含扩展名。</summary>
        private void WriteImage(string name, int gray, int width = 40, int height = 30)
        {
            using (var img = ConstImage(width, height, gray))
            {
                HOperatorSet.WriteImage(img, "bmp", 0, Path.Combine(_dir, name));
            }
        }

        private int CurrentGray() => GrayAt(_strategy.inPara.Image, 5, 5);

        [TestMethod]
        public void Identity()
        {
            Assert.AreEqual(AlgoEnum.FileImage, _strategy.Algorithm);
            Assert.AreEqual("文件图像", _strategy.Name);
            Assert.AreEqual(string.Empty, new FileImage().ImageFolder, "未配置时是空串而不是 null");
        }

        [TestMethod]
        public void Cycles_InNumericOrder_AndWraps()
        {
            WriteImage("10", 100);
            WriteImage("2", 20);
            WriteImage("1", 10);
            _strategy.Init(new FakeRoiHost());

            var grays = Enumerable.Range(0, 4).Select(_ =>
            {
                Assert.IsTrue(_strategy.Fun_action(_display, Strategies.Of()));
                return CurrentGray();
            }).ToArray();

            CollectionAssert.AreEqual(new[] { 10, 20, 100, 10 }, grays, "按数字排序轮播，越界回到第一张");
            Assert.AreEqual("文件图像 : W:40 H:30 索引:0/3", _display.LastText);
            Assert.AreEqual(4, _display.DispImageCount);
            Assert.AreSame(_strategy.inPara.Image, _display.HoImage);
        }

        [TestMethod]
        public void WithoutInit_ScansFolderLazily()
        {
            WriteImage("1", 10);
            Assert.IsTrue(_strategy.Fun_action(_display, Strategies.Of()));
            Assert.AreEqual(10, CurrentGray());
        }

        [TestMethod]
        public void PreviousImage_IsReleased()
        {
            WriteImage("1", 10);
            WriteImage("2", 20);
            _strategy.Init(new FakeRoiHost());

            Assert.IsTrue(_strategy.Fun_action(_display, Strategies.Of()));
            var first = _strategy.inPara.Image;
            Assert.IsTrue(_strategy.Fun_action(_display, Strategies.Of()));

            Assert.IsFalse(first.IsInitialized());
        }

        [TestMethod]
        public void DispTextOff_NoText()
        {
            WriteImage("1", 10);
            _strategy.inPara.DispText = false;
            Assert.IsTrue(_strategy.Fun_action(_display, Strategies.Of()));
            Assert.AreEqual(0, _display.Texts.Count);
        }

        [TestMethod]
        public void Rotate90_SwapsSize()
        {
            WriteImage("1", 10, 40, 30);
            _strategy.inPara.Rotate = 90;

            Assert.IsTrue(_strategy.Fun_action(_display, Strategies.Of()));

            ImageSize(_strategy.inPara.Image, out int w, out int h);
            Assert.AreEqual(30, w);
            Assert.AreEqual(40, h);
        }

        [DataTestMethod]
        [DataRow("行镜像", 29, 0)]
        [DataRow("列镜像", 0, 39)]
        [DataRow("无", 0, 0)]
        public void Mirror(string mode, int brightRow, int brightCol)
        {
            // 左上角单个亮像素，镜像后落到对应角
            using (var dark = ConstImage(40, 30, 0))
            using (var dot = Rectangle1(0, 0, 0, 0))
            using (var img = Paint(dark, dot, 255))
            {
                HOperatorSet.WriteImage(img, "bmp", 0, Path.Combine(_dir, "1"));
            }
            _strategy.inPara.Mirror = mode;

            Assert.IsTrue(_strategy.Fun_action(_display, Strategies.Of()));

            Assert.AreEqual(255, GrayAt(_strategy.inPara.Image, brightRow, brightCol));
        }

        [TestMethod]
        public void Mirror_Diagonal_Transposes()
        {
            WriteImage("1", 10, 40, 30);
            _strategy.inPara.Mirror = "原点镜像";

            Assert.IsTrue(_strategy.Fun_action(_display, Strategies.Of()));

            ImageSize(_strategy.inPara.Image, out int w, out int h);
            Assert.AreEqual(30, w);
            Assert.AreEqual(40, h);
        }

        [TestMethod]
        public void Init_BadFolder_LogsError_DoesNotThrow()
        {
            _strategy.inPara.ImageFolder = Path.Combine(_dir, "missing");

            using (var log = new CapturingLogger())
            {
                _strategy.Init(new FakeRoiHost());

                var entry = log.Entries.Single();
                Assert.AreEqual(LogLevel.Error, entry.Level);
                Assert.AreEqual("FileImageStrategy", entry.Category);
                StringAssert.Contains(entry.Message, "missing");
                Assert.IsInstanceOfType(entry.Exception, typeof(DirectoryNotFoundException));
            }

            Assert.IsNotNull(_strategy.inPara.Image, "失败时仍保留有效的空句柄");
        }

        [TestMethod]
        public void Init_Repeated_ReleasesPreviousImage()
        {
            WriteImage("1", 10);
            _strategy.Init(new FakeRoiHost());
            Assert.IsTrue(_strategy.Fun_action(_display, Strategies.Of()));
            var loaded = _strategy.inPara.Image;

            _strategy.Init(new FakeRoiHost());

            Assert.IsFalse(loaded.IsInitialized());
        }

        [TestMethod]
        public void EmptyFolder_Throws()
        {
            using (new CapturingLogger())
            {
                _strategy.Init(new FakeRoiHost());
            }
            Assert.ThrowsException<InvalidOperationException>(() => _strategy.Fun_action(_display, Strategies.Of()));
        }

        [TestMethod]
        public void SavePara_FolderChanged_RescansNewFolder_AndResetsCursor()
        {
            WriteImage("1", 10);
            WriteImage("2", 20);
            _strategy.Init(new FakeRoiHost());
            Assert.IsTrue(_strategy.Fun_action(_display, Strategies.Of()));
            Assert.AreEqual(10, CurrentGray());

            string other = Path.Combine(_dir, "other");
            Directory.CreateDirectory(other);
            using (var img = ConstImage(40, 30, 99))
            {
                HOperatorSet.WriteImage(img, "bmp", 0, Path.Combine(other, "1"));
            }

            var ui = new FakeUiHost();
            _strategy.DispPara(ui);
            _strategy.SavePara(ui.Set("cmb_ImageFolder", other));

            Assert.IsTrue(_strategy.Fun_action(_display, Strategies.Of()));
            Assert.AreEqual(99, CurrentGray(), "改了目录后必须轮播新目录, 而不是继续旧目录的缓存列表");
            Assert.AreEqual("文件图像 : W:40 H:30 索引:0/1", _display.LastText, "游标从新目录第一张开始");
        }

        [TestMethod]
        public void SavePara_SameFolder_KeepsCursor()
        {
            WriteImage("1", 10);
            WriteImage("2", 20);
            _strategy.Init(new FakeRoiHost());
            Assert.IsTrue(_strategy.Fun_action(_display, Strategies.Of()));

            // SavePara 在每次点"运行"时都会被调用: 目录没变就不能打断轮播
            var ui = new FakeUiHost();
            _strategy.DispPara(ui);
            _strategy.SavePara(ui);

            Assert.IsTrue(_strategy.Fun_action(_display, Strategies.Of()));
            Assert.AreEqual(20, CurrentGray());
        }

        [TestMethod]
        public void ReadFailure_KeepsPreviousImageUsable_AndSkipsBadFile()
        {
            WriteImage("1", 10);
            File.WriteAllBytes(Path.Combine(_dir, "2.bmp"), new byte[] { 1, 2, 3, 4 });
            _strategy.Init(new FakeRoiHost());

            Assert.IsTrue(_strategy.Fun_action(_display, Strategies.Of()));
            var before = _strategy.inPara.Image;

            Assert.ThrowsException<HOperatorException>(() => _strategy.Fun_action(_display, Strategies.Of()));

            Assert.AreSame(before, _strategy.inPara.Image, "读图失败不能替换输出");
            Assert.IsTrue(_strategy.inPara.Image.IsInitialized(), "读图失败不能留下已释放的句柄");
            Assert.AreEqual(10, CurrentGray());

            // 坏图不会卡住轮播: 下一轮越过它回到第一张
            Assert.IsTrue(_strategy.Fun_action(_display, Strategies.Of()));
            Assert.AreEqual(10, CurrentGray());
            Assert.AreEqual("文件图像 : W:40 H:30 索引:0/2", _display.LastText);
        }

        [TestMethod]
        public void Output_Image()
        {
            WriteImage("1", 10);
            _strategy.GenTreeNode(new FakeTree());
            Assert.IsTrue(_strategy.Fun_action(_display, Strategies.Of()));

            Assert.AreSame(_strategy.inPara.Image, Strategies.Of(_strategy).ResolveFrom<HObject>("文件图像/图像"));
        }

        [TestMethod]
        public void ParaRoundTrip()
        {
            _strategy.inPara.Rotate = 180;
            _strategy.inPara.Mirror = "列镜像";
            var ui = new FakeUiHost();
            _strategy.DispPara(ui);

            var other = new FileImageStrategy();
            try
            {
                other.SavePara(ui);
                Assert.AreEqual(180, other.inPara.Rotate);
                Assert.AreEqual("列镜像", other.inPara.Mirror);
                Assert.AreEqual(_dir, other.inPara.ImageFolder);
            }
            finally
            {
                other.inPara.Dispose();
            }
        }
    }

    [TestClass]
    public class RotateImageStrategyTests : HalconTestBase
    {
        private HObject _source;
        private FakeDisplay _display;
        private RotateImageStrategy _strategy;

        [TestInitialize]
        public void SetUp()
        {
            _source = ConstImage(40, 30, 50);
            _display = new FakeDisplay();
            _display.SetImage(_source);
            _strategy = new RotateImageStrategy();
        }

        [TestCleanup]
        public void TearDown()
        {
            _strategy.inPara.Image.Dispose();
            _source.Dispose();
        }

        private string RunWithCoord(string mode, double angleDeg)
        {
            _strategy.inPara.RotateType = mode;
            _strategy.inPara.CoordIn = "定位/坐标系";
            var locator = StubStrategy.Coord("定位", new Point2d(0, 0), CvCoord.FromDegrees(20, 15, angleDeg));
            Assert.IsTrue(_strategy.Fun_action(_display, Strategies.Of(locator)));
            return _display.LastText;
        }

        [TestMethod]
        public void ImageCenter_ZeroAngle_CopiesImage()
        {
            Assert.IsTrue(_strategy.Fun_action(_display, Strategies.Of()));

            Assert.AreNotSame(_source, _strategy.inPara.Image);
            Assert.AreEqual(50, GrayAt(_strategy.inPara.Image, 10, 10));
            Assert.AreSame(_strategy.inPara.Image, _display.HoImage);
            Assert.AreEqual("旋转图像 : 方式:图像中心 角度:0.00°", _display.LastText);
        }

        [TestMethod]
        public void ImageCenter_90_Rotates()
        {
            _strategy.inPara.RotateAngle = 90;

            Assert.IsTrue(_strategy.Fun_action(_display, Strategies.Of()));

            ImageSize(_strategy.inPara.Image, out int w, out int h);
            Assert.AreEqual(30, w);
            Assert.AreEqual(40, h);
            StringAssert.EndsWith(_display.LastText, "角度:90.00°");
        }

        [TestMethod]
        public void ImageOverload_UsesGivenImage()
        {
            var display = new FakeDisplay();
            Assert.IsTrue(_strategy.Fun_action(_source, display));
            Assert.AreEqual(50, GrayAt(_strategy.inPara.Image, 0, 0));
        }

        [TestMethod]
        public void NoImage_Throws()
        {
            var ex = Assert.ThrowsException<InvalidOperationException>(() => _strategy.Fun_action(new FakeDisplay(), Strategies.Of()));
            StringAssert.Contains(ex.Message, "旋转图像");
        }

        [TestMethod]
        public void ImageIn_FromUpstream()
        {
            using (var upstream = ConstImage(20, 10, 7))
            {
                _strategy.inPara.ImageIn = "取像/图像";
                Assert.IsTrue(_strategy.Fun_action(_display, Strategies.Of(new StubStrategy("取像").Output("图像", upstream))));
                Assert.AreEqual(7, GrayAt(_strategy.inPara.Image, 0, 0));
            }
        }

        [DataTestMethod]
        [DataRow("坐标系", 30, -30)]
        [DataRow("坐标系X轴", -45, 45)]
        [DataRow("坐标系Y轴", 30, 60)]
        [DataRow("坐标系Y轴", -30, -60)]
        [DataRow("坐标系Y轴", 0, 90)]
        public void CoordModes_RotationAngle(string mode, double coordDeg, double expectedDeg)
        {
            string text = RunWithCoord(mode, coordDeg);

            Assert.AreEqual($"旋转图像 : 方式:{mode} 坐标:(20.00,15.00) 角度:{expectedDeg:F2}°", text);
            ImageSize(_strategy.inPara.Image, out int w, out int h);
            Assert.AreEqual(40, w, "affine_trans_image 不改变图像尺寸");
            Assert.AreEqual(30, h);
        }

        [TestMethod]
        public void CoordMode_AngleNormalizedBeforeMapping()
        {
            // 350° 归一化为 -10° → 坐标系模式旋转 +10°
            StringAssert.EndsWith(RunWithCoord("坐标系", 350), "角度:10.00°");
        }

        [TestMethod]
        public void UnknownMode_WarnsAndKeepsAngle()
        {
            using (var log = new CapturingLogger())
            {
                string text = RunWithCoord("xxx", 30);

                StringAssert.EndsWith(text, "角度:30.00°");
                var entry = log.Entries.Single();
                Assert.AreEqual(LogLevel.Warn, entry.Level);
                Assert.AreEqual("RotateImageStrategy", entry.Category);
                StringAssert.Contains(entry.Message, "xxx");
            }
        }

        [TestMethod]
        public void CoordMode_Unresolvable_Throws()
        {
            _strategy.inPara.RotateType = "坐标系";
            _strategy.inPara.CoordIn = "定位/坐标系";
            Assert.ThrowsException<AlgoOutputNotFoundException>(() => _strategy.Fun_action(_display, Strategies.Of()));
        }

        [TestMethod]
        public void SavePara_ImageCenter_InvalidAngleKeepsOld()
        {
            _strategy.inPara.RotateAngle = 45;
            var ui = new FakeUiHost();
            _strategy.DispPara(ui);
            Assert.AreEqual("旋转角度", ui.Values["lbl_102"]);

            _strategy.SavePara(ui.Set("cmb_102", "abc"));
            Assert.AreEqual(45f, _strategy.inPara.RotateAngle);

            _strategy.SavePara(ui.Set("cmb_102", "12.5"));
            Assert.AreEqual(12.5f, _strategy.inPara.RotateAngle);
        }

        [TestMethod]
        public void SavePara_CoordMode_Slot102IsCoordIn()
        {
            _strategy.inPara.RotateType = "坐标系";
            _strategy.inPara.RotateAngle = 45;
            var ui = new FakeUiHost();
            _strategy.DispPara(ui);
            Assert.AreEqual("坐标系", ui.Values["lbl_102"]);

            _strategy.SavePara(ui.Set("cmb_102", "定位/坐标系"));

            Assert.AreEqual("定位/坐标系", _strategy.inPara.CoordIn);
            Assert.AreEqual(45f, _strategy.inPara.RotateAngle);
        }
    }

    [TestClass]
    public class LineRotImageStrategyTests : HalconTestBase
    {
        private HObject _source;
        private FakeDisplay _display;
        private LineRotImageStrategy _strategy;

        [TestInitialize]
        public void SetUp()
        {
            _source = ConstImage(40, 30, 50);
            _display = new FakeDisplay();
            _display.SetImage(_source);
            _strategy = new LineRotImageStrategy();
            _strategy.inPara.LineIn = "拟合/直线";
        }

        [TestCleanup]
        public void TearDown()
        {
            _strategy.inPara.Image.Dispose();
            _source.Dispose();
        }

        private static StubStrategy LineSource(CvLine line) => new StubStrategy("拟合").Output("直线", line);

        [TestMethod]
        public void LineUnresolvable_Throws()
        {
            Assert.ThrowsException<AlgoOutputNotFoundException>(() => _strategy.Fun_action(_display, Strategies.Of()));
        }

        [TestMethod]
        public void DegenerateLine_Throws()
        {
            var line = new CvLine(new Point2d(5, 5), new Point2d(5, 5));
            // 退化直线是业务错误, 不再伪装成 NullReferenceException; 消息带工具名与直线来源便于定位
            var ex = Assert.ThrowsException<InvalidOperationException>(() => _strategy.Fun_action(_display, Strategies.Of(LineSource(line))));
            StringAssert.Contains(ex.Message, "直线图像");
            StringAssert.Contains(ex.Message, "拟合/直线");
        }

        [TestMethod]
        public void NoImage_Throws()
        {
            Assert.ThrowsException<InvalidOperationException>(() => _strategy.Fun_action(new FakeDisplay(), Strategies.Of()));
        }

        [DataTestMethod]
        [DataRow("平行X轴", 0, 0, 10, 0, 0.0)]
        [DataRow("平行X轴", 0, 0, 10, 10, 45.0)]
        [DataRow("平行X轴", 10, 10, 0, 0, 45.0)]       // 反向直线：-135° 归一化为 45°
        [DataRow("平行X轴", 0, 0, 10, -10, -45.0)]
        [DataRow("平行X轴", 0, 0, 0, 10, 90.0)]
        [DataRow("平行Y轴", 0, 0, 0, 10, 0.0)]
        [DataRow("平行Y轴", 0, 0, 0, -10, 0.0)]        // -90° - 90° = -180° 归一化为 0°
        [DataRow("平行Y轴", 0, 0, 10, 10, -45.0)]
        [DataRow("平行Y轴", 0, 0, 10, 0, -90.0)]
        public void RotationAngle_Normalized(string axis, double x1, double y1, double x2, double y2, double expectedDeg)
        {
            _strategy.inPara.AlignAxis = axis;
            var line = new CvLine(x1, y1, x2, y2);

            Assert.IsTrue(_strategy.Fun_action(_display, Strategies.Of(LineSource(line))));

            Assert.AreEqual($"直线图像 : 对齐:{axis} 旋转:{expectedDeg:F2}°", _display.LastText);
            Assert.AreSame(_strategy.inPara.Image, _display.HoImage);
            ImageSize(_strategy.inPara.Image, out int w, out int h);
            Assert.AreEqual(40, w);
            Assert.AreEqual(30, h);
        }

        [TestMethod]
        public void ZeroRotation_PreservesPixels()
        {
            Assert.IsTrue(_strategy.Fun_action(_display, Strategies.Of(LineSource(new CvLine(0, 0, 10, 0)))));
            Assert.AreEqual(50, GrayAt(_strategy.inPara.Image, 15, 20));
        }

        [TestMethod]
        public void ImageOverload_ResolvesLineFromEmptyList_Throws()
        {
            // 单图重载没有上游：直线来源必然解析不到
            Assert.ThrowsException<AlgoOutputNotFoundException>(() => _strategy.Fun_action(_source, new FakeDisplay()));
        }

        [TestMethod]
        public void ParaRoundTrip()
        {
            _strategy.inPara.AlignAxis = "平行Y轴";
            _strategy.inPara.DispText = false;
            var ui = new FakeUiHost();
            _strategy.DispPara(ui);

            var other = new LineRotImageStrategy();
            other.SavePara(ui);
            other.inPara.Image.Dispose();

            Assert.AreEqual("拟合/直线", other.inPara.LineIn);
            Assert.AreEqual("平行Y轴", other.inPara.AlignAxis);
            Assert.AreEqual("默认", other.inPara.ImageIn);
            Assert.IsFalse(other.inPara.DispText);
        }
    }
}
