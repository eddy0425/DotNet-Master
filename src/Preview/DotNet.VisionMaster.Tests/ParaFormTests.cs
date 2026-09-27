using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using DotNet.Drawing;
using DotNet.HalconCore;
using DotNet.HalconUI;
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
            public FakeStrategy Strategy;
        }

        private static void Run(AlgoEnum algorithm, Action<Ctx> body) =>
            Sta.Run(() =>
            {
                var display = new HDisplayUI();
                var para = new ParaForm(display);
                using (var host = WindowHost.ShowOffscreen(display, para))
                {
                    var strategy = new FakeStrategy(algorithm);
                    para.SelectPara(0, new List<IParaStrategy> { strategy });
                    var ctx = new Ctx { Display = display, Para = para, Host = host, Strategy = strategy };
                    try { body(ctx); }
                    finally
                    {
                        // 不收尾的话，挂起的 async void 续体会在窗体销毁后才跑
                        strategy.FinishDraw();
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
