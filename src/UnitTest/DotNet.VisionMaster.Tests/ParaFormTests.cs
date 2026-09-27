using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using DotNet.Drawing;
using DotNet.HalconAlgo;
using DotNet.HalconCore;
using DotNet.HalconUI;
using HalconDotNet;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNet.VisionMaster.Tests
{
    /// <summary>
    /// <see cref="ParaForm"/>：4 个绘制入口的类型/参数选择、同一时刻只允许一条绘制在飞的闸门、销毁时释放游离窗体。
    /// </summary>
    /// <remarks>
    /// 绘制入口都是 <c>async void</c>；<see cref="FakeStrategy"/> 返回的任务由测试决定何时完成，
    /// 从而能在"绘制进行中"这个窗口里观察闸门。
    /// </remarks>
    [TestClass]
    public class ParaFormTests : HalconTestBase
    {
        private sealed class Ctx
        {
            public HDisplayUI Display;
            public ParaForm Para;
            public Form Host;
            /// <summary>当前工具为 <see cref="FakeStrategy"/> 时才有值。</summary>
            public FakeStrategy Strategy;
            public PromptLog Prompts;
        }

        private static void Run(AlgoEnum algorithm, Action<Ctx> body) => Run(new FakeStrategy(algorithm), body);

        /// <param name="strategy">当前工具；null 表示宿主还没选过任何工具（不调 <c>SelectPara</c>）。</param>
        private static void Run(IParaStrategy strategy, Action<Ctx> body) =>
            Sta.Run(() =>
            {
                var display = new HDisplayUI();
                var para = new ParaForm(display);
                using (var prompts = new PromptLog())
                using (var host = WindowHost.ShowOffscreen(display, para))
                {
                    if (strategy != null)
                        para.SelectPara(0, new List<IParaStrategy> { strategy });
                    var fake = strategy as FakeStrategy;
                    var ctx = new Ctx { Display = display, Para = para, Host = host, Strategy = fake, Prompts = prompts };
                    try { body(ctx); }
                    finally
                    {
                        // 不收尾的话，挂起的 async void 续体会在窗体销毁后才跑
                        fake?.FinishDraw();
                        WindowHost.Pump();
                    }
                }
            });

        private static void Finish(Ctx ctx)
        {
            ctx.Strategy.FinishDraw();
            WindowHost.PumpUntil(() => !ctx.Para.IsDrawBusy);
        }

        [TestMethod]
        public void IsDrawBusy_InitiallyFalse()
        {
            Run(AlgoEnum.CreateROI, ctx => Assert.IsFalse(ctx.Para.IsDrawBusy));
        }

        #region 绘制类型与参数

        [DataTestMethod]
        [DataRow(AlgoEnum.FitLine, RectEnum.AffRect)]
        [DataRow(AlgoEnum.FitArcMidpoint, RectEnum.AffRect)]
        [DataRow(AlgoEnum.CreateROI, RectEnum.Rectangle)]
        [DataRow(AlgoEnum.ShapeModel, RectEnum.Rectangle)]
        public void DrawRegion_PicksTypeByAlgorithm_AndRequestsNewRoi(AlgoEnum algorithm, RectEnum expected)
        {
            Run(algorithm, ctx =>
            {
                Priv.Click(ctx.Para, "btn_drawRegion_Click");

                Assert.AreEqual(1, ctx.Strategy.RoiDraws.Count);
                Assert.AreEqual(expected, ctx.Strategy.RoiDraws[0].Item1);
                Assert.IsTrue(ctx.Strategy.RoiDraws[0].Item2, "新建 ROI");
                Assert.IsTrue(ctx.Para.IsDrawBusy);

                Finish(ctx);
                Assert.IsFalse(ctx.Para.IsDrawBusy);
            });
        }

        [TestMethod]
        public void EditRegion_KeepsCheckedType_AndEditsExistingRoi()
        {
            Run(AlgoEnum.FitLine, ctx =>
            {
                Priv.Click(ctx.Para, "btn_drawRegion_Click");   // 拟合直线会把单选切到仿射矩形
                Finish(ctx);

                Priv.Click(ctx.Para, "but_editRegion_Click");

                Assert.AreEqual(2, ctx.Strategy.RoiDraws.Count);
                Assert.AreEqual(Tuple.Create(RectEnum.AffRect, false), ctx.Strategy.RoiDraws[1]);
                Finish(ctx);
            });
        }

        [TestMethod]
        public void NewModel_And_ModifyModel_UseCheckedModelType()
        {
            Run(AlgoEnum.ShapeModel, ctx =>
            {
                Priv.Get<RadioButton>(ctx.Para, "btn_modelCircle").Checked = true;

                Priv.Click(ctx.Para, "btn_newModel_Click");
                Finish(ctx);
                Priv.Click(ctx.Para, "but_modifyModel_Click");
                Finish(ctx);

                CollectionAssert.AreEqual(new[]
                {
                    Tuple.Create(RectEnum.Circle, true),
                    Tuple.Create(RectEnum.Circle, false),
                }, ctx.Strategy.TemplateDraws);
                Assert.AreEqual(0, ctx.Strategy.RoiDraws.Count, "模板入口不该走 ROI 绘制");
            });
        }

        #endregion

        #region 重入闸门

        [DataTestMethod]
        [DataRow("btn_drawRegion_Click")]
        [DataRow("but_editRegion_Click")]
        [DataRow("btn_newModel_Click")]
        [DataRow("but_modifyModel_Click")]
        public void AnyDrawEntry_WhileBusy_IsIgnored(string first)
        {
            Run(AlgoEnum.ShapeModel, ctx =>
            {
                Priv.Click(ctx.Para, first);
                Assert.IsTrue(ctx.Para.IsDrawBusy);

                foreach (var entry in new[] { "btn_drawRegion_Click", "but_editRegion_Click", "btn_newModel_Click", "but_modifyModel_Click" })
                    Priv.Click(ctx.Para, entry);

                Assert.AreEqual(1, ctx.Strategy.RoiDraws.Count + ctx.Strategy.TemplateDraws.Count,
                    "绘制进行中，其它入口都不应再发起绘制");

                Finish(ctx);
            });
        }

        [TestMethod]
        public void DrawEntry_AfterPreviousFinished_IsAllowedAgain()
        {
            Run(AlgoEnum.CreateROI, ctx =>
            {
                Priv.Click(ctx.Para, "btn_drawRegion_Click");
                Finish(ctx);

                Priv.Click(ctx.Para, "btn_drawRegion_Click");

                Assert.AreEqual(2, ctx.Strategy.RoiDraws.Count);
                Assert.IsTrue(ctx.Para.IsDrawBusy);
                Finish(ctx);
            });
        }

        [TestMethod]
        public void DrawEntry_EachRoundBumpsEpoch()
        {
            Run(AlgoEnum.CreateROI, ctx =>
            {
                int before = Priv.Get<int>(ctx.Para, "_drawEpoch");

                Priv.Click(ctx.Para, "btn_drawRegion_Click");
                Priv.Click(ctx.Para, "btn_drawRegion_Click");   // 被闸门挡掉，不应计数
                Finish(ctx);

                Assert.AreEqual(before + 1, Priv.Get<int>(ctx.Para, "_drawEpoch"));
            });
        }

        /// <summary>
        /// 绘制失败时把原因告诉用户，并且放开闸门 —— 不然之后所有绘制入口都点不动了。
        /// </summary>
        [DataTestMethod]
        [DataRow("btn_drawRegion_Click")]
        [DataRow("but_editRegion_Click")]
        [DataRow("btn_newModel_Click")]
        [DataRow("but_modifyModel_Click")]
        public void DrawEntry_Failure_PromptsReason_AndReleasesGate(string entry)
        {
            Run(AlgoEnum.ShapeModel, ctx =>
            {
                ctx.Strategy.DrawError = new InvalidOperationException("绘制失败原因");

                Priv.Click(ctx.Para, entry);
                WindowHost.PumpUntil(() => !ctx.Para.IsDrawBusy);

                CollectionAssert.AreEqual(new[] { "绘制失败原因" }, ctx.Prompts.Messages);
            });
        }

        #endregion

        #region 尚未选择工具

        private static readonly string[] AllHandlers =
        {
            "btn_100_Click", "btn_101_Click", "btn_coordSource_Click", "btn_setCoordIn_Click",
            "btn_drawRegion_Click", "but_editRegion_Click", "btn_newModel_Click", "but_modifyModel_Click", "but_editModel_Click",
        };

        /// <summary>
        /// 宿主启动后并不会自动选中一个工具，参数页的按钮在此之前就能点。
        /// </summary>
        /// <remarks>回归：此前一律 NRE，被各自的 catch 弹成"未将对象引用设置到对象的实例"。</remarks>
        [TestMethod]
        public void Handlers_BeforeAnyToolSelected_DoNothing()
        {
            Run((IParaStrategy)null, ctx =>
            {
                foreach (var handler in AllHandlers)
                    Priv.Click(ctx.Para, handler);
                Priv.Click(ctx.Para, "btn_regionSource_Click", Priv.Get<Button>(ctx.Para, "btn_102"));
                Priv.Call(ctx.Para, "DrawDoneEvent", ctx.Display, new DrawModelUIArgs("", null, null, default(ModelResult)));
                WindowHost.Pump();

                CollectionAssert.AreEqual(new string[0], ctx.Prompts.Messages);
                Assert.IsFalse(ctx.Para.IsDrawBusy);
            });
        }

        [TestMethod]
        public void Handlers_IndexOutOfRange_DoNothing()
        {
            Run(AlgoEnum.ShapeModel, ctx =>
            {
                ctx.Para.SelectPara(5, new List<IParaStrategy> { ctx.Strategy });

                foreach (var handler in AllHandlers)
                    Priv.Click(ctx.Para, handler);
                WindowHost.Pump();

                CollectionAssert.AreEqual(new string[0], ctx.Prompts.Messages);
                Assert.AreEqual(0, ctx.Strategy.RoiDraws.Count + ctx.Strategy.TemplateDraws.Count);
            });
        }

        #endregion

        #region 引用上游输出（ValueForm）

        private const string Picked = "上游0/输出";

        /// <summary>点一个"引用"按钮，应答弹出的 <see cref="ValueForm"/>；没弹出时返回 null。</summary>
        /// <returns>(弹窗要求的类型, 弹窗拿到的初始值)</returns>
        private static Tuple<OutEnum, string> ClickValueSource(Ctx ctx, string handler, string senderName, bool accept)
        {
            var dialog = Priv.Get<ValueForm>(ctx.Para, "_form_Value");
            Tuple<OutEnum, string> seen = null;
            using (WindowHost.RespondWhenShown(dialog, () =>
            {
                seen = Tuple.Create(dialog.ValueType, dialog.StrReturn);
                dialog.StrReturn = Picked;
                dialog.DialogResult = accept ? DialogResult.OK : DialogResult.Cancel;
            }))
            {
                Priv.Click(ctx.Para, handler, senderName == null ? null : Priv.Get<Button>(ctx.Para, senderName));
            }
            return seen;
        }

        [DataTestMethod]
        [DataRow(AlgoEnum.ShapeModel, "btn_100_Click", null, "cmb_100", OutEnum.Image)]
        [DataRow(AlgoEnum.FitLine, "btn_100_Click", null, "cmb_100", OutEnum.Image)]
        [DataRow(AlgoEnum.RotateImage, "btn_100_Click", null, "cmb_100", OutEnum.Image)]
        [DataRow(AlgoEnum.MergeRegion, "btn_100_Click", null, "cmb_100", OutEnum.Region)]
        [DataRow(AlgoEnum.NccModel, "btn_101_Click", null, "cmb_101", OutEnum.Region)]
        [DataRow(AlgoEnum.MergeRegion, "btn_101_Click", null, "cmb_101", OutEnum.Region)]
        [DataRow(AlgoEnum.LineRotImage, "btn_101_Click", null, "cmb_101", OutEnum.Line)]
        [DataRow(AlgoEnum.MergeRegion, "btn_regionSource_Click", "btn_102", "cmb_102", OutEnum.Region)]
        [DataRow(AlgoEnum.MergeRegion, "btn_regionSource_Click", "btn_103", "cmb_103", OutEnum.Region)]
        [DataRow(AlgoEnum.MergeRegion, "btn_regionSource_Click", "btn_104", "cmb_104", OutEnum.Region)]
        [DataRow(AlgoEnum.MergeRegion, "btn_regionSource_Click", "btn_105", "cmb_105", OutEnum.Region)]
        [DataRow(AlgoEnum.MergeRegion, "btn_coordSource_Click", null, "cmb_110", OutEnum.Coord)]
        [DataRow(AlgoEnum.CreateROI, "btn_setCoordIn_Click", null, "cmb_CoordIn", OutEnum.Coord)]
        [DataRow(AlgoEnum.FitArcMidpoint, "btn_setCoordIn_Click", null, "cmb_CoordIn", OutEnum.Coord)]
        public void ValueSource_AsksForMatchingType_AndWritesBackOnOk(AlgoEnum algorithm, string handler, string sender, string combo, OutEnum expected)
        {
            Run(algorithm, ctx =>
            {
                var target = Priv.Get<ComboBox>(ctx.Para, combo);
                target.Text = "上次的值";

                var seen = ClickValueSource(ctx, handler, sender, accept: true);

                Assert.IsNotNull(seen, "应弹出引用窗");
                Assert.AreEqual(expected, seen.Item1);
                Assert.AreEqual("上次的值", seen.Item2, "引用窗要拿当前值去预选节点");
                Assert.AreEqual(Picked, target.Text);
                CollectionAssert.AreEqual(new string[0], ctx.Prompts.Messages);
            });
        }

        [TestMethod]
        public void ValueSource_Cancelled_KeepsOriginalText()
        {
            Run(AlgoEnum.ShapeModel, ctx =>
            {
                var target = Priv.Get<ComboBox>(ctx.Para, "cmb_100");
                target.Text = "上次的值";

                Assert.IsNotNull(ClickValueSource(ctx, "btn_100_Click", null, accept: false));

                Assert.AreEqual("上次的值", target.Text);
            });
        }

        /// <summary>按钮被隐藏只是不可见，不适用的算法点到了也不能弹窗。</summary>
        [DataTestMethod]
        [DataRow(AlgoEnum.CreateROI, "btn_100_Click", null)]
        [DataRow(AlgoEnum.FileImage, "btn_101_Click", null)]
        [DataRow(AlgoEnum.ShapeModel, "btn_regionSource_Click", "btn_102")]
        [DataRow(AlgoEnum.FitLine, "btn_coordSource_Click", null)]
        [DataRow(AlgoEnum.MergeRegion, "btn_setCoordIn_Click", null)]
        public void ValueSource_NotApplicableToAlgorithm_DoesNotOpen(AlgoEnum algorithm, string handler, string sender)
        {
            Run(algorithm, ctx =>
            {
                Assert.IsNull(ClickValueSource(ctx, handler, sender, accept: true));
                CollectionAssert.AreEqual(new string[0], ctx.Prompts.Messages);
            });
        }

        #endregion

        #region 编辑模板

        private static Form EditModelWindow(Ctx ctx) => Priv.Get<Form>(ctx.Para, "_editModel");

        [TestMethod]
        public void EditModel_WhileDrawing_PromptsAndDoesNotOpen()
        {
            Run(AlgoEnum.ShapeModel, ctx =>
            {
                Priv.Click(ctx.Para, "btn_newModel_Click");

                Priv.Click(ctx.Para, "but_editModel_Click");

                Assert.AreEqual(1, ctx.Prompts.Messages.Count);
                StringAssert.Contains(ctx.Prompts.Messages[0], "正在绘制");
                Assert.IsFalse(EditModelWindow(ctx).Visible);
                Finish(ctx);
            });
        }

        [TestMethod]
        public void EditModel_WhileEditWindowDrawing_PromptsAndDoesNotReload()
        {
            Run(new ShapeModelStrategy(), ctx =>
            {
                Priv.Set(EditModelWindow(ctx), "_drawBusy", true);
                try { Priv.Click(ctx.Para, "but_editModel_Click"); }
                finally { Priv.Set(EditModelWindow(ctx), "_drawBusy", false); }

                Assert.AreEqual(1, ctx.Prompts.Messages.Count);
                StringAssert.Contains(ctx.Prompts.Messages[0], "模板编辑窗正在绘制");
            });
        }

        private static IParaStrategy NewStrategy(Type type, string modelPath = null)
        {
            var strategy = (IParaStrategy)Activator.CreateInstance(type);
            if (modelPath != null)
            {
                var inPara = strategy.GetType().GetProperty("inPara").GetValue(strategy);
                inPara.GetType().GetProperty("ModelPath").SetValue(inPara, modelPath);
            }
            return strategy;
        }

        /// <summary>
        /// 还没建过模板就点"编辑模板"，应提示先建模板。
        /// </summary>
        /// <remarks>回归：此前直接取 <c>Results[0]</c>，弹出的是"索引超出范围"。</remarks>
        [DataTestMethod]
        [DataRow(typeof(ShapeModelStrategy))]
        [DataRow(typeof(NccModelStrategy))]
        [DataRow(typeof(ScaledModelStrategy))]
        [DataRow(typeof(GenericModelStrategy))]
        public void EditModel_NoTemplateYet_PromptsToCreateOne(Type type)
        {
            Run(NewStrategy(type), ctx =>
            {
                Priv.Click(ctx.Para, "but_editModel_Click");

                Assert.AreEqual(1, ctx.Prompts.Messages.Count);
                StringAssert.Contains(ctx.Prompts.Messages[0], "新建模板");
                Assert.IsFalse(EditModelWindow(ctx).Visible);
            });
        }

        /// <summary>
        /// 模板建过、但最近一次运行没匹配上（<c>Results</c> 被清空），应提示先匹配成功。
        /// </summary>
        /// <remarks>编辑窗要拿 <c>Results[0]</c> 的位姿把模板区域摆回模板图上，没有结果就无从摆放。</remarks>
        [DataTestMethod]
        [DataRow(typeof(ShapeModelStrategy))]
        [DataRow(typeof(NccModelStrategy))]
        [DataRow(typeof(ScaledModelStrategy))]
        [DataRow(typeof(GenericModelStrategy))]
        public void EditModel_NoMatchResult_PromptsToRunFirst(Type type)
        {
            Run(NewStrategy(type, modelPath: typeof(ParaFormTests).Assembly.Location), ctx =>
            {
                Priv.Click(ctx.Para, "but_editModel_Click");

                Assert.AreEqual(1, ctx.Prompts.Messages.Count);
                StringAssert.Contains(ctx.Prompts.Messages[0], "匹配");
                Assert.IsFalse(EditModelWindow(ctx).Visible);
            });
        }

        /// <summary>
        /// 模板图在、也有匹配结果：打开编辑窗，不提示。
        /// </summary>
        /// <remarks>上面两条提示的反面 —— 闸门不能把正常情况也拦掉。</remarks>
        [DataTestMethod]
        [DataRow(typeof(ShapeModelStrategy))]
        [DataRow(typeof(NccModelStrategy))]
        [DataRow(typeof(ScaledModelStrategy))]
        [DataRow(typeof(GenericModelStrategy))]
        public void EditModel_TemplateAndMatchResult_OpensEditWindow(Type type)
        {
            string modelPath = Path.Combine(Path.GetTempPath(), "VisionMasterTests_" + Guid.NewGuid().ToString("N") + ".png");
            HOperatorSet.GenImageConst(out HObject image, "byte", 64, 64);
            try { HOperatorSet.WriteImage(image, "png", 0, modelPath); }
            finally { image.Dispose(); }

            try
            {
                var strategy = NewStrategy(type, modelPath);
                var inPara = strategy.GetType().GetProperty("inPara").GetValue(strategy);
                inPara.GetType().GetProperty("Results").SetValue(inPara, new List<ModelResult> { new ModelResult(32, 32, 0, 1) });

                Run(strategy, ctx =>
                {
                    Priv.Click(ctx.Para, "but_editModel_Click");

                    CollectionAssert.AreEqual(new string[0], ctx.Prompts.Messages);
                    Assert.IsTrue(EditModelWindow(ctx).Visible);
                    EditModelWindow(ctx).Hide();
                });
            }
            finally { File.Delete(modelPath); }
        }

        [TestMethod]
        public void EditModel_NonModelAlgorithm_DoesNothing()
        {
            Run(AlgoEnum.FitLine, ctx =>
            {
                Priv.Click(ctx.Para, "but_editModel_Click");

                CollectionAssert.AreEqual(new string[0], ctx.Prompts.Messages);
                Assert.IsFalse(EditModelWindow(ctx).Visible);
            });
        }

        [TestMethod]
        public void DrawDone_NonModelAlgorithm_Ignored()
        {
            Run(AlgoEnum.CreateROI, ctx =>
            {
                ctx.Display.DrawDone("", null, null, default(ModelResult));

                CollectionAssert.AreEqual(new string[0], ctx.Prompts.Messages);
            });
        }

        #endregion

        #region 图像目录

        [TestMethod]
        public void OpenPath_MissingFolder_PromptsReason()
        {
            Run(AlgoEnum.FileImage, ctx =>
            {
                Priv.Get<ComboBox>(ctx.Para, "cmb_ImageFolder").Text =
                    Path.Combine(Path.GetTempPath(), "VisionMasterTests_missing_" + Guid.NewGuid().ToString("N"));

                Priv.Click(ctx.Para, "btn_openPath_Click", Priv.Get<Button>(ctx.Para, "btn_openPath"));

                Assert.AreEqual(1, ctx.Prompts.Messages.Count, "打不开目录要告诉用户，而不是静默失败");
            });
        }

        #endregion

        #region 释放

        private static IEnumerable<Delegate> DrawDoneSubscribers(HDisplayUI display)
        {
            var handler = (Delegate)typeof(HDisplayUI)
                .GetField(nameof(HDisplayUI.DrawDoneEvent), BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(display);
            return handler?.GetInvocationList() ?? Enumerable.Empty<Delegate>();
        }

        [TestMethod]
        public void Load_SubscribesDrawDone()
        {
            Run(AlgoEnum.ShapeModel, ctx =>
            {
                Assert.IsTrue(DrawDoneSubscribers(ctx.Display).Any(d => d.Target == ctx.Para));
            });
        }

        [TestMethod]
        public void Dispose_ReleasesDetachedForms_AndUnsubscribesDrawDone()
        {
            Run(AlgoEnum.ShapeModel, ctx =>
            {
                var valueForm = Priv.Get<Form>(ctx.Para, "_form_Value");
                var editModel = Priv.Get<Form>(ctx.Para, "_editModel");

                ctx.Para.Dispose();

                Assert.IsTrue(valueForm.IsDisposed, "_form_Value 没有父容器，只能由 ParaForm 释放");
                Assert.IsTrue(editModel.IsDisposed, "_editModel 没有父容器，只能由 ParaForm 释放");
                Assert.IsFalse(DrawDoneSubscribers(ctx.Display).Any(d => d.Target == ctx.Para),
                    "不退订的话 HDisplayUI 会一直引着已销毁的 ParaForm");
            });
        }

        /// <summary>
        /// 句柄重建（控件仍存活）时 HandleDestroyed 也会发，此时不能释放游离窗体。
        /// </summary>
        /// <remarks>
        /// 不用改 <c>RightToLeft</c> 真去重建句柄：那会连带重建 HModelUI 里的 HWindowControl，
        /// 重建途中 HALCON 的 get_window_extents 抛 #5154，被 WinForms 弹成模态框卡住测试。
        /// 这里直接在控件存活时调 handler，验证的正是它开头那道 <c>Disposing</c> 判据。
        /// </remarks>
        [TestMethod]
        public void HandleDestroyed_WhileAlive_DoesNotReleaseDetachedForms()
        {
            Run(AlgoEnum.ShapeModel, ctx =>
            {
                var valueForm = Priv.Get<Form>(ctx.Para, "_form_Value");
                var editModel = Priv.Get<Form>(ctx.Para, "_editModel");

                Priv.Click(ctx.Para, "ParaForm_HandleDestroyed", ctx.Para);

                Assert.IsFalse(valueForm.IsDisposed);
                Assert.IsFalse(editModel.IsDisposed);
                Assert.IsTrue(DrawDoneSubscribers(ctx.Display).Any(d => d.Target == ctx.Para));
            });
        }

        #endregion
    }
}
