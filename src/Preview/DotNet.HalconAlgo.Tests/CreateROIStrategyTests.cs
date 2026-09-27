using DotNet.Drawing;
using DotNet.HalconCore;
using HalconDotNet;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNet.HalconAlgo.Tests
{
    [TestClass]
    public class CreateROIStrategyTests : HalconTestBase
    {
        private FakeDisplay _display;
        private CreateROIStrategy _strategy;

        [TestInitialize]
        public void SetUp()
        {
            _display = new FakeDisplay();
            _strategy = new CreateROIStrategy();
            // 左上 (40,20)，宽 60 高 40 → 中心 (70,40)，面积 61*41（HALCON 矩形含端点）
            _strategy.inPara.HoRect = NewRegion(RectEnum.Rectangle, 40, 20, 60, 40);
        }

        [TestCleanup]
        public void TearDown()
        {
            _strategy.Dispose();
            _strategy.inPara.HoRect.Dispose();
        }

        [TestMethod]
        public void Identity()
        {
            Assert.AreEqual(AlgoEnum.CreateROI, _strategy.Algorithm);
            Assert.AreEqual("创建ROI", _strategy.Name);
        }

        [TestMethod]
        public void RoiNotDrawn_ReturnsFalse_RedText_ClearsResult()
        {
            _strategy.inPara.HoRect.Dispose();
            _strategy.inPara.HoRect = new CvRegion();
            _strategy.inPara.DispText = false;
            _strategy.inPara.Coord = new CvCoord(5, 5);

            Assert.IsFalse(_strategy.Fun_action(_display, Strategies.Of()));

            Assert.AreEqual(1, _display.Texts.Count, "报错不受 DispText 门控");
            Assert.AreEqual("创建ROI : 尚未绘制 ROI", _display.LastText);
            Assert.AreEqual(HColor.Red.Name, _display.Texts[0].ColorName);
            Assert.AreEqual(new CvCoord(), _strategy.inPara.Coord);
            Assert.IsFalse(_strategy.inPara.Result.HoRegion.IsUsableRegion());
        }

        [TestMethod]
        public void Default_CopiesRegion_CoordIsRoiCenter()
        {
            Assert.IsTrue(_strategy.Fun_action(_display, Strategies.Of()));

            var result = _strategy.inPara.Result.HoRegion;
            Assert.AreNotSame(_strategy.inPara.HoRect.HoRegion, result, "结果是独立句柄，不与配置 ROI 共享");
            double area = AreaCenter(result, out Point2d center);
            Assert.AreEqual(AreaCenter(_strategy.inPara.HoRect.HoRegion, out _), area);
            Assert.AreEqual(70, center.X, 0.6);
            Assert.AreEqual(40, center.Y, 0.6);

            Assert.AreEqual(new CvCoord(new Point2d(70, 40)), _strategy.inPara.Coord);

            Assert.AreEqual(1, _display.Objects.Count);
            Assert.AreSame(result, _display.Objects[0].Item);
            Assert.AreEqual(HColor.Blue.Name, _display.Objects[0].ColorName);
            StringAssert.StartsWith(_display.LastText, "创建ROI : 中心:(70.00,40.00) 宽:60 高:40 跟随坐标:默认");
            Assert.AreEqual(HColor.Green.Name, _display.Texts[0].ColorName);
        }

        [TestMethod]
        public void DisplayFlagsOff_DrawsNothing()
        {
            _strategy.inPara.DispRegion = false;
            _strategy.inPara.DispText = false;

            Assert.IsTrue(_strategy.Fun_action(_display, Strategies.Of()));

            Assert.AreEqual(0, _display.Objects.Count);
            Assert.AreEqual(0, _display.Texts.Count);
        }

        [TestMethod]
        public void RepeatedRun_ReleasesPreviousResult()
        {
            Assert.IsTrue(_strategy.Fun_action(_display, Strategies.Of()));
            var first = _strategy.inPara.Result.HoRegion;

            Assert.IsTrue(_strategy.Fun_action(_display, Strategies.Of()));

            Assert.AreNotSame(first, _strategy.inPara.Result.HoRegion);
            Assert.IsFalse(first.IsInitialized());
            Assert.IsTrue(_strategy.inPara.HoRect.HoRegion.IsInitialized(), "配置 ROI 不受影响");
        }

        [TestMethod]
        public void CoordIn_Translation_MovesRegionAndCoord()
        {
            _strategy.inPara.CoordIn = "定位/坐标系";
            var locator = StubStrategy.Coord("定位", new Point2d(0, 0), new CvCoord(10, 5));

            Assert.IsTrue(_strategy.Fun_action(_display, Strategies.Of(locator)));

            AreaCenter(_strategy.inPara.Result.HoRegion, out Point2d center);
            Assert.AreEqual(80, center.X, 0.6);
            Assert.AreEqual(45, center.Y, 0.6);
            Assert.AreEqual(80, _strategy.inPara.Coord.X, 1e-6);
            Assert.AreEqual(45, _strategy.inPara.Coord.Y, 1e-6);
            Assert.AreEqual(0, _strategy.inPara.Coord.Angle.Radians, 1e-12);
            StringAssert.Contains(_display.LastText, "跟随坐标:定位/坐标系");
        }

        [TestMethod]
        public void CoordIn_Rotation_CoordUsesInputAngle()
        {
            // 绕 ROI 中心转 90°：中心不动，区域外接框宽高互换，坐标系角度取输入角度。
            _strategy.inPara.CoordIn = "定位/坐标系";
            var locator = StubStrategy.Coord("定位", new Point2d(70, 40), CvCoord.FromDegrees(70, 40, 90));

            Assert.IsTrue(_strategy.Fun_action(_display, Strategies.Of(locator)));

            // TransPoint 走 affine_trans_pixel（像素中心约定），旋转时原点会偏出最多 1px：
            // 当前实测 (70,39)，而非绕自身旋转应得的 (70,40)。
            Assert.AreEqual(70, _strategy.inPara.Coord.X, 1.0);
            Assert.AreEqual(40, _strategy.inPara.Coord.Y, 1.0);
            Assert.AreEqual(90, _strategy.inPara.Coord.AngleDegrees, 1e-9);

            HOperatorSet.SmallestRectangle1(_strategy.inPara.Result.HoRegion, out HTuple r1, out HTuple c1, out HTuple r2, out HTuple c2);
            Assert.AreEqual(60, r2.D - r1.D, 1.5, "旋转后高度 ≈ 原宽");
            Assert.AreEqual(40, c2.D - c1.D, 1.5, "旋转后宽度 ≈ 原高");
        }

        [TestMethod]
        public void CoordIn_Unresolvable_ThrowsAndClearsResult()
        {
            Assert.IsTrue(_strategy.Fun_action(_display, Strategies.Of()));
            _strategy.inPara.CoordIn = "不存在/坐标系";

            Assert.ThrowsException<AlgoOutputNotFoundException>(() => _strategy.Fun_action(_display, Strategies.Of()));

            Assert.IsFalse(_strategy.inPara.Result.HoRegion.IsUsableRegion(), "异常时清掉上一轮结果，避免下游读到过期区域");
            Assert.AreEqual(new CvCoord(), _strategy.inPara.Coord);
        }

        [TestMethod]
        public void Outputs_AfterGenTreeNode()
        {
            var tree = new FakeTree();
            _strategy.GenTreeNode(tree);
            CollectionAssert.IsSubsetOf(new[]
            {
                "创建ROI/坐标系", "创建ROI/坐标系/原点", "创建ROI/坐标系/原点/行", "创建ROI/坐标系/原点/列",
                "创建ROI/坐标系/角度", "创建ROI/区域",
            }, tree.Paths);

            _strategy.inPara.CoordIn = "定位/坐标系";
            var locator = StubStrategy.Coord("定位", new Point2d(0, 0), new CvCoord(10, 5));
            Assert.IsTrue(_strategy.Fun_action(_display, Strategies.Of(locator)));

            var all = Strategies.Of(locator, _strategy);
            Assert.AreEqual(new Point2d(70, 40), all.ResolveFrom<Point2d>("创建ROI/坐标系".ToTmplPoint()), "TmplPoint 是配置态中心，不随跟随变化");
            Assert.AreEqual(_strategy.inPara.Coord, all.ResolveFrom<CvCoord>("创建ROI/坐标系"));
            Assert.AreEqual(_strategy.inPara.Coord.Center, all.ResolveFrom<Point2d>("创建ROI/坐标系/原点"));
            Assert.AreEqual(45, all.ResolveFrom<double>("创建ROI/坐标系/原点/行"), 1e-6);
            Assert.AreEqual(80, all.ResolveFrom<double>("创建ROI/坐标系/原点/列"), 1e-6);
            Assert.AreEqual(0, all.ResolveFrom<double>("创建ROI/坐标系/角度"), 1e-12);
            Assert.AreSame(_strategy.inPara.Result.HoRegion, all.ResolveRegionFrom("创建ROI/区域"));
        }

        [TestMethod]
        public void ParaRoundTrip()
        {
            _strategy.inPara.CoordIn = "定位/坐标系";
            _strategy.inPara.DispRegion = false;
            _strategy.inPara.FontSize = 30;

            var ui = new FakeUiHost();
            _strategy.DispPara(ui);
            CollectionAssert.AreEqual(new[] { TabPageEnum.Region, TabPageEnum.Display }, ui.Tabs);
            Assert.AreEqual("60", ui.Values["cmb_Width"]);
            Assert.AreEqual("70;40", ui.Values["cmb_Center"]);

            using (var other = new CreateROIStrategy())
            {
                other.SavePara(ui);
                Assert.AreEqual("定位/坐标系", other.inPara.CoordIn);
                Assert.IsFalse(other.inPara.DispRegion);
                Assert.IsTrue(other.inPara.DispText);
                Assert.AreEqual(30, other.inPara.FontSize);
            }
        }

        [TestMethod]
        public void Close_ClearsResult_KeepsRoi()
        {
            Assert.IsTrue(_strategy.Fun_action(_display, Strategies.Of()));

            _strategy.Close(new FakeRoiHost());

            Assert.IsFalse(_strategy.inPara.Result.HoRegion.IsUsableRegion());
            Assert.AreEqual(new CvCoord(), _strategy.inPara.Coord);
            Assert.IsTrue(_strategy.inPara.HoRect.HoRegion.IsUsableRegion(), "重新打开工具页仍可复用配置 ROI");
        }

        [TestMethod]
        public void Dispose_ReleasesOnlyResult_Idempotent()
        {
            Assert.IsTrue(_strategy.Fun_action(_display, Strategies.Of()));
            var result = _strategy.inPara.Result.HoRegion;

            _strategy.Dispose();
            _strategy.Dispose();
            _strategy.Close(new FakeRoiHost()); // Dispose 后 Close 为空操作

            Assert.IsFalse(result.IsInitialized());
            Assert.IsNull(_strategy.inPara.Result.HoRegion);
            Assert.IsTrue(_strategy.inPara.HoRect.HoRegion.IsInitialized(), "配置态 ROI 随 job 落盘，不由策略释放");
        }

        [TestMethod]
        public void DrawROIAsync_Cancel_RestoresType()
        {
            var host = new FakeRoiHost { Confirm = false };

            _strategy.DrawROIAsync(host, RectEnum.Circle, true).GetAwaiter().GetResult();

            Assert.AreEqual(RectEnum.Circle, host.TypeDuringDraw);
            Assert.AreEqual(RectEnum.Rectangle, _strategy.inPara.HoRect.Type);
            Assert.AreEqual(1, host.FakeDisplay.Regions.Count, "取消后仍把原 ROI 重画回去");
            Assert.AreEqual(1, host.SetRectParaCount);
        }

        [TestMethod]
        public void DrawROIAsync_Confirm_KeepsType_ModifyUsesModDraw()
        {
            var host = new FakeRoiHost();

            _strategy.DrawROIAsync(host, RectEnum.Circle, true).GetAwaiter().GetResult();
            Assert.AreEqual(RectEnum.Circle, _strategy.inPara.HoRect.Type);

            _strategy.DrawROIAsync(host, RectEnum.Rectangle, false).GetAwaiter().GetResult();
            Assert.AreEqual(1, host.DrawCount);
            Assert.AreEqual(1, host.DrawModCount);
            Assert.AreEqual(RectEnum.Circle, _strategy.inPara.HoRect.Type, "修改模式不改类型");
            Assert.AreEqual(2, host.SetRectParaCount);
        }

        [TestMethod]
        public void DispROI_KeepsType()
        {
            var host = new FakeRoiHost();
            _strategy.DispROI(host);
            Assert.AreEqual(RectEnum.Rectangle, _strategy.inPara.HoRect.Type);
            Assert.AreEqual(1, host.SetRectParaCount);
            Assert.AreEqual(0, host.FakeDisplay.Regions.Count + host.FakeDisplay.Objects.Count);
        }

        [TestMethod]
        public void Result_IsNotSerialized()
        {
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(_strategy.inPara);
            Assert.IsFalse(json.Contains("\"Result\""));
            Assert.IsTrue(json.Contains("\"HoRect\""));
        }
    }
}
