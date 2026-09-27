using System;
using System.Reflection;
using System.Windows.Forms;
using DotNet.Drawing;
using HalconDotNet;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNet.HalconUI.Tests
{
    /// <summary>
    /// <see cref="HEditModelUI"/>：涂抹擦除与添加/删除区域之间的句柄交接、关窗取消绘制、释放句柄。
    /// </summary>
    /// <remarks>
    /// 按钮处理器与模板字段都是 private 的，用反射驱动；鼠标事件走 public 的 OnMouseXxx。
    /// 窗体要先 Show 一次，Load 里才会填充形状 / 线宽 / 颜色下拉框。
    /// </remarks>
    [TestClass]
    public class HEditModelUITests : HalconTestBase
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

        [TestCleanup]
        public void Cleanup() => DrawHelper.CancelDraw();

        private static void Run(Action<HEditModelUI> body) =>
            Sta.Run(() =>
            {
                var ui = new HEditModelUI();
                try
                {
                    WindowHost.ShowOffscreen(ui);
                    body(ui);
                }
                finally { ui.Dispose(); }
            });

        private static HObject Field(HEditModelUI ui, string name) =>
            (HObject)typeof(HEditModelUI).GetField(name, Private).GetValue(ui);

        private static void SetField(HEditModelUI ui, string name, HObject value)
        {
            var field = typeof(HEditModelUI).GetField(name, Private);
            ((HObject)field.GetValue(ui))?.Dispose();
            field.SetValue(ui, value);
        }

        private static void Click(HEditModelUI ui, string handler) =>
            typeof(HEditModelUI).GetMethod(handler, Private).Invoke(ui, new object[] { ui, EventArgs.Empty });

        /// <summary>在编辑窗的显示窗口上画一个矩形并右键确认。</summary>
        private static void DrawRectangle(HEditModelUI ui, int row1, int col1, int row2, int col2)
        {
            var window = ui.GetDisplay().HoWindow;
            DrawHelper.ForwardMouseDown(window, Mouse.Left(col1, row1));
            DrawHelper.ForwardMouseUp(window, Mouse.Left(col2, row2));
            DrawHelper.ForwardMouseUp(window, Mouse.Right(0, 0));
        }

        private static void PumpUntilIdle(HEditModelUI ui)
        {
            var deadline = Environment.TickCount + 5000;
            while (ui.IsDrawBusy)
            {
                if (Environment.TickCount - deadline > 0) throw new TimeoutException("绘制会话未结束.");
                Application.DoEvents();
            }
        }

        private static void EraseStroke(HEditModelUI ui, int row, int col)
        {
            ui.OnMouseDown(null, Mouse.Left(col, row));
            ui.OnMouseMove(null, Mouse.Move(col, row));
            ui.OnMouseUp(null, Mouse.Left(col, row));
        }

        [TestMethod]
        public void Erase_KeepsFieldsInSyncWithEraseHandler()
        {
            Run(ui =>
            {
                SetField(ui, "shrFindMode", new HRegion(0.0, 0, 99, 99));
                double before = Area(Field(ui, "shrFindMode"));

                Click(ui, "but_ApplyRegion_Click");
                Assert.AreEqual(DrawEnum.Erase, ui.GetDisplay().DrawType);

                EraseStroke(ui, 30, 30);
                EraseStroke(ui, 60, 60);

                var findMode = Field(ui, "shrFindMode");
                var erase = Field(ui, "shrErase");
                Assert.IsTrue(findMode.IsInitialized(), "涂抹后模板字段不能指向已释放的句柄");
                Assert.IsTrue(erase.IsInitialized());
                Assert.IsTrue(Area(findMode) < before);
                Assert.IsFalse(Contains(findMode, 30, 30));
                Assert.IsFalse(Contains(findMode, 60, 60));
                Assert.IsTrue(Contains(erase, 30, 30));
            });
        }

        [TestMethod]
        public void Erase_KeepsDispModelCacheInSync()
        {
            // dispModel 缓存的是 SetModelPara 时的 shrFindMode；涂抹会释放它，缓存不跟着换的话
            // 之后切回 DispModel 画的就是已释放句柄（HDisplay.Disp 按空句柄静默跳过，模板区域凭空消失）。
            Run(ui =>
            {
                SetField(ui, "shrFindMode", new HRegion(0.0, 0, 99, 99));
                ui.GetDisplay().SetModelPara(Field(ui, "shrFindMode"), Field(ui, "_shrContour"), new CvCoord(50, 50));

                Click(ui, "but_ApplyRegion_Click");
                EraseStroke(ui, 30, 30);

                var dispModel = typeof(HDisplayUI).GetField("dispModel", Private).GetValue(ui.GetDisplay());
                var cached = (HObject)typeof(DispModelMouse).GetField("_findMode", Private).GetValue(dispModel);
                Assert.AreSame(Field(ui, "shrFindMode"), cached);
                Assert.IsTrue(cached.IsInitialized());
            });
        }

        [TestMethod]
        public void AddRegion_AfterErase_UnionsWithErasedTemplate()
        {
            Run(ui =>
            {
                SetField(ui, "shrFindMode", new HRegion(0.0, 0, 99, 99));
                Click(ui, "but_ApplyRegion_Click");
                EraseStroke(ui, 30, 30);

                Click(ui, "but_addRegion_Click");
                Assert.IsTrue(ui.IsDrawBusy);
                Assert.AreEqual(DrawEnum.None, ui.GetDisplay().DrawType);
                DrawRectangle(ui, 150, 150, 200, 250);
                PumpUntilIdle(ui);

                var findMode = Field(ui, "shrFindMode");
                Assert.IsTrue(findMode.IsInitialized());
                Assert.IsTrue(Contains(findMode, 170, 200), "新增区域应并入模板");
                Assert.IsTrue(Contains(findMode, 80, 80), "原模板保留");
                Assert.IsFalse(Contains(findMode, 30, 30), "此前擦除的部分不应被恢复");
                Assert.AreEqual(DrawEnum.None, ui.GetDisplay().DrawType);
            });
        }

        [TestMethod]
        public void DeleteRegion_SubtractsDrawnShape()
        {
            Run(ui =>
            {
                SetField(ui, "shrFindMode", new HRegion(0.0, 0, 99, 99));

                Click(ui, "btn_deleteRegion_Click");
                DrawRectangle(ui, 0, 0, 49, 99);
                PumpUntilIdle(ui);

                var findMode = Field(ui, "shrFindMode");
                Assert.IsFalse(Contains(findMode, 20, 50));
                Assert.IsTrue(Contains(findMode, 80, 50));
            });
        }

        [TestMethod]
        public void CancelledDraw_LeavesTemplateUntouched()
        {
            Run(ui =>
            {
                var original = new HRegion(0.0, 0, 99, 99);
                SetField(ui, "shrFindMode", original);

                Click(ui, "btn_deleteRegion_Click");
                DrawHelper.CancelDraw(ui.GetDisplay().HoWindow);
                PumpUntilIdle(ui);

                Assert.AreSame(original, Field(ui, "shrFindMode"));
                Assert.IsTrue(original.IsInitialized());
                Assert.AreEqual(100 * 100, Area(original));
            });
        }

        [TestMethod]
        public void ApplyRegion_IsIgnoredWhileDrawing()
        {
            Run(ui =>
            {
                Click(ui, "but_addRegion_Click");
                Click(ui, "but_ApplyRegion_Click");

                Assert.AreEqual(DrawEnum.None, ui.GetDisplay().DrawType, "绘制期间切到 Erase 会让会话收不到鼠标事件");

                DrawHelper.CancelDraw(ui.GetDisplay().HoWindow);
                PumpUntilIdle(ui);
            });
        }

        [TestMethod]
        public void Closing_WhileDrawing_CancelsSessionAndOnlyHides()
        {
            Run(ui =>
            {
                Click(ui, "but_addRegion_Click");
                Assert.IsTrue(ui.IsDrawBusy);

                ui.Close();
                PumpUntilIdle(ui);

                Assert.IsFalse(ui.IsDisposed, "编辑窗是复用的，关窗只隐藏");
                Assert.IsFalse(ui.Visible);
            });
        }

        [TestMethod]
        public void Dispose_ReleasesTemplateHandles()
        {
            Sta.Run(() =>
            {
                var ui = new HEditModelUI();
                WindowHost.ShowOffscreen(ui);
                SetField(ui, "shrFindMode", new HRegion(0.0, 0, 99, 99));
                Click(ui, "but_ApplyRegion_Click");
                EraseStroke(ui, 30, 30);

                var handles = new[] { "_srcImage", "shrErase", "shrFindMode", "_shrContour" };
                var objects = Array.ConvertAll(handles, name => Field(ui, name));

                using (var log = new CapturingLogger())
                {
                    ui.Dispose();
                    Assert.AreEqual(0, log.Entries.FindAll(e => e.Level >= LogLevel.Warn).Count);
                }

                for (int i = 0; i < handles.Length; i++)
                    Assert.IsFalse(objects[i].IsInitialized(), handles[i] + " 应随窗体释放");
            });
        }
    }
}
