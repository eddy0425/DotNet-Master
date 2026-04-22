using DotNet.Drawing;
using HalconDotNet;
using System.Windows.Forms;
using static DotNet.HalconUI.ModelHandlerFactory;


namespace DotNet.HalconUI
{
    /// <summary>
    /// 擦除矩形处理器
    /// 通过左键拖动以圆形画笔擦除区域
    /// </summary>
    public class EraseRectHandler : IModelHandler
    {
        private bool _editing;

        public bool NeedReDisp => false;

        public void SetUp(EditModelForm display)
        {
            if (display.SetUp != SetUpEnum.None) return;

            display.Reset();
            display.ReDispImage();
            display.SetUp = SetUpEnum.Step1;
        }

        public void OnMouseDown(EditModelForm display, HMouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;

            _editing = true;
            EraseAt(display, e.Y, e.X);
        }

        public void OnMouseUp(EditModelForm display, HMouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left) _editing = false;
        }

        public void OnMouseWheel(EditModelForm display, HMouseEventArgs e)
        {
            DispEraseRegion(display);
        }

        public void OnMouseMove(EditModelForm display, HMouseEventArgs e)
        {
            if (_editing) EraseAt(display, e.Y, e.X);
        }

        private static void EraseAt(EditModelForm display, HTuple row, HTuple column)
        {
            DrawCircle(display, row, column);
            DispEraseRegion(display);
        }

        private static void DrawCircle(EditModelForm display, HTuple row, HTuple column)
        {
            HOperatorSet.GenEmptyObj(out HObject subRegion);
            try
            {
                display.SetDraw("fill");
                HOperatorSet.GenCircle(out subRegion, row, column, display.ShrLineWidth);
                HOperatorSet.Union2(display.ShrErase, subRegion, out display.ShrErase);
                HOperatorSet.Difference(display.ShrFindMode, display.ShrErase, out display.ShrFindMode);
            }
            finally
            {
                display.SetDraw("margin");
                subRegion.Dispose();
            }
        }

        private static void DispEraseRegion(EditModelForm display)
        {
            if (!display.ShrErase.NotNull()) return;

            display.SetDraw("fill");
            display.DispRegion(display.ShrErase, display.ShrColor);
            display.SetDraw("margin");
        }

    }
}
