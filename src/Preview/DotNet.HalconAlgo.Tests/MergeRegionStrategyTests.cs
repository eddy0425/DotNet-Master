using System.Collections.Generic;
using DotNet.Drawing;
using DotNet.Vision.Abstractions;
using HalconDotNet;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNet.HalconAlgo.Tests
{
    [TestClass]
    public class MergeRegionStrategyTests : HalconTestBase
    {
        private readonly List<HObject> _owned = new List<HObject>();
        private FakeDisplay _display;
        private MergeRegionStrategy _strategy;
        private StubStrategy _a, _b;

        /// <summary>两个不相交的 11×11 方块：A 中心 (15,15)，B 中心 (55,15)。</summary>
        [TestInitialize]
        public void SetUp()
        {
            _display = new FakeDisplay();
            _strategy = new MergeRegionStrategy();
            _a = new StubStrategy("A").Output("区域", Own(Rectangle1(10, 10, 20, 20)));
            _b = new StubStrategy("B").Output("区域", Own(Rectangle1(10, 50, 20, 60)));
        }

        [TestCleanup]
        public void TearDown()
        {
            _strategy.Dispose();
            foreach (var o in _owned) o.Dispose();
        }

        private HObject Own(HObject o)
        {
            _owned.Add(o);
            return o;
        }

        private void Sources(params string[] paths)
        {
            for (int i = 0; i < paths.Length; i++) _strategy.inPara.RegionSources[i] = paths[i];
        }

        [TestMethod]
        public void Identity()
        {
            Assert.AreEqual(AlgoEnum.MergeRegion, _strategy.Algorithm);
            Assert.AreEqual("区域合并", _strategy.Name);
            Assert.AreEqual(6, _strategy.inPara.RegionSources.Length);
        }

        [TestMethod]
        public void NoSources_ReturnsFalse_RedTextEvenIfTextDisabled()
        {
            _strategy.inPara.DispText = false;

            Assert.IsFalse(_strategy.Fun_action(_display, Strategies.Of(_a, _b)));

            Assert.AreEqual("区域合并 : 无有效输入区域", _display.LastText);
            Assert.AreEqual(HColor.Red.Name, _display.Texts[0].ColorName);
            Assert.IsFalse(_strategy.inPara.Result.HoRegion.IsUsableRegion());
            Assert.IsNull(_strategy.inPara.TmplPoint);
        }

        [TestMethod]
        public void NullSourceArray_TreatedAsNoSources()
        {
            _strategy.inPara.RegionSources = null;
            Assert.IsFalse(_strategy.Fun_action(_display, Strategies.Of(_a)));
        }

        [TestMethod]
        public void AllSourcesMissing_ReturnsFalse()
        {
            Sources("X/区域", "Y/区域");
            Assert.IsFalse(_strategy.Fun_action(_display, Strategies.Of(_a, _b)));
            StringAssert.Contains(_display.LastText, "无有效输入区域");
        }

        [TestMethod]
        public void Union_AreaCenter_AndTeachesTmplPointOnce()
        {
            Sources("A/区域", " ", "B/区域");

            Assert.IsTrue(_strategy.Fun_action(_display, Strategies.Of(_a, _b)));

            double area = AreaCenter(_strategy.inPara.Result.HoRegion, out Point2d center);
            Assert.AreEqual(2 * 121, area, "空白路径跳过，两块并在一起");
            Assert.AreEqual(35, center.X, 1e-6);
            Assert.AreEqual(15, center.Y, 1e-6);

            Assert.AreEqual(35, _strategy.inPara.Coord.X, 1e-6);
            Assert.AreEqual(15, _strategy.inPara.Coord.Y, 1e-6);
            Assert.AreEqual(0, _strategy.inPara.Coord.Angle.Radians);
            Assert.AreEqual(new Point2d(35, 15), _strategy.inPara.TmplPoint);

            Assert.AreEqual(1, _display.Objects.Count);
            Assert.AreSame(_strategy.inPara.Result.HoRegion, _display.Objects[0].Item);
            Assert.AreEqual("区域合并 : 合并数量:2 中心:(35.00,15.00) 跟随坐标:默认", _display.LastText);
            Assert.AreEqual(HColor.Green.Name, _display.Texts[0].ColorName);

            // 示教只发生一次：来源变化后 TmplPoint 仍是首轮的值
            Sources("A/区域", null, null);
            Assert.IsTrue(_strategy.Fun_action(_display, Strategies.Of(_a, _b)));
            Assert.AreEqual(new Point2d(35, 15), _strategy.inPara.TmplPoint);
            Assert.AreEqual(15, _strategy.inPara.Coord.X, 1e-6);
        }

        [TestMethod]
        public void Result_IsIndependentCopy()
        {
            Sources("A/区域");
            Assert.IsTrue(_strategy.Fun_action(_display, Strategies.Of(_a)));
            var first = _strategy.inPara.Result.HoRegion;

            Assert.IsTrue(_strategy.Fun_action(_display, Strategies.Of(_a)));

            Assert.IsFalse(first.IsInitialized(), "上一轮结果被释放");
            Assert.IsTrue(_owned[0].IsInitialized(), "上游区域不能被释放");
        }

        [TestMethod]
        public void MissingSource_RedText_DoesNotTeach()
        {
            Sources("A/区域", "不存在/区域");

            Assert.IsTrue(_strategy.Fun_action(_display, Strategies.Of(_a, _b)));

            Assert.AreEqual(121, AreaCenter(_strategy.inPara.Result.HoRegion, out _));
            Assert.IsNull(_strategy.inPara.TmplPoint, "残缺重心不能当示教基准");
            StringAssert.Contains(_display.LastText, "合并数量:1 无效来源:1");
            Assert.AreEqual(HColor.Red.Name, _display.Texts[0].ColorName);
        }

        [TestMethod]
        public void EmptyUpstreamRegion_CountsAsMissing()
        {
            HOperatorSet.GenEmptyObj(out HObject empty);
            var c = new StubStrategy("C").Output("区域", Own(empty));
            Sources("A/区域", "C/区域");

            Assert.IsTrue(_strategy.Fun_action(_display, Strategies.Of(_a, c)));

            StringAssert.Contains(_display.LastText, "无效来源:1");
        }

        [TestMethod]
        public void ZeroAreaUpstreamRegion_ReturnsFalse_ClearsResult_DoesNotTeach()
        {
            // gen_empty_region：count_obj 为 1、能通过来源校验，但没有像素。
            // 原先 area_center 给出 (0,0)，被当成重心发布并示教成 TmplPoint
            HOperatorSet.GenEmptyRegion(out HObject emptyRegion);
            var c = new StubStrategy("C").Output("区域", Own(emptyRegion));
            Sources("A/区域");
            Assert.IsTrue(_strategy.Fun_action(_display, Strategies.Of(_a)));
            _strategy.inPara.TmplPoint = null;

            Sources("C/区域");
            Assert.IsFalse(_strategy.Fun_action(_display, Strategies.Of(c)));

            Assert.AreEqual("区域合并 : 无有效输入区域", _display.LastText);
            Assert.AreEqual(HColor.Red.Name, _display.Texts[_display.Texts.Count - 1].ColorName);
            Assert.IsFalse(_strategy.inPara.Result.HoRegion.IsUsableRegion(), "上一轮结果不能留给下游");
            Assert.IsNull(_strategy.inPara.TmplPoint);
        }

        [TestMethod]
        public void ZeroAreaRegion_AlongsideValidRegion_CountsAsMissing_DoesNotTeach()
        {
            // 与空句柄同口径: 这一轮上游什么都没找到, 重心是残缺的, 不能示教、也不能显示成绿字
            HOperatorSet.GenEmptyRegion(out HObject emptyRegion);
            var c = new StubStrategy("C").Output("区域", Own(emptyRegion));
            Sources("A/区域", "C/区域");

            Assert.IsTrue(_strategy.Fun_action(_display, Strategies.Of(_a, c)));

            Assert.AreEqual(121, AreaCenter(_strategy.inPara.Result.HoRegion, out _));
            Assert.IsNull(_strategy.inPara.TmplPoint, "残缺重心不能当示教基准");
            StringAssert.Contains(_display.LastText, "合并数量:1 无效来源:1");
            Assert.AreEqual(HColor.Red.Name, _display.Texts[_display.Texts.Count - 1].ColorName);
        }

        [TestMethod]
        public void CoordIn_Follow_TransformsResult_TeachesUntransformedCenter()
        {
            Sources("A/区域", "B/区域");
            _strategy.inPara.CoordIn = "定位/坐标系";
            var locator = StubStrategy.Coord("定位", new Point2d(0, 0), new CvCoord(100, 50));

            Assert.IsTrue(_strategy.Fun_action(_display, Strategies.Of(_a, _b, locator)));

            AreaCenter(_strategy.inPara.Result.HoRegion, out Point2d center);
            Assert.AreEqual(135, center.X, 1e-6);
            Assert.AreEqual(65, center.Y, 1e-6);
            Assert.AreEqual(135, _strategy.inPara.Coord.X, 1e-6);
            Assert.AreEqual(new Point2d(35, 15), _strategy.inPara.TmplPoint, "示教取变换前的重心");
        }

        [TestMethod]
        public void CoordIn_Rotation_CoordCarriesInputAngle()
        {
            Sources("A/区域");
            _strategy.inPara.CoordIn = "定位/坐标系";
            var locator = StubStrategy.Coord("定位", new Point2d(15, 15), CvCoord.FromDegrees(15, 15, 30));

            Assert.IsTrue(_strategy.Fun_action(_display, Strategies.Of(_a, locator)));

            Assert.AreEqual(30, _strategy.inPara.Coord.AngleDegrees, 1e-9);
            Assert.AreEqual(15, _strategy.inPara.Coord.X, 0.5);
            Assert.AreEqual(15, _strategy.inPara.Coord.Y, 0.5);
        }

        [TestMethod]
        public void CoordIn_Unresolvable_ThrowsAndClearsResult()
        {
            Sources("A/区域");
            Assert.IsTrue(_strategy.Fun_action(_display, Strategies.Of(_a)));
            _strategy.inPara.CoordIn = "定位/坐标系";

            Assert.ThrowsException<AlgoOutputNotFoundException>(() => _strategy.Fun_action(_display, Strategies.Of(_a)));

            Assert.IsFalse(_strategy.inPara.Result.HoRegion.IsUsableRegion());
            Assert.AreEqual(new CvCoord(), _strategy.inPara.Coord);
        }

        [TestMethod]
        public void Outputs_TmplPointOnlyAfterTeaching()
        {
            var tree = new FakeTree();
            _strategy.GenTreeNode(tree);
            CollectionAssert.IsSubsetOf(new[] { "区域合并/坐标系", "区域合并/坐标系/原点/行", "区域合并/坐标系/角度", "区域合并/区域" }, tree.Paths);

            var all = Strategies.Of(_a, _strategy);
            Assert.ThrowsException<AlgoOutputNotFoundException>(() => all.ResolveFrom<Point2d>("区域合并/TmplPoint"),
                "未示教时下游跟随应响亮失败");

            Sources("A/区域");
            Assert.IsTrue(_strategy.Fun_action(_display, all));

            Assert.AreEqual(new Point2d(15, 15), all.ResolveFrom<Point2d>("区域合并/坐标系".ToTmplPoint()));
            Assert.AreEqual(_strategy.inPara.Coord, all.ResolveFrom<CvCoord>("区域合并/坐标系"));
            Assert.AreEqual(15, all.ResolveFrom<double>("区域合并/坐标系/原点/列"), 1e-6);
            Assert.AreSame(_strategy.inPara.Result.HoRegion, all.ResolveRegionFrom("区域合并/区域"));
        }

        #region SavePara

        private FakeUiHost DisplayedUi()
        {
            var ui = new FakeUiHost();
            _strategy.DispPara(ui);
            return ui;
        }

        [TestMethod]
        public void DispPara_UsesSlot110ForCoordIn_AndSixSourceSlots()
        {
            Sources("A/区域");
            var ui = DisplayedUi();

            CollectionAssert.AreEqual(new[] { TabPageEnum.Parameter, TabPageEnum.Display }, ui.Tabs);
            Assert.AreEqual("跟随坐标", ui.Values["lbl_110"]);
            Assert.AreEqual("默认", ui.Values["cmb_110"]);
            Assert.AreEqual("A/区域", ui.Values["cmb_100"]);
            Assert.AreEqual("输入区域5", ui.Values["lbl_105"]);
            Assert.IsFalse(ui.Values.ContainsKey("cmb_CoordIn"));
        }

        [TestMethod]
        public void SavePara_Unchanged_KeepsTmplPoint_NullEqualsEmpty()
        {
            _strategy.inPara.TmplPoint = new Point2d(1, 2);
            var ui = DisplayedUi();
            // 空下拉框读回 ""，配置里是 null：不应视为改动
            for (int i = 0; i < 6; i++) ui.Set($"cmb_{100 + i}", "");

            _strategy.SavePara(ui);

            Assert.AreEqual(new Point2d(1, 2), _strategy.inPara.TmplPoint);
        }

        [TestMethod]
        public void ParaPage_NullSourceArray_ShowsSixEmptySlots_AndSaves()
        {
            // job 文件里 "RegionSources": null 反序列化后就是 null: Fun_action 早已容忍, 参数页原先直接 NRE
            var cfg = Newtonsoft.Json.JsonConvert.DeserializeObject<RegionMerge>("{\"RegionSources\":null}");
            Assert.IsNull(cfg.RegionSources);
            _strategy.inPara.RegionSources = null;

            var ui = DisplayedUi();
            Assert.AreEqual("输入区域5", ui.Values["lbl_105"]);
            Assert.IsNull(ui.Values["cmb_100"]);

            _strategy.SavePara(ui.Set("cmb_102", "C/区域"));

            Assert.AreEqual(6, _strategy.inPara.RegionSources.Length);
            Assert.AreEqual("C/区域", _strategy.inPara.RegionSources[2]);
        }

        [TestMethod]
        public void SavePara_SourceChanged_ClearsTmplPoint()
        {
            _strategy.inPara.TmplPoint = new Point2d(1, 2);
            var ui = DisplayedUi().Set("cmb_103", "B/区域");

            _strategy.SavePara(ui);

            Assert.IsNull(_strategy.inPara.TmplPoint);
            Assert.AreEqual("B/区域", _strategy.inPara.RegionSources[3]);
        }

        [TestMethod]
        public void SavePara_CoordInChanged_ClearsTmplPoint()
        {
            _strategy.inPara.TmplPoint = new Point2d(1, 2);
            var ui = DisplayedUi().Set("cmb_110", "定位/坐标系");

            _strategy.SavePara(ui);

            Assert.IsNull(_strategy.inPara.TmplPoint);
            Assert.AreEqual("定位/坐标系", _strategy.inPara.CoordIn);
        }

        [TestMethod]
        public void SavePara_DisplayOnlyChange_KeepsTmplPoint()
        {
            _strategy.inPara.TmplPoint = new Point2d(1, 2);
            var ui = DisplayedUi().Set("CB_FontSize", "30").Check("ckb_disp1", false);

            _strategy.SavePara(ui);

            Assert.AreEqual(new Point2d(1, 2), _strategy.inPara.TmplPoint);
            Assert.AreEqual(30, _strategy.inPara.FontSize);
            Assert.IsFalse(_strategy.inPara.DispRegion);
        }

        #endregion

        [TestMethod]
        public void Close_ClearsResult_KeepsTmplPoint()
        {
            Sources("A/区域");
            Assert.IsTrue(_strategy.Fun_action(_display, Strategies.Of(_a)));

            _strategy.Close(new FakeRoiHost());

            Assert.IsFalse(_strategy.inPara.Result.HoRegion.IsUsableRegion());
            Assert.AreEqual(new CvCoord(), _strategy.inPara.Coord);
            Assert.AreEqual(new Point2d(15, 15), _strategy.inPara.TmplPoint);
        }

        [TestMethod]
        public void Dispose_Idempotent_CloseAfterIsNoop()
        {
            Sources("A/区域");
            Assert.IsTrue(_strategy.Fun_action(_display, Strategies.Of(_a)));
            var result = _strategy.inPara.Result.HoRegion;

            _strategy.Dispose();
            _strategy.Dispose();
            _strategy.Close(new FakeRoiHost());

            Assert.IsFalse(result.IsInitialized());
            Assert.IsNull(_strategy.inPara.Result.HoRegion);
        }

        [TestMethod]
        public void Serialization_KeepsTmplPoint_SkipsResult()
        {
            _strategy.inPara.TmplPoint = new Point2d(1, 2);
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(_strategy.inPara);
            var back = Newtonsoft.Json.JsonConvert.DeserializeObject<RegionMerge>(json);

            Assert.IsFalse(json.Contains("\"Result\""));
            Assert.AreEqual(new Point2d(1, 2), back.TmplPoint);
        }
    }
}
