using System;
using System.Collections.Generic;
using System.IO;
using DotNet.Drawing;
using DotNet.Vision.Abstractions;
using HalconDotNet;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNet.HalconAlgo.Tests
{
    /// <summary>
    /// 形状 / 灰度 / 缩放 / 通用四种匹配的公共行为：流程完全相同，只有 HALCON 算子不同。
    /// 子类只提供参数访问器，测试方法由 MSTest 从基类继承执行。
    /// </summary>
    public abstract class MatchingStrategyTestBase<TStrategy> : HalconTestBase
        where TStrategy : IParaStrategy, IParaBinding, ITreeNodeProvider, IRoiEditable, ITemplateEditable, new()
    {
        private const int W = 200, H = 160;

        /// <summary>模板框：左上 (35,25)，80×80 → 中心 (75,65)，距图像边界留足 SaveSmallestRectImage 的 20px 外扩。</summary>
        private static readonly Rect2d TemplateBounds = new Rect2d(35.0, 25.0, 80.0, 80.0);
        private static readonly Point2d TemplateCenter = new Point2d(75, 65);

        private string _savedProjectDir;
        private string _projectDir;
        private HObject _image;
        protected TStrategy Strategy;
        private FakeDisplay _display;

        protected abstract string ExpectedName { get; }
        protected abstract CvRegion ModeRect(TStrategy s);
        protected abstract CvRegion HoRect(TStrategy s);
        protected abstract HTuple ModelID(TStrategy s);
        protected abstract Point2d TmplPoint(TStrategy s);
        protected abstract CvCoord Coord(TStrategy s);
        protected abstract List<ModelResult> Results(TStrategy s);
        protected abstract string ModelPath(TStrategy s);
        protected abstract void ClearModel(HTuple modelId);

        /// <summary>暗背景上的亮 L 形：竖条行 40..90 × 列 50..60，横条行 80..90 × 列 50..100，整体平移 (dx, dy)。</summary>
        private static HObject LImage(int dx = 0, int dy = 0)
        {
            using (var dark = ConstImage(W, H, 0))
            using (var bar1 = Rectangle1(40 + dy, 50 + dx, 90 + dy, 60 + dx))
            using (var bar2 = Rectangle1(80 + dy, 50 + dx, 90 + dy, 100 + dx))
            using (var step = Paint(dark, bar1, 255))
            {
                return Paint(step, bar2, 255);
            }
        }

        [TestInitialize]
        public void SetUpMatching()
        {
            _savedProjectDir = AlgoPaths.ProjectDir;
            _projectDir = Path.Combine(Path.GetTempPath(), "DotNet.HalconAlgo.Tests", Guid.NewGuid().ToString("N"));
            AlgoPaths.ProjectDir = _projectDir;

            _image = LImage();
            _display = new FakeDisplay();
            Strategy = new TStrategy { RunIndex = 3 };
        }

        [TestCleanup]
        public void TearDownMatching()
        {
            var id = ModelID(Strategy);
            if (id != null && id.Length > 0) ClearModel(id);
            ModeRect(Strategy).Dispose();
            HoRect(Strategy).Dispose();
            _image.Dispose();

            AlgoPaths.ProjectDir = _savedProjectDir;
            if (Directory.Exists(_projectDir)) Directory.Delete(_projectDir, true);
        }

        private FakeRoiHost TemplateHost(bool confirm = true)
        {
            var host = new FakeRoiHost
            {
                Confirm = confirm,
                OnDraw = r =>
                {
                    r.Bounds = TemplateBounds;
                    r.RebuildRegion();
                },
            };
            host.FakeDisplay.SetImage(_image);
            return host;
        }

        private protected FakeRoiHost CreateTemplate()
        {
            var host = TemplateHost();
            Strategy.SetTemplateAsync(host, RectEnum.Rectangle, true).GetAwaiter().GetResult();
            return host;
        }

        /// <summary>替换查找 ROI，并释放原 ROI 的区域句柄。</summary>
        private protected void ReplaceHoRect(CvRegion region)
        {
            HoRect(Strategy).Dispose();
            SetHoRect(region);
        }

        /// <summary>整图查找 ROI。</summary>
        private protected void UseFullImageRoi() => ReplaceHoRect(NewRegion(RectEnum.Rectangle, 0, 0, W - 1, H - 1));

        [TestMethod]
        public void Name_IsDefault() => Assert.AreEqual(ExpectedName, Strategy.Name);

        [TestMethod]
        public void SetTemplate_CreatesModel_TeachesTmplPoint_SavesImage()
        {
            var host = CreateTemplate();

            Assert.IsNotNull(ModelID(Strategy));
            Assert.IsTrue(ModelID(Strategy).Length > 0);
            Assert.AreEqual(RectEnum.Rectangle, ModeRect(Strategy).Type);

            var tmpl = TmplPoint(Strategy);
            Assert.AreEqual(TemplateCenter.X, tmpl.X, 1.0, "模板原点取模板区域重心");
            Assert.AreEqual(TemplateCenter.Y, tmpl.Y, 1.0);
            Assert.AreEqual(tmpl, Coord(Strategy).Center);
            Assert.AreEqual(1, Results(Strategy).Count);

            string expectedPath = Path.Combine(AlgoPaths.JobDir, "3", "matching.bmp");
            Assert.AreEqual(expectedPath, ModelPath(Strategy));
            Assert.AreEqual(expectedPath, host.DonePath);
            Assert.IsTrue(File.Exists(expectedPath), "模板图应落盘到 JobDir/RunIndex/matching.bmp");
            Assert.IsTrue(host.DoneResult.HasValue);
            Assert.AreEqual(1, host.SetModelParaCount);

            Assert.AreEqual("新建模板成功！", host.FakeDisplay.LastText);
            Assert.AreEqual(HColor.Green.Name, host.FakeDisplay.Texts[0].ColorName);
            Assert.AreEqual(HColor.Orange.Name, host.FakeDisplay.Regions[0].ColorName);
        }

        [TestMethod]
        public void SetTemplate_Cancel_HasNoSideEffects()
        {
            var host = TemplateHost(confirm: false);

            Strategy.SetTemplateAsync(host, RectEnum.Circle, true).GetAwaiter().GetResult();

            Assert.AreEqual(RectEnum.Circle, host.TypeDuringDraw);
            Assert.AreEqual(RectEnum.Rectangle, ModeRect(Strategy).Type, "取消后类型还原");
            Assert.IsNull(ModelID(Strategy));
            Assert.AreEqual(string.Empty, ModelPath(Strategy));
            Assert.AreEqual(new Point2d(), TmplPoint(Strategy));
            Assert.IsNull(host.DonePath);
            Assert.AreEqual(0, host.SetModelParaCount);
            Assert.AreEqual(1, host.FakeDisplay.Regions.Count, "原模板区域重画回去");
            Assert.AreEqual(HColor.Orange.Name, host.FakeDisplay.Regions[0].ColorName);
            Assert.IsFalse(Directory.Exists(_projectDir));
        }

        [TestMethod]
        public void SetTemplate_Cancel_KeepsExistingModel()
        {
            CreateTemplate();
            var id = ModelID(Strategy);
            var tmpl = TmplPoint(Strategy);

            Strategy.SetTemplateAsync(TemplateHost(confirm: false), RectEnum.Circle, false).GetAwaiter().GetResult();

            Assert.AreSame(id, ModelID(Strategy));
            Assert.AreEqual(tmpl, TmplPoint(Strategy));
        }

        [TestMethod]
        public void SetTemplate_Modify_UsesModDraw()
        {
            var host = TemplateHost();
            Strategy.SetTemplateAsync(host, RectEnum.Rectangle, false).GetAwaiter().GetResult();

            Assert.AreEqual(0, host.DrawCount);
            Assert.AreEqual(1, host.DrawModCount);
            Assert.IsNotNull(ModelID(Strategy));
        }

        [TestMethod]
        public void SetTemplate_NoImage_Throws()
        {
            var host = TemplateHost();
            host.FakeDisplay.HoImage = null;

            var ex = Assert.ThrowsException<InvalidOperationException>(
                () => Strategy.SetTemplateAsync(host, RectEnum.Rectangle, true).GetAwaiter().GetResult());
            StringAssert.Contains(ex.Message, ExpectedName);
        }

        [TestMethod]
        public void Run_FindsShiftedTarget()
        {
            CreateTemplate();
            var tmpl = TmplPoint(Strategy);
            UseFullImageRoi();

            using (var shifted = LImage(dx: 30, dy: 20))
            {
                _display.SetImage(shifted);
                Assert.IsTrue(Strategy.Fun_action(_display, Strategies.Of()));
            }

            Assert.AreEqual(1, Results(Strategy).Count);
            var coord = Coord(Strategy);
            Assert.AreEqual(tmpl.X + 30, coord.X, 1.0);
            Assert.AreEqual(tmpl.Y + 20, coord.Y, 1.0);
            Assert.AreEqual(0, coord.AngleDegrees, 1.0);
            Assert.IsTrue(Results(Strategy)[0].Score > 0.9);

            StringAssert.StartsWith(_display.LastText, ExpectedName + " : 数量:1 最佳得分:");
            Assert.AreEqual(HColor.Green.Name, _display.Texts[0].ColorName);
            Assert.AreEqual(1, _display.Objects.FindAll(o => o.ColorName == HColor.Blue.Name).Count, "查找区域");
            Assert.AreEqual(1, _display.Objects.FindAll(o => o.ColorName == HColor.Green.Name).Count, "模板轮廓");
            Assert.AreEqual(1, _display.Coords.Count, "匹配点");
        }

        [TestMethod]
        public void Run_Outputs_ResolveFromTree()
        {
            CreateTemplate();
            UseFullImageRoi();
            Strategy.GenTreeNode(new FakeTree());
            _display.SetImage(_image);
            Assert.IsTrue(Strategy.Fun_action(_display, Strategies.Of()));

            var all = Strategies.Of(Strategy);
            string coordPath = ExpectedName + "/坐标系";
            Assert.AreEqual(Coord(Strategy), all.ResolveFrom<CvCoord>(coordPath));
            Assert.AreEqual(TmplPoint(Strategy), all.ResolveFrom<Point2d>(coordPath.ToTmplPoint()));
            Assert.AreEqual(Coord(Strategy).Y, all.ResolveFrom<double>(coordPath + "/原点/行"));
            Assert.AreEqual(Coord(Strategy).Angle.Radians, all.ResolveFrom<double>(coordPath + "/角度"));
        }

        [TestMethod]
        public void Run_NoMatch_RedText_ResetsPreviousResult()
        {
            CreateTemplate();
            UseFullImageRoi();
            _display.SetImage(_image);
            Assert.IsTrue(Strategy.Fun_action(_display, Strategies.Of()));
            Assert.AreEqual(1, Results(Strategy).Count);

            using (var blank = ConstImage(W, H, 0))
            {
                _display.SetImage(blank);
                Assert.IsTrue(Strategy.Fun_action(_display, Strategies.Of()));
            }

            Assert.AreEqual(0, Results(Strategy).Count);
            Assert.AreEqual(new CvCoord(), Coord(Strategy), "没有匹配时不能留着上一轮的坐标系");
            StringAssert.Contains(_display.LastText, "数量:0");
            Assert.AreEqual(HColor.Red.Name, _display.Texts[_display.Texts.Count - 1].ColorName);
        }

        [TestMethod]
        public void Run_RoiNotDrawn_ReturnsFalse_ResetsResult()
        {
            CreateTemplate();
            Assert.AreNotEqual(new CvCoord(), Coord(Strategy));
            _display.SetImage(_image);

            Assert.IsFalse(Strategy.Fun_action(_display, Strategies.Of()));

            Assert.AreEqual($"{ExpectedName} : 尚未绘制 ROI，无法执行匹配！", _display.LastText);
            Assert.AreEqual(HColor.Red.Name, _display.Texts[0].ColorName);
            Assert.AreEqual(0, Results(Strategy).Count);
            Assert.AreEqual(new CvCoord(), Coord(Strategy));
        }

        [TestMethod]
        public void Run_NoImage_Throws()
        {
            // 未建模板的校验先于取图, 要走到取图这一步必须先有模板
            CreateTemplate();
            var ex = Assert.ThrowsException<InvalidOperationException>(() => Strategy.Fun_action(new FakeDisplay(), Strategies.Of()));
            StringAssert.Contains(ex.Message, ExpectedName);
        }

        [TestMethod]
        public void Run_RegionIn_Unresolvable_Throws()
        {
            CreateTemplate();
            SetRegionIn("上游/区域");
            _display.SetImage(_image);
            Assert.ThrowsException<AlgoOutputNotFoundException>(() => Strategy.Fun_action(_display, Strategies.Of()));
        }

        [TestMethod]
        public void Run_WithoutModel_ReturnsFalse_RedText_ResetsResult()
        {
            CreateTemplate();
            Assert.AreEqual(1, Results(Strategy).Count);
            ClearModel(ModelID(Strategy));
            SetModelID(null);
            UseFullImageRoi();
            _display.SetImage(_image);

            // 原先 null 的 ModelID 会直接传进查找算子, 报出与真实原因无关的 HALCON 参数错误
            Assert.IsFalse(Strategy.Fun_action(_display, Strategies.Of()));

            StringAssert.StartsWith(_display.LastText, ExpectedName + " : 未建立模板");
            Assert.AreEqual(HColor.Red.Name, _display.Texts[0].ColorName);
            Assert.AreEqual(0, Results(Strategy).Count, "不能留着上一轮的匹配结果");
            Assert.AreEqual(new CvCoord(), Coord(Strategy));
        }

        [TestMethod]
        public void SetTemplate_Repeated_ReplacesModel_StillMatches()
        {
            CreateTemplate();
            var first = ModelID(Strategy);

            CreateTemplate();

            Assert.AreNotSame(first, ModelID(Strategy), "重建模板必须换成新句柄(旧句柄已释放)");
            UseFullImageRoi();
            _display.SetImage(_image);
            Assert.IsTrue(Strategy.Fun_action(_display, Strategies.Of()));
            Assert.AreEqual(1, Results(Strategy).Count);
            Assert.AreEqual(TmplPoint(Strategy).X, Coord(Strategy).X, 1.0);
            Assert.AreEqual(TmplPoint(Strategy).Y, Coord(Strategy).Y, 1.0);
        }

        [TestMethod]
        public void SetTemplate_TrialNoMatch_RedText_KeepsPreviousModel()
        {
            CreateTemplate();
            var id = ModelID(Strategy);
            var tmpl = TmplPoint(Strategy);

            // 新模板只搜 85°..95° 且要求 0.9 分: 未旋转的 L 形转 90° 后最多与自身一条边重合, 试匹配必然 0 个结果
            SetSearchRange(85, 10, 0.9);
            var host = TemplateHost();
            Strategy.SetTemplateAsync(host, RectEnum.Rectangle, false).GetAwaiter().GetResult();

            Assert.AreEqual("新建模板失败！", host.FakeDisplay.LastText);
            Assert.AreEqual(HColor.Red.Name, host.FakeDisplay.Texts[0].ColorName);
            Assert.AreSame(id, ModelID(Strategy), "试匹配失败不能丢掉旧模板");
            Assert.AreEqual(tmpl, TmplPoint(Strategy), "旧模板与旧示教原点必须仍是一对");
            Assert.IsNull(host.DonePath);
            Assert.AreEqual(0, host.SetModelParaCount);

            // 旧模板句柄仍然可用: 恢复查找参数后照常匹配到示教位置
            SetSearchRange(-90, 180, 0.6);
            UseFullImageRoi();
            _display.SetImage(_image);
            Assert.IsTrue(Strategy.Fun_action(_display, Strategies.Of()));
            Assert.AreEqual(1, Results(Strategy).Count);
            Assert.AreEqual(tmpl.X, Coord(Strategy).X, 1.0);
            Assert.AreEqual(tmpl.Y, Coord(Strategy).Y, 1.0);
        }

        [TestMethod]
        public void DrawROIAsync_Cancel_RestoresType_StillRedraws()
        {
            var host = new FakeRoiHost { Confirm = false };

            Strategy.DrawROIAsync(host, RectEnum.Circle, true).GetAwaiter().GetResult();

            Assert.AreEqual(RectEnum.Rectangle, HoRect(Strategy).Type);
            Assert.AreEqual(1, host.FakeDisplay.Regions.Count);
            Assert.AreEqual(1, host.SetRectParaCount);
        }

        [TestMethod]
        public void DispROI_PushesModelPara()
        {
            var host = new FakeRoiHost();
            Strategy.DispROI(host);
            Assert.AreEqual(1, host.SetModelParaCount);
            Assert.AreEqual(0, host.SetRectParaCount);
        }

        [TestMethod]
        public void ParaRoundTrip_NumMatchesMany()
        {
            var ui = new FakeUiHost();
            Strategy.DispPara(ui);
            ui.Set("cmb_110", "多个").Set("cmb_111", "0.75").Set("cmb_102", "-45");

            var other = new TStrategy();
            other.SavePara(ui);
            try
            {
                Assert.AreEqual(0, NumMatches(other), "\"多个\" 映射为 0（找全部）");
                Assert.AreEqual(0.75, MinScore(other), 1e-12);
                Assert.AreEqual(-45, AngleStart(other), 1e-12);

                var back = new FakeUiHost();
                other.DispPara(back);
                Assert.AreEqual("多个", back.Values["cmb_110"]);
            }
            finally
            {
                ModeRect(other).Dispose();
                HoRect(other).Dispose();
            }
        }

        protected abstract void SetHoRect(CvRegion region);
        protected abstract void SetModelID(HTuple modelId);
        protected abstract void SetRegionIn(string path);
        protected abstract void SetSearchRange(double angleStartDeg, double angleExtentDeg, double minScore);
        protected abstract int NumMatches(TStrategy s);
        protected abstract double MinScore(TStrategy s);
        protected abstract double AngleStart(TStrategy s);
    }

    [TestClass]
    public class ShapeModelStrategyTests : MatchingStrategyTestBase<ShapeModelStrategy>
    {
        protected override string ExpectedName => "形状匹配";
        protected override CvRegion ModeRect(ShapeModelStrategy s) => s.inPara.ModeRect;
        protected override CvRegion HoRect(ShapeModelStrategy s) => s.inPara.HoRect;
        protected override HTuple ModelID(ShapeModelStrategy s) => s.inPara.ModelID;
        protected override Point2d TmplPoint(ShapeModelStrategy s) => s.inPara.TmplPoint;
        protected override CvCoord Coord(ShapeModelStrategy s) => s.inPara.Coord;
        protected override List<ModelResult> Results(ShapeModelStrategy s) => s.inPara.Results;
        protected override string ModelPath(ShapeModelStrategy s) => s.inPara.ModelPath;
        protected override void ClearModel(HTuple modelId) => HOperatorSet.ClearShapeModel(modelId);
        protected override void SetHoRect(CvRegion region) => Strategy.inPara.HoRect = region;
        protected override void SetRegionIn(string path) => Strategy.inPara.RegionIn = path;
        protected override void SetSearchRange(double angleStartDeg, double angleExtentDeg, double minScore)
        {
            Strategy.inPara.AngleStart = angleStartDeg;
            Strategy.inPara.AngleExtent = angleExtentDeg;
            Strategy.inPara.MinScore = minScore;
        }
        protected override void SetModelID(HTuple modelId) => Strategy.inPara.ModelID = modelId;
        protected override int NumMatches(ShapeModelStrategy s) => s.inPara.NumMatches.I;
        protected override double MinScore(ShapeModelStrategy s) => s.inPara.MinScore.D;
        protected override double AngleStart(ShapeModelStrategy s) => s.inPara.AngleStart.D;
    }

    [TestClass]
    public class NccModelStrategyTests : MatchingStrategyTestBase<NccModelStrategy>
    {
        protected override string ExpectedName => "灰度匹配";
        protected override CvRegion ModeRect(NccModelStrategy s) => s.inPara.ModeRect;
        protected override CvRegion HoRect(NccModelStrategy s) => s.inPara.HoRect;
        protected override HTuple ModelID(NccModelStrategy s) => s.inPara.ModelID;
        protected override Point2d TmplPoint(NccModelStrategy s) => s.inPara.TmplPoint;
        protected override CvCoord Coord(NccModelStrategy s) => s.inPara.Coord;
        protected override List<ModelResult> Results(NccModelStrategy s) => s.inPara.Results;
        protected override string ModelPath(NccModelStrategy s) => s.inPara.ModelPath;
        protected override void ClearModel(HTuple modelId) => HOperatorSet.ClearNccModel(modelId);
        protected override void SetHoRect(CvRegion region) => Strategy.inPara.HoRect = region;
        protected override void SetRegionIn(string path) => Strategy.inPara.RegionIn = path;
        protected override void SetSearchRange(double angleStartDeg, double angleExtentDeg, double minScore)
        {
            Strategy.inPara.AngleStart = angleStartDeg;
            Strategy.inPara.AngleExtent = angleExtentDeg;
            Strategy.inPara.MinScore = minScore;
        }
        protected override void SetModelID(HTuple modelId) => Strategy.inPara.ModelID = modelId;
        protected override int NumMatches(NccModelStrategy s) => s.inPara.NumMatches.I;
        protected override double MinScore(NccModelStrategy s) => s.inPara.MinScore.D;
        protected override double AngleStart(NccModelStrategy s) => s.inPara.AngleStart.D;
    }

    [TestClass]
    public class ScaledModelStrategyTests : MatchingStrategyTestBase<ScaledModelStrategy>
    {
        protected override string ExpectedName => "缩放匹配";
        protected override CvRegion ModeRect(ScaledModelStrategy s) => s.inPara.ModeRect;
        protected override CvRegion HoRect(ScaledModelStrategy s) => s.inPara.HoRect;
        protected override HTuple ModelID(ScaledModelStrategy s) => s.inPara.ModelID;
        protected override Point2d TmplPoint(ScaledModelStrategy s) => s.inPara.TmplPoint;
        protected override CvCoord Coord(ScaledModelStrategy s) => s.inPara.Coord;
        protected override List<ModelResult> Results(ScaledModelStrategy s) => s.inPara.Results;
        protected override string ModelPath(ScaledModelStrategy s) => s.inPara.ModelPath;
        protected override void ClearModel(HTuple modelId) => HOperatorSet.ClearShapeModel(modelId);
        protected override void SetHoRect(CvRegion region) => Strategy.inPara.HoRect = region;
        protected override void SetRegionIn(string path) => Strategy.inPara.RegionIn = path;
        protected override void SetSearchRange(double angleStartDeg, double angleExtentDeg, double minScore)
        {
            Strategy.inPara.AngleStart = angleStartDeg;
            Strategy.inPara.AngleExtent = angleExtentDeg;
            Strategy.inPara.MinScore = minScore;
        }
        protected override void SetModelID(HTuple modelId) => Strategy.inPara.ModelID = modelId;
        protected override int NumMatches(ScaledModelStrategy s) => s.inPara.NumMatches.I;
        protected override double MinScore(ScaledModelStrategy s) => s.inPara.MinScore.D;
        protected override double AngleStart(ScaledModelStrategy s) => s.inPara.AngleStart.D;

        [TestMethod]
        public void ParaRoundTrip_ScaleRange()
        {
            var ui = new FakeUiHost();
            Strategy.DispPara(ui);
            Strategy.SavePara(ui.Set("cmb_113", "0.7").Set("cmb_114", "1.5"));

            Assert.AreEqual(0.7, Strategy.inPara.ScaleMin.D, 1e-12);
            Assert.AreEqual(1.5, Strategy.inPara.ScaleMax.D, 1e-12);
        }
    }

    [TestClass]
    public class GenericModelStrategyTests : MatchingStrategyTestBase<GenericModelStrategy>
    {
        protected override string ExpectedName => "通用匹配";
        protected override CvRegion ModeRect(GenericModelStrategy s) => s.inPara.ModeRect;
        protected override CvRegion HoRect(GenericModelStrategy s) => s.inPara.HoRect;
        protected override HTuple ModelID(GenericModelStrategy s) => s.inPara.ModelID;
        protected override Point2d TmplPoint(GenericModelStrategy s) => s.inPara.TmplPoint;
        protected override CvCoord Coord(GenericModelStrategy s) => s.inPara.Coord;
        protected override List<ModelResult> Results(GenericModelStrategy s) => s.inPara.Results;
        protected override string ModelPath(GenericModelStrategy s) => s.inPara.ModelPath;
        // 22.11 没有 ClearGenericShapeModel, 通用句柄统一由 clear_handle 释放
        protected override void ClearModel(HTuple modelId) => HOperatorSet.ClearHandle(modelId);
        protected override void SetHoRect(CvRegion region) => Strategy.inPara.HoRect = region;
        protected override void SetModelID(HTuple modelId) => Strategy.inPara.ModelID = modelId;
        protected override void SetRegionIn(string path) => Strategy.inPara.RegionIn = path;
        protected override void SetSearchRange(double angleStartDeg, double angleExtentDeg, double minScore)
        {
            Strategy.inPara.AngleStart = angleStartDeg;
            Strategy.inPara.AngleExtent = angleExtentDeg;
            Strategy.inPara.MinScore = minScore;
        }
        protected override int NumMatches(GenericModelStrategy s) => s.inPara.NumMatches.I;
        protected override double MinScore(GenericModelStrategy s) => s.inPara.MinScore.D;
        protected override double AngleStart(GenericModelStrategy s) => s.inPara.AngleStart.D;

        [TestMethod]
        public void SavePara_WithModel_PushesSearchParamsIntoModel()
        {
            CreateTemplate();
            var ui = new FakeUiHost();
            Strategy.DispPara(ui);

            Strategy.SavePara(ui.Set("cmb_111", "0.8").Set("cmb_110", "多个").Set("cmb_104", "0.3"));

            // 通用匹配的查找参数存在模板句柄里, 只改 inPara 不会影响下一次 FindGenericShapeModel
            var id = Strategy.inPara.ModelID;
            HOperatorSet.GetGenericShapeModelParam(id, "min_score", out HTuple minScore);
            HOperatorSet.GetGenericShapeModelParam(id, "num_matches", out HTuple numMatches);
            HOperatorSet.GetGenericShapeModelParam(id, "max_overlap", out HTuple maxOverlap);
            Assert.AreEqual(0.8, minScore.D, 1e-9);
            Assert.AreEqual("all", numMatches.S, "\"多个\" 映射为 0, 模型内读回为 all（找全部）");
            Assert.AreEqual(0.3, maxOverlap.D, 1e-9);
        }

        [TestMethod]
        public void Run_WithoutModel_NoImageNeeded_GenericMessage()
        {
            var strategy = Strategy;   // 用夹具实例: TearDown 统一释放 ROI 句柄
            strategy.inPara.Results = new List<ModelResult> { new ModelResult(1, 2, 0, 1) };
            strategy.inPara.Coord = new CvCoord(2, 1);
            var display = new FakeDisplay();

            // 未建模板时连图像都不需要：前置校验先于取图
            Assert.IsFalse(strategy.Fun_action(display, Strategies.Of()));

            Assert.AreEqual("通用匹配", strategy.Name);
            Assert.AreEqual("通用匹配 : 未建立模板，无法执行通用匹配！", display.LastText);
            Assert.AreEqual(HColor.Red.Name, display.Texts[0].ColorName);
            Assert.AreEqual(0, strategy.inPara.Results.Count);
            Assert.AreEqual(new CvCoord(), strategy.inPara.Coord);
        }

        [TestMethod]
        public void Run_EmptyModelId_ReturnsFalse()
        {
            var strategy = Strategy;   // 用夹具实例: TearDown 统一释放 ROI 句柄
            strategy.inPara.ModelID = new HTuple();
            Assert.IsFalse(strategy.Fun_action(new FakeDisplay(), Strategies.Of()));
        }
    }
}
