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
        // 两段式初始化：字段在 SetUp 里赋值而不是构造函数里。
        // 字段不做判空，是因为「事件到达时必然已 SetUp」由调用方的结构保证：
        // 鼠标事件只在 HEditModelUI._drawType == DrawEnum.Erase 时才分发，而该赋值与 SetUp
        // 写在同一个方法里（but_ApplyRegion_Click）。
        private bool _editing;
        private HObject _erase = null;    //擦除区域 ShrErase
        private HObject _findMode = null; //查找模版区域 ShrFindMode
        private HColor _color;
        private int _lineWidth;
        private IHDisplay _display = null;

        public void SetUp(IHDisplay display, HObject shrErase, HObject shrFindMode, HColor color, int lineWidth)
        {
            //display.Reset();
            //display.ReDispImage();
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
