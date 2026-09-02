using DotNet.Drawing;
using HalconDotNet;
using System.Windows.Forms;
using DotNet.Vision.Abstractions;


namespace DotNet.HalconUI
{
    /// <summary>
    /// 擦除矩形处理器
    /// 通过左键拖动以圆形画笔擦除区域
    /// </summary>
    public class EraseRectMouse : IMouseHandler
    {
        private enum Phase { Idle, Drawing, Editing }

        private bool _editing;
        private HObject _erase;    //擦除区域 ShrErase
        private HObject _findMode; //查找模版区域 ShrFindMode
        private Phase _phase;
        private HColor _color;
        private int _lineWidth;
        private IHDisplay _display;

        public void SetUp(IHDisplay display, HObject shrErase, HObject shrFindMode, HColor color, int lineWidth)
        {
            //display.Reset();
            //display.ReDispImage();
            _phase = Phase.Idle;
            _display = display;
            _erase = shrErase;
            _findMode = shrFindMode;
            _color = color;
            _lineWidth = lineWidth;
        }

        public void SetPara(HColor color, int lineWidth)
        {
            _color = color;
            _lineWidth = lineWidth;
        }

        public void OnMouseDown(HMouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;

            _editing = true;
            EraseAt(e.Y, e.X);
        }

        public void OnMouseUp(HMouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left) _editing = false;
        }

        public void OnMouseWheel(HMouseEventArgs e)
        {
            DispEraseRegion();
        }

        public void OnMouseMove(HMouseEventArgs e)
        {
            if (_editing) EraseAt(e.Y, e.X);
        }

        private void EraseAt(HTuple row, HTuple column)
        {
            DrawCircle(row, column);
            DispEraseRegion();
        }

        private void DrawCircle(HTuple row, HTuple column)
        {
            HOperatorSet.GenEmptyObj(out HObject subRegion);
            try
            {
                _display.SetDraw("fill");
                HOperatorSet.GenCircle(out subRegion, row, column, _lineWidth);

                if (_erase.CountObj() > 0)
                {
                    HOperatorSet.Union2(_erase, subRegion, out HObject regionUnion);
                    _erase.Dispose();
                    _erase = regionUnion;
                }
                else
                {
                    _erase.Dispose();
                    HOperatorSet.CopyObj(subRegion, out _erase, 1, -1);
                }

                if (_findMode.CountObj() > 0)
                {
                    HOperatorSet.Difference(_findMode, _erase, out HObject regionDifference);
                    _findMode.Dispose();
                    _findMode = regionDifference;
                }
            }
            finally
            {
                _display.SetDraw("margin");
                subRegion.Dispose();
            }
        }

        private void DispEraseRegion()
        {
            if (!_erase.NotNull()) return;

            // 颜色与填充模式一并由 DrawStyle 描述，省掉一次单独的 SetDraw
            _display.Disp(_erase, new DrawStyle { Color = _color, DrawMode = "fill" });
            _display.SetDraw("margin");
        }

    }
}
