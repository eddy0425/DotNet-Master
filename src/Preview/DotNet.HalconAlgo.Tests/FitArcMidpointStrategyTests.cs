using System;
using System.Linq;
using DotNet.Drawing;
using DotNet.Vision.Abstractions;
using HalconDotNet;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNet.HalconAlgo.Tests
{
    [TestClass]
    public class FitArcMidpointStrategyTests : HalconTestBase
    {
        private const int Size = 200;
        private static readonly Point2d DiskCenter = new Point2d(100, 100);
        private const double Radius = 50;

        private HObject _image;
        private FakeDisplay _display;
        private FitArcMidpointStrategy _strategy;

        [TestInitialize]
        public void SetUp()
        {
            _image = DiskImage(Size, Size, DiskCenter, Radius);
            _display = new FakeDisplay();
            _display.SetImage(_image);

            _strategy = new FitArcMidpointStrategy();
            // 圆盘右侧弧段：沿列从内（亮）向外（暗）测量，行 70..130 共 7 个测量矩形
            _strategy.inPara.HoRect = NewAffRect(new Point2d(150, 100), 40, 60);
            _strategy.inPara.Transition = "由白到黑";
        }

        [TestCleanup]
        public void TearDown()
        {
            _strategy.Dispose();
            _image.Dispose();
        }

        [TestMethod]
        public void Identity()
        {
            Assert.AreEqual(AlgoEnum.FitArcMidpoint, _strategy.Algorithm);
            Assert.AreEqual("圆弧中点", _strategy.Name);
        }

        [TestMethod]
        public void FindsMidpointOfRightArc()
        {
            Assert.IsTrue(_strategy.Fun_action(_display, Strategies.Of()));

            var mid = _strategy.inPara.ArcMidpoint;
            Assert.AreEqual(150, mid.X, 1.0);
            Assert.AreEqual(100, mid.Y, 1.0);
        }

        [TestMethod]
        public void Overlay_IsDrawnAndReleased()
        {
            Assert.IsTrue(_strategy.Fun_action(_display, Strategies.Of()));

            Assert.IsNull(_strategy.TakeRenderData(), "Fun_action 画完叠加层后应立即释放，不留待取数据");
            Assert.AreEqual(1, _display.Points.Count(p => p.ColorName == HColor.OrangeRed.Name), "中点");
            Assert.AreEqual(5, _display.Points.Count(p => p.ColorName == HColor.Green.Name), "7 点裁首尾后剩 5 点");
            Assert.AreEqual(2, _display.Points.Count(p => p.ColorName == HColor.Red.Name));
            StringAssert.Contains(_display.LastText, "圆弧中点 : 中点:(");
            StringAssert.Contains(_display.LastText, "用点:5");
            Assert.IsTrue(_display.Texts.All(t => t.ColorName == HColor.Green.Name));
        }

        [TestMethod]
        public void ImageOverload_SetsDisplayImage()
        {
            var display = new FakeDisplay();
            Assert.IsTrue(_strategy.Fun_action(_image, display));
            Assert.AreSame(_image, display.HoImage);
            Assert.AreEqual(150, _strategy.inPara.ArcMidpoint.X, 1.0);
            Assert.IsNull(_strategy.TakeRenderData());
        }

        [TestMethod]
        public void NoEdge_Throws_ButStillDrawsPartialOverlay()
        {
            _strategy.inPara.Transition = "由黑到白";
            _strategy.inPara.DispFixRegion = true;

            var ex = Assert.ThrowsException<InvalidOperationException>(() => _strategy.Fun_action(_display, Strategies.Of()));

            StringAssert.Contains(ex.Message, "未找到足够的轮廓点");
            Assert.AreEqual(1, _display.Objects.Count(o => o.ColorName == HColor.Blue.Name), "失败时仍显示查找区域便于排查");
            Assert.AreEqual(7, _display.Rect2Centers.Count, "失败时仍显示测量矩形");
            Assert.AreEqual(0, _display.Texts.Count, "失败时没有结果文本");
            Assert.IsNull(_strategy.TakeRenderData());
        }

        [TestMethod]
        public void TooFewPoints_Throws_ButStillDrawsFoundPoints()
        {
            // 行 60..114 右半圆涂黑: 测量矩形在行 70..130 每 10 行一个, 只有行 120 / 130 仍有边缘, 共 2 点 < 3
            using (var mask = Rectangle1(60, 100, 114, Size - 1))
            using (var masked = Paint(_image, mask, 0))
            {
                _display.SetImage(masked);

                var ex = Assert.ThrowsException<InvalidOperationException>(() => _strategy.Fun_action(_display, Strategies.Of()));

                StringAssert.Contains(ex.Message, "未找到足够的轮廓点");
            }

            // 点不足恰恰最需要看点落在哪: 已找到的点在失败时也要画出来
            var green = _display.Points.Where(p => p.ColorName == HColor.Green.Name).ToList();
            Assert.AreEqual(2, green.Count);
            Assert.IsTrue(green.All(p => p.Item.Y > 115), "只有未涂黑的行 120 / 130 有点");
            Assert.AreEqual(0, _display.Points.Count(p => p.ColorName == HColor.OrangeRed.Name), "失败时没有中点");
            Assert.IsNull(_strategy.TakeRenderData());
        }

        [DataTestMethod]
        [DataRow(150.0, 100.0, 0.0, DisplayName = "右侧弧")]
        [DataRow(50.0, 100.0, 180.0, DisplayName = "左侧弧(起止角跨 ±π)")]
        [DataRow(100.0, 50.0, 90.0, DisplayName = "顶部弧")]
        [DataRow(100.0, 150.0, -90.0, DisplayName = "底部弧")]
        public void FindsMidpoint_InEveryDirection(double x, double y, double phiDeg)
        {
            // 测量方向沿 phi 由圆内(亮)指向圆外(暗); HALCON 角度逆时针为正、行向下, 90° 指向图像上方
            _strategy.inPara.HoRect.Dispose();
            _strategy.inPara.HoRect = NewAffRect(new Point2d(x, y), 40, 60, phiDeg * Math.PI / 180);

            Assert.IsTrue(_strategy.Fun_action(_display, Strategies.Of()));

            var mid = _strategy.inPara.ArcMidpoint;
            Assert.AreEqual(x, mid.X, 1.0);
            Assert.AreEqual(y, mid.Y, 1.0);
        }

        [TestMethod]
        public void CoarseGate_CullsOutlier_MidpointUnaffected()
        {
            // 行 77..83 处圆盘向右凸出到列 174: 行 80 的测量点落在 ~175, 径向偏差 ~25 > 粗滤门限 15。
            // 凸起故意避开弧顶(行 100): 7 点短弧上弧顶的单个离群点会劫持第一阶段 atukey 粗拟合,
            // 反把好点剔掉 —— 这是已知局限, 此处只验证常规离群点的剔除路径。
            using (var bump = Rectangle1(77, 140, 83, 174))
            using (var bumped = Paint(_image, bump, 255))
            {
                _display.SetImage(bumped);
                _strategy.inPara.HoRect.Dispose();
                _strategy.inPara.HoRect = NewAffRect(new Point2d(150, 100), 60, 60);
                _strategy.inPara.TrimEnds = "否";

                Assert.IsTrue(_strategy.Fun_action(_display, Strategies.Of()));
            }

            var mid = _strategy.inPara.ArcMidpoint;
            Assert.AreEqual(150, mid.X, 1.0);
            Assert.AreEqual(100, mid.Y, 1.0);

            var red = _display.Points.Where(p => p.ColorName == HColor.Red.Name).ToList();
            Assert.AreEqual(1, red.Count, "离群点被剔除并以红色显示");
            Assert.AreEqual(175, red[0].Item.X, 1.0);
            Assert.AreEqual(80, red[0].Item.Y, 1.0);
            StringAssert.Contains(_display.LastText, "用点:6");
        }

        [TestMethod]
        public void NotEnoughPoints_Throws_StillDrawsFoundPoints()
        {
            // 3 个测量矩形(行 90/100/110), 把行 110 附近涂暗后只剩 2 个边缘点 < 最少 3 点
            using (var mask = Rectangle1(105, 100, 115, Size - 1))
            using (var masked = Paint(_image, mask, 0))
            {
                _display.SetImage(masked);
                _strategy.inPara.HoRect.Dispose();
                _strategy.inPara.HoRect = NewAffRect(new Point2d(150, 100), 40, 20);

                var ex = Assert.ThrowsException<InvalidOperationException>(() => _strategy.Fun_action(_display, Strategies.Of()));
                StringAssert.Contains(ex.Message, "未找到足够的轮廓点");
            }

            // 原先点集只在拟合成功后才挂到显示数据上, 恰恰是点不够时最需要看点落在哪
            Assert.AreEqual(2, _display.Points.Count(p => p.ColorName == HColor.Green.Name), "失败时仍显示已找到的点");
            Assert.AreEqual(0, _display.Points.Count(p => p.ColorName == HColor.OrangeRed.Name), "失败时没有中点");
            Assert.AreEqual(0, _display.Texts.Count);
        }

        [TestMethod]
        public void RoiNotDrawn_Throws()
        {
            _strategy.inPara.HoRect = new CvRegion { Type = RectEnum.AffRect };

            var ex = Assert.ThrowsException<InvalidOperationException>(() => _strategy.Fun_action(_display, Strategies.Of()));
            StringAssert.Contains(ex.Message, "尚未绘制 ROI");
        }

        [TestMethod]
        public void NoImage_Throws()
        {
            Assert.ThrowsException<InvalidOperationException>(() => _strategy.Fun_action(new FakeDisplay(), Strategies.Of()));
        }

        [TestMethod]
        public void Failure_ResetsPreviousMidpoint()
        {
            Assert.IsTrue(_strategy.Fun_action(_display, Strategies.Of()));
            Assert.AreNotEqual(default(Point2d), _strategy.inPara.ArcMidpoint);

            _strategy.inPara.Transition = "由黑到白";
            Assert.ThrowsException<InvalidOperationException>(() => _strategy.Fun_action(_display, Strategies.Of()));

            // 宿主吞掉异常后下游照跑: 不能继续拿到上一轮的中点
            Assert.AreEqual(default(Point2d), _strategy.inPara.ArcMidpoint);
        }

        [TestMethod]
        public void ImageOverload_Failure_ResetsPreviousMidpoint()
        {
            Assert.IsTrue(_strategy.Fun_action(_image, new FakeDisplay()));

            _strategy.inPara.Transition = "由黑到白";
            Assert.ThrowsException<InvalidOperationException>(() => _strategy.Fun_action(_image, new FakeDisplay()));

            Assert.AreEqual(default(Point2d), _strategy.inPara.ArcMidpoint);
        }

        [TestMethod]
        public void UnknownTransition_ThrowsClearError_ResetsPreviousMidpoint()
        {
            Assert.IsTrue(_strategy.Fun_action(_display, Strategies.Of()));

            _strategy.inPara.Transition = "未知";
            var ex = Assert.ThrowsException<ArgumentException>(() => _strategy.Fun_action(_display, Strategies.Of()));
            StringAssert.Contains(ex.Message, "过渡方向");
            StringAssert.Contains(ex.Message, "未知", "报错要带出写错的原值");
            StringAssert.Contains(ex.Message, _strategy.Name, "报错要带出工具名");

            Assert.AreEqual(default(Point2d), _strategy.inPara.ArcMidpoint);
        }

        [TestMethod]
        public void ImageIn_ResolvesUpstreamImage()
        {
            _strategy.inPara.ImageIn = "取像/图像";
            // 显示窗口里是全黑图, 只有上游那张有圆盘
            using (var black = ConstImage(Size, Size, 0))
            {
                _display.SetImage(black);
                Assert.IsTrue(_strategy.Fun_action(_display, Strategies.Of(new StubStrategy("取像").Output("图像", _image))));
            }

            Assert.AreEqual(150, _strategy.inPara.ArcMidpoint.X, 1.0);
            Assert.AreEqual(100, _strategy.inPara.ArcMidpoint.Y, 1.0);
        }

        [TestMethod]
        public void RegionIn_UpstreamRegionExcludingArc_Throws()
        {
            // 圆盘右缘在列 ~150；上游区域只到列 140。原先 measure_pos 忽略定义域，区域外的弧照样拟合成功
            using (var upstreamRegion = Rectangle1(0, 0, Size - 1, 140))
            {
                _strategy.inPara.RegionIn = "上游/区域";
                var upstream = new StubStrategy("上游").Output("区域", upstreamRegion);

                var ex = Assert.ThrowsException<InvalidOperationException>(() => _strategy.Fun_action(_display, Strategies.Of(upstream)));
                StringAssert.Contains(ex.Message, "未找到足够的轮廓点");
                Assert.AreEqual(default(Point2d), _strategy.inPara.ArcMidpoint);
            }
        }

        [TestMethod]
        public void RegionIn_UpstreamRegionCoveringArc_Fits()
        {
            using (var upstreamRegion = Rectangle1(0, 0, Size - 1, Size - 1))
            {
                _strategy.inPara.RegionIn = "上游/区域";
                var upstream = new StubStrategy("上游").Output("区域", upstreamRegion);

                Assert.IsTrue(_strategy.Fun_action(_display, Strategies.Of(upstream)));
                Assert.AreEqual(150, _strategy.inPara.ArcMidpoint.X, 1.0);
                Assert.AreEqual(100, _strategy.inPara.ArcMidpoint.Y, 1.0);
            }
        }

        [TestMethod]
        public void MaxErrAndCoarseGateZero_DisableFiltering()
        {
            // 两个门限都为 0 时粗滤门限也是 0：原先 RemoveOutliers 没有 <= 0 守卫，残差非零的点全被剔光
            _strategy.inPara.MaxErr = 0;
            _strategy.inPara.CoarseGate = 0;
            _strategy.inPara.TrimEnds = "否";

            Assert.IsTrue(_strategy.Fun_action(_display, Strategies.Of()));

            Assert.AreEqual(150, _strategy.inPara.ArcMidpoint.X, 1.0);
            Assert.AreEqual(0, _display.Points.Count(p => p.ColorName == HColor.Red.Name));
            StringAssert.Contains(_display.LastText, "用点:7");
        }

        [TestMethod]
        public void SigmaZero_FromUi_StillFits()
        {
            _strategy.inPara.Sigma = 0;

            Assert.IsTrue(_strategy.Fun_action(_display, Strategies.Of()));
            Assert.AreEqual(150, _strategy.inPara.ArcMidpoint.X, 1.0);
        }

        [TestMethod]
        public void RegionIn_Unresolvable_Throws()
        {
            _strategy.inPara.RegionIn = "上游/区域";
            Assert.ThrowsException<AlgoOutputNotFoundException>(() => _strategy.Fun_action(_display, Strategies.Of()));
        }

        [TestMethod]
        public void CoordIn_TranslatesRoi()
        {
            using (var image = DiskImage(Size + 40, Size, new Point2d(130, 100), Radius))
            {
                _display.SetImage(image);
                _strategy.inPara.CoordIn = "定位/坐标系";
                var locator = StubStrategy.Coord("定位", new Point2d(100, 100), new CvCoord(130, 100));

                Assert.IsTrue(_strategy.Fun_action(_display, Strategies.Of(locator)));

                Assert.AreEqual(180, _strategy.inPara.ArcMidpoint.X, 1.0);
                Assert.AreEqual(100, _strategy.inPara.ArcMidpoint.Y, 1.0);
            }
        }

        [TestMethod]
        public void CoordIn_RotatesAroundTmplPoint()
        {
            // 坐标系绕圆心转 -90°（HALCON 角度逆时针为正，行向下）：右侧弧段转到上方。
            _strategy.inPara.CoordIn = "定位/坐标系";
            var locator = StubStrategy.Coord("定位", DiskCenter, CvCoord.FromDegrees(100, 100, 90));

            Assert.IsTrue(_strategy.Fun_action(_display, Strategies.Of(locator)));

            var mid = _strategy.inPara.ArcMidpoint;
            Assert.AreEqual(100, mid.X, 1.0);
            Assert.AreEqual(50, mid.Y, 1.0, "逆时针 90° 后右侧 (150,100) 转到顶部 (100,50)");
        }

        [TestMethod]
        public void Outputs_AfterGenTreeNode()
        {
            var tree = new FakeTree();
            _strategy.GenTreeNode(tree);
            Assert.IsTrue(_strategy.Fun_action(_display, Strategies.Of()));

            CollectionAssert.IsSubsetOf(new[] { "圆弧中点/中点", "圆弧中点/中点/行", "圆弧中点/中点/列" }, tree.Paths);

            var mid = _strategy.inPara.ArcMidpoint;
            Assert.AreEqual(mid, Strategies.Of(_strategy).ResolveFrom<Point2d>("圆弧中点/中点"));
            Assert.AreEqual(mid.Y, _strategy.ResolveOutput<double>(new[] { "中点", "行" }));
            Assert.AreEqual(mid.X, _strategy.ResolveOutput<double>(new[] { "中点", "列" }));
        }

        [TestMethod]
        public void SavePara_CoarseGate_FallsBackTo15()
        {
            var ui = new FakeUiHost();
            _strategy.inPara.CoarseGate = 30;
            _strategy.DispPara(ui);
            ui.Values.Remove("cmb_114");

            _strategy.SavePara(ui);

            Assert.AreEqual(15.0, _strategy.inPara.CoarseGate, "读不到粗滤阈值时回落到默认 15");
        }

        [TestMethod]
        public void ParaRoundTrip()
        {
            var p = _strategy.inPara;
            p.CoarseGate = 30;
            p.MaxErr = 3;
            p.ContourType = "第二条边";
            p.DispFixRegion = true;

            var ui = new FakeUiHost();
            _strategy.DispPara(ui);
            using (var other = new FitArcMidpointStrategy())
            {
                other.SavePara(ui);
                Assert.AreEqual(30.0, other.inPara.CoarseGate);
                Assert.AreEqual(3, other.inPara.MaxErr);
                Assert.AreEqual("第二条边", other.inPara.ContourType);
                Assert.AreEqual("由白到黑", other.inPara.Transition);
                Assert.IsTrue(other.inPara.DispFixRegion);
            }
        }

        [TestMethod]
        public void Close_And_Dispose_AreSafeWithoutRenderData()
        {
            _strategy.Close(new FakeRoiHost());
            var roi = _strategy.inPara.HoRect;
            _strategy.Dispose();
            _strategy.Dispose();
            Assert.IsNull(roi.HoRegion);
        }
    }
}
