using System;
using System.Linq;
using System.Threading.Tasks;
using DotNet.Drawing;
using DotNet.HalconCore;
using HalconDotNet;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNet.HalconAlgo.Tests
{
    [TestClass]
    public class FitLineStrategyTests : HalconTestBase
    {
        private const int Size = 200;

        private HObject _image;
        private FakeDisplay _display;
        private FitLineStrategy _strategy;

        [TestInitialize]
        public void SetUp()
        {
            _image = VerticalStepImage(Size, Size, 100);
            _display = new FakeDisplay();
            _display.SetImage(_image);

            _strategy = new FitLineStrategy();
            // 沿列测量 60 宽，沿行步进 120 高：步数 6 → 13 个测量矩形，行 40..160
            _strategy.inPara.HoRect = NewAffRect(new Point2d(100, 100), 60, 120);
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
            Assert.AreEqual(AlgoEnum.FitLine, _strategy.Algorithm);
            Assert.AreEqual("拟合直线", _strategy.Name);
        }

        [TestMethod]
        public void FitsVerticalEdge_WithTrimEnds()
        {
            Assert.IsTrue(_strategy.Fun_action(_display, Strategies.Of()));

            var line = _strategy.inPara.Line;
            Assert.AreEqual(99.5, line.Start.X, 0.5);
            Assert.AreEqual(99.5, line.End.X, 0.5);
            // 首尾各裁掉一个点：剩余 11 点，行 50..150
            Assert.AreEqual(50, Math.Min(line.Start.Y, line.End.Y), 0.5);
            Assert.AreEqual(150, Math.Max(line.Start.Y, line.End.Y), 0.5);

            Assert.AreEqual(11, _display.Points.Count(p => p.ColorName == HColor.Green.Name), "参与拟合的点为绿色");
            Assert.AreEqual(2, _display.Points.Count(p => p.ColorName == HColor.Red.Name), "被裁剪的首尾点为红色");
            Assert.AreEqual(1, _display.Arrows.Count);
            Assert.AreEqual(1, _display.Objects.Count(o => o.ColorName == HColor.Blue.Name), "显示查找区域");
            StringAssert.Contains(_display.LastText, "用点:11");
        }

        [TestMethod]
        public void TrimEndsOff_UsesAllPoints()
        {
            _strategy.inPara.TrimEnds = "否";

            Assert.IsTrue(_strategy.Fun_action(_display, Strategies.Of()));

            var line = _strategy.inPara.Line;
            Assert.AreEqual(40, Math.Min(line.Start.Y, line.End.Y), 0.5);
            Assert.AreEqual(160, Math.Max(line.Start.Y, line.End.Y), 0.5);
            Assert.AreEqual(0, _display.Points.Count(p => p.ColorName == HColor.Red.Name));
            StringAssert.Contains(_display.LastText, "用点:13");
        }

        [TestMethod]
        public void DisplayFlags_SuppressOverlay()
        {
            _strategy.inPara.DispRegion = false;
            _strategy.inPara.DispFixPoint = false;
            _strategy.inPara.DispResult = false;
            _strategy.inPara.DispText = false;
            _strategy.inPara.DispFixRegion = true;

            Assert.IsTrue(_strategy.Fun_action(_display, Strategies.Of()));

            Assert.AreEqual(0, _display.Objects.Count);
            Assert.AreEqual(0, _display.Points.Count);
            Assert.AreEqual(0, _display.Arrows.Count);
            Assert.AreEqual(0, _display.Texts.Count);
            Assert.AreEqual(13, _display.Rect2Centers.Count, "拟合区域：每个测量矩形各画一次");
        }

        [TestMethod]
        public void ImageOverload_SetsDisplayImage()
        {
            var display = new FakeDisplay();
            Assert.IsTrue(_strategy.Fun_action(_image, display));
            Assert.AreSame(_image, display.HoImage);
            Assert.AreEqual(99.5, _strategy.inPara.Line.Start.X, 0.5);
        }

        [TestMethod]
        public void ImageOverload_IgnoresImageIn_UsesGivenImage()
        {
            // 单图重载没有上游: 原先转到另一重载按 ImageIn 取图, 明明给了图却抛 AlgoOutputNotFoundException
            _strategy.inPara.ImageIn = "上游/图像";
            var display = new FakeDisplay();

            Assert.IsTrue(_strategy.Fun_action(_image, display));

            Assert.AreSame(_image, display.HoImage);
            Assert.AreEqual(99.5, _strategy.inPara.Line.Start.X, 0.5);
        }

        [TestMethod]
        public void MaxErr_RejectsOutlierPoint()
        {
            // 行 97..103 处边缘被挖成列 115: 只有行 100 那个测量矩形(半宽 2.5)落在缺口里, 得到一个 ~15px 的离群点
            using (var notch = Rectangle1(97, 100, 103, 114))
            using (var notched = Paint(_image, notch, 0))
            {
                _display.SetImage(notched);
                _strategy.inPara.TrimEnds = "否";

                Assert.IsTrue(_strategy.Fun_action(_display, Strategies.Of()));
            }

            var line = _strategy.inPara.Line;
            Assert.AreEqual(99.5, line.Start.X, 0.5, "离群点被剔除后直线回到真实边缘");
            Assert.AreEqual(99.5, line.End.X, 0.5);

            var red = _display.Points.Where(p => p.ColorName == HColor.Red.Name).ToList();
            Assert.AreEqual(1, red.Count, "离群点以红色显示");
            Assert.AreEqual(114.5, red[0].Item.X, 0.5);
            Assert.AreEqual(100, red[0].Item.Y, 0.5);
            StringAssert.Contains(_display.LastText, "用点:12");
        }

        [TestMethod]
        public void MaxErr_Large_KeepsOutlierPoint()
        {
            using (var notch = Rectangle1(97, 100, 103, 114))
            using (var notched = Paint(_image, notch, 0))
            {
                _display.SetImage(notched);
                _strategy.inPara.TrimEnds = "否";
                _strategy.inPara.MaxErr = 100;

                Assert.IsTrue(_strategy.Fun_action(_display, Strategies.Of()));
            }

            Assert.AreEqual(0, _display.Points.Count(p => p.ColorName == HColor.Red.Name));
            StringAssert.Contains(_display.LastText, "用点:13");
        }

        [DataTestMethod]
        [DataRow("第一条边", 99.5)]
        [DataRow("第二条边", 129.5)]
        public void ContourType_SelectsEdge(string contourType, double expectedX)
        {
            // 亮带 100..119、暗带 120..129、其后再亮: 由黑到白的边缘在 99.5 与 129.5 各一条
            using (var dark = Rectangle1(0, 120, Size - 1, 129))
            using (var striped = Paint(_image, dark, 0))
            {
                _display.SetImage(striped);
                _strategy.inPara.HoRect.Dispose();
                _strategy.inPara.HoRect = NewAffRect(new Point2d(110, 100), 80, 120);
                _strategy.inPara.ContourType = contourType;

                Assert.IsTrue(_strategy.Fun_action(_display, Strategies.Of()));
            }

            Assert.AreEqual(expectedX, _strategy.inPara.Line.Start.X, 0.5);
            Assert.AreEqual(expectedX, _strategy.inPara.Line.End.X, 0.5);
        }

        [TestMethod]
        public void NoImage_Throws()
        {
            var ex = Assert.ThrowsException<InvalidOperationException>(() => _strategy.Fun_action(new FakeDisplay(), Strategies.Of()));
            StringAssert.Contains(ex.Message, "拟合直线");
        }

        [TestMethod]
        public void RoiNotDrawn_Throws()
        {
            _strategy.inPara.HoRect = new CvRegion { Type = RectEnum.AffRect };

            var ex = Assert.ThrowsException<InvalidOperationException>(() => _strategy.Fun_action(_display, Strategies.Of()));
            StringAssert.Contains(ex.Message, "尚未绘制 ROI");
        }

        [TestMethod]
        public void NoEdge_Throws()
        {
            _strategy.inPara.Transition = "由白到黑";

            var ex = Assert.ThrowsException<InvalidOperationException>(() => _strategy.Fun_action(_display, Strategies.Of()));
            StringAssert.Contains(ex.Message, "未找到足够的轮廓点");
        }

        [TestMethod]
        public void Failure_ResetsPreviousLine()
        {
            Assert.IsTrue(_strategy.Fun_action(_display, Strategies.Of()));
            Assert.IsFalse(_strategy.inPara.Line.IsDegenerate);

            _strategy.inPara.Transition = "由白到黑";
            Assert.ThrowsException<InvalidOperationException>(() => _strategy.Fun_action(_display, Strategies.Of()));

            // 宿主吞掉异常后下游照跑: 此时必须拿到退化直线(下游会明确报错), 而不是上一轮的旧直线
            Assert.IsTrue(_strategy.inPara.Line.IsDegenerate);
        }

        [TestMethod]
        public void UnknownTransition_ThrowsClearError_ResetsPreviousLine()
        {
            Assert.IsTrue(_strategy.Fun_action(_display, Strategies.Of()));

            // job 文件里手改 / 旧版本留下的非法值: 必须报出配置错误, 而不是 HALCON 原生异常
            _strategy.inPara.Transition = "未知";
            var ex = Assert.ThrowsException<ArgumentException>(() => _strategy.Fun_action(_display, Strategies.Of()));
            StringAssert.Contains(ex.Message, "过渡方向");
            StringAssert.Contains(ex.Message, "未知", "报错要带出写错的原值");
            StringAssert.Contains(ex.Message, _strategy.Name, "报错要带出工具名");

            Assert.IsTrue(_strategy.inPara.Line.IsDegenerate);
        }

        [TestMethod]
        public void NoImage_ResetsPreviousLine()
        {
            Assert.IsTrue(_strategy.Fun_action(_display, Strategies.Of()));

            Assert.ThrowsException<InvalidOperationException>(() => _strategy.Fun_action(new FakeDisplay(), Strategies.Of()));

            Assert.IsTrue(_strategy.inPara.Line.IsDegenerate);
        }

        [TestMethod]
        public void ImageIn_ResolvesUpstreamImage()
        {
            using (var shifted = VerticalStepImage(Size, Size, 110))
            {
                _strategy.inPara.ImageIn = "取像/图像";
                var upstream = new StubStrategy("取像").Output("图像", shifted);

                Assert.IsTrue(_strategy.Fun_action(_display, Strategies.Of(upstream)));
                Assert.AreEqual(109.5, _strategy.inPara.Line.Start.X, 0.5, "应使用上游图像而不是窗口图像");
            }
        }

        [TestMethod]
        public void RegionIn_UsesUpstreamRegionButLocalGeometry()
        {
            using (var upstreamRegion = Rectangle1(0, 0, Size - 1, Size - 1))
            {
                _strategy.inPara.RegionIn = "区域源/区域";
                var upstream = new StubStrategy("区域源").Output("区域", upstreamRegion);

                Assert.IsTrue(_strategy.Fun_action(_display, Strategies.Of(upstream)));
                Assert.AreEqual(99.5, _strategy.inPara.Line.Start.X, 0.5);
                Assert.AreSame(upstreamRegion, _display.Objects.Single().Item, "显示的查找区域就是上游区域");
            }
        }

        [TestMethod]
        public void RegionIn_UpstreamRegionExcludingEdge_Throws()
        {
            // measure_pos 忽略定义域：原先上游区域只影响显示，区域外的边照样被找到并拟合成功
            using (var upstreamRegion = Rectangle1(0, 0, Size - 1, 90))
            {
                _strategy.inPara.RegionIn = "区域源/区域";
                var upstream = new StubStrategy("区域源").Output("区域", upstreamRegion);

                var ex = Assert.ThrowsException<InvalidOperationException>(() => _strategy.Fun_action(_display, Strategies.Of(upstream)));
                StringAssert.Contains(ex.Message, "未找到足够的轮廓点");
                Assert.IsTrue(_strategy.inPara.Line.IsDegenerate);
            }
        }

        [TestMethod]
        public void RegionIn_UpstreamRegion_LimitsPointsToRegion()
        {
            // 上游区域只覆盖行 0..100：测量矩形行 40..100 共 7 个有点，裁剪首尾后行 50..90
            using (var upstreamRegion = Rectangle1(0, 0, 100, Size - 1))
            {
                _strategy.inPara.RegionIn = "区域源/区域";
                var upstream = new StubStrategy("区域源").Output("区域", upstreamRegion);

                Assert.IsTrue(_strategy.Fun_action(_display, Strategies.Of(upstream)));

                var line = _strategy.inPara.Line;
                Assert.AreEqual(50, Math.Min(line.Start.Y, line.End.Y), 0.5);
                Assert.AreEqual(90, Math.Max(line.Start.Y, line.End.Y), 0.5);
                StringAssert.Contains(_display.LastText, "用点:5");
            }
        }

        [TestMethod]
        public void SigmaZero_FromUi_StillFits()
        {
            // "滤波"下拉提供 0：原先直接传给 measure_pos 抛 HALCON #1302
            _strategy.inPara.Sigma = 0;

            Assert.IsTrue(_strategy.Fun_action(_display, Strategies.Of()));
            Assert.AreEqual(99.5, _strategy.inPara.Line.Start.X, 0.5);
        }

        [TestMethod]
        public void MaxErrZero_DisablesRefinement()
        {
            _strategy.inPara.MaxErr = 0;
            _strategy.inPara.TrimEnds = "否";

            Assert.IsTrue(_strategy.Fun_action(_display, Strategies.Of()));
            StringAssert.Contains(_display.LastText, "用点:13");
            Assert.AreEqual(0, _display.Points.Count(p => p.ColorName == HColor.Red.Name));
        }

        [TestMethod]
        public void RegionIn_Unresolvable_Throws()
        {
            _strategy.inPara.RegionIn = "区域源/区域";
            Assert.ThrowsException<AlgoOutputNotFoundException>(() => _strategy.Fun_action(_display, Strategies.Of()));
        }

        [TestMethod]
        public void CoordIn_TranslatesRoi()
        {
            using (var image = VerticalStepImage(Size, Size, 130))
            {
                _display.SetImage(image);
                _strategy.inPara.CoordIn = "定位/坐标系";
                var locator = StubStrategy.Coord("定位", new Point2d(100, 100), new CvCoord(130, 100));

                Assert.IsTrue(_strategy.Fun_action(_display, Strategies.Of(locator)));

                Assert.AreEqual(129.5, _strategy.inPara.Line.Start.X, 0.5, "ROI 应随坐标系平移 +30 列");
                Assert.AreEqual(129.5, _strategy.inPara.Line.End.X, 0.5);
            }
        }

        [TestMethod]
        public void CoordIn_RotatesMeasureDirection()
        {
            // 水平阶跃边在行 100；配置态 ROI 沿列测量，坐标系转 90° 后改为沿行测量。
            using (var dark = ConstImage(Size, Size, 0))
            using (var bottom = Rectangle1(100, 0, Size - 1, Size - 1))
            using (var image = Paint(dark, bottom, 255))
            {
                _display.SetImage(image);
                _strategy.inPara.Transition = "全部";
                _strategy.inPara.CoordIn = "定位/坐标系";
                var locator = StubStrategy.Coord("定位", new Point2d(100, 100), CvCoord.FromDegrees(100, 100, 90));

                Assert.IsTrue(_strategy.Fun_action(_display, Strategies.Of(locator)));

                var line = _strategy.inPara.Line;
                Assert.AreEqual(99.5, line.Start.Y, 0.5);
                Assert.AreEqual(99.5, line.End.Y, 0.5);
                Assert.AreEqual(100, Math.Abs(line.End.X - line.Start.X), 1, "11 个点沿列展开 100 像素");
            }
        }

        [TestMethod]
        public void CoordIn_RotationWithTranslation_RotatesAroundTmplPointThenMoves()
        {
            // 示教原点 (100,100) → 当前 (130,120) 且转 90°：ROI 中心本就在示教原点，跟随后落在 (130,120)，
            // 改为沿行测量；水平阶跃边在行 120。旋转中心或平移顺序写错时 ROI 会落在别处而找不到边。
            using (var dark = ConstImage(Size, Size, 0))
            using (var bottom = Rectangle1(120, 0, Size - 1, Size - 1))
            using (var image = Paint(dark, bottom, 255))
            {
                _display.SetImage(image);
                _strategy.inPara.Transition = "全部";
                _strategy.inPara.CoordIn = "定位/坐标系";
                var locator = StubStrategy.Coord("定位", new Point2d(100, 100), CvCoord.FromDegrees(130, 120, 90));

                Assert.IsTrue(_strategy.Fun_action(_display, Strategies.Of(locator)));

                var line = _strategy.inPara.Line;
                Assert.AreEqual(119.5, line.Start.Y, 0.5);
                Assert.AreEqual(119.5, line.End.Y, 0.5);
                Assert.AreEqual(130, (line.Start.X + line.End.X) / 2, 1, "点沿列以跟随后的中心 130 对称展开");
                Assert.AreEqual(100, Math.Abs(line.End.X - line.Start.X), 1);
            }
        }

        [TestMethod]
        public void CoordIn_MissingTmplPoint_Throws()
        {
            _strategy.inPara.CoordIn = "定位/坐标系";
            var locator = new StubStrategy("定位").Output("坐标系", new CvCoord(130, 100));

            Assert.ThrowsException<AlgoOutputNotFoundException>(() => _strategy.Fun_action(_display, Strategies.Of(locator)));
        }

        [TestMethod]
        public void Outputs_AfterGenTreeNode()
        {
            var tree = new FakeTree();
            _strategy.GenTreeNode(tree);
            Assert.IsTrue(_strategy.Fun_action(_display, Strategies.Of()));

            CollectionAssert.IsSubsetOf(new[] { "拟合直线", "拟合直线/直线", "拟合直线/直线/起点/行", "拟合直线/直线/终点/列" }, tree.Paths);

            var line = _strategy.ResolveOutput<CvLine>(new[] { "直线" });
            Assert.AreEqual(line.Start, _strategy.ResolveOutput<Point2d>(new[] { "直线", "起点" }));
            Assert.AreEqual(line.End, _strategy.ResolveOutput<Point2d>(new[] { "直线", "终点" }));
            Assert.AreEqual(line.Start.Y, _strategy.ResolveOutput<double>(new[] { "直线", "起点", "行" }));
            Assert.AreEqual(line.Start.X, _strategy.ResolveOutput<double>(new[] { "直线", "起点", "列" }));
            Assert.AreEqual(line.End.Y, _strategy.ResolveOutput<double>(new[] { "直线", "终点", "行" }));
            Assert.AreEqual(line.End.X, _strategy.ResolveOutput<double>(new[] { "直线", "终点", "列" }));

            // 下游经 ResolveFrom 取到同一条线
            Assert.AreEqual(line, Strategies.Of(_strategy).ResolveFrom<CvLine>("拟合直线/直线"));
        }

        [TestMethod]
        public void Outputs_BeforeGenTreeNode_AreNotRegistered()
        {
            Assert.IsNull(_strategy.ResolveOutput(new[] { "直线" }));
        }

        [TestMethod]
        public void ParaRoundTrip()
        {
            var p = _strategy.inPara;
            p.CoordIn = "定位/坐标系";
            p.ImageIn = "取像/图像";
            p.RegionIn = "区域源/区域";
            p.Transition = "全部";
            p.ContourType = "最后一条";
            p.Sigma = 2;
            p.Threshold = 33;
            p.StepPace = 7;
            p.StepWidth = 3;
            p.MaxErr = 9;
            p.TrimEnds = "否";
            p.DispText = false;
            p.DispRegion = false;
            p.DispFixRegion = true;
            p.DispFixPoint = false;
            p.DispResult = false;
            p.FontX = 20;
            p.FontY = 21;
            p.FontSize = 30;

            var ui = new FakeUiHost();
            _strategy.DispPara(ui);
            CollectionAssert.AreEqual(new[] { TabPageEnum.Parameter, TabPageEnum.Region, TabPageEnum.Display }, ui.Tabs);

            var other = new FitLineStrategy();
            other.SavePara(ui);
            var q = other.inPara;
            Assert.AreEqual(p.CoordIn, q.CoordIn);
            Assert.AreEqual(p.ImageIn, q.ImageIn);
            Assert.AreEqual(p.RegionIn, q.RegionIn);
            Assert.AreEqual(p.Transition, q.Transition);
            Assert.AreEqual(p.ContourType, q.ContourType);
            Assert.AreEqual(p.Sigma, q.Sigma);
            Assert.AreEqual(p.Threshold, q.Threshold);
            Assert.AreEqual(p.StepPace, q.StepPace);
            Assert.AreEqual(p.StepWidth, q.StepWidth);
            Assert.AreEqual(p.MaxErr, q.MaxErr);
            Assert.AreEqual(p.TrimEnds, q.TrimEnds);
            Assert.AreEqual(p.DispText, q.DispText);
            Assert.AreEqual(p.DispRegion, q.DispRegion);
            Assert.AreEqual(p.DispFixRegion, q.DispFixRegion);
            Assert.AreEqual(p.DispFixPoint, q.DispFixPoint);
            Assert.AreEqual(p.DispResult, q.DispResult);
            Assert.AreEqual(p.FontX, q.FontX);
            Assert.AreEqual(p.FontY, q.FontY);
            Assert.AreEqual(p.FontSize, q.FontSize);
            other.Dispose();
        }

        [TestMethod]
        public async Task DrawROIAsync_Cancelled_RestoresType()
        {
            var host = new FakeRoiHost { Confirm = false };

            await _strategy.DrawROIAsync(host, RectEnum.Circle, true);

            Assert.AreEqual(RectEnum.Circle, host.TypeDuringDraw, "绘制前要先写入 Type");
            Assert.AreEqual(RectEnum.AffRect, _strategy.inPara.HoRect.Type, "取消后 Type 还原");
            Assert.AreEqual(1, host.FakeDisplay.Regions.Count, "取消后仍把原 ROI 画回去");
            Assert.AreEqual(1, host.SetRectParaCount);
        }

        [TestMethod]
        public async Task DrawROIAsync_Confirmed_KeepsNewType()
        {
            var host = new FakeRoiHost();

            await _strategy.DrawROIAsync(host, RectEnum.Rectangle, true);

            Assert.AreEqual(RectEnum.Rectangle, _strategy.inPara.HoRect.Type);
            Assert.AreEqual(1, host.DrawCount);
        }

        [TestMethod]
        public async Task DrawROIAsync_Modify_UsesModApi()
        {
            var host = new FakeRoiHost();

            await _strategy.DrawROIAsync(host, RectEnum.Rectangle, false);

            Assert.AreEqual(0, host.DrawCount);
            Assert.AreEqual(1, host.DrawModCount);
            Assert.AreEqual(RectEnum.AffRect, _strategy.inPara.HoRect.Type, "修改模式不改 Type");
        }

        [TestMethod]
        public void DispROI_ForcesAffRect()
        {
            _strategy.inPara.HoRect.Type = RectEnum.Circle;
            var host = new FakeRoiHost();

            _strategy.DispROI(host);

            Assert.AreEqual(RectEnum.AffRect, _strategy.inPara.HoRect.Type);
            Assert.AreEqual(1, host.SetRectParaCount);
        }

        [TestMethod]
        public void Dispose_IsIdempotent_AndReleasesRoi()
        {
            var roi = _strategy.inPara.HoRect;
            _strategy.Dispose();
            _strategy.Dispose();
            Assert.IsNull(roi.HoRegion);
        }
    }
}
