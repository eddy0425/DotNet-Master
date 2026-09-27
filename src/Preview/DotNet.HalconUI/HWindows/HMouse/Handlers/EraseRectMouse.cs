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

        /// <summary> 当前累计的擦除区域 </summary>
        /// <remarks>
        /// <see cref="SetUp"/> 传入的句柄所有权随之转给本类：每次涂抹都会生成新对象并释放旧对象，
        /// 调用方原先持有的引用在第一次涂抹后即失效，必须改读本属性（见 <c>HEditModelUI.SyncEraseResult</c>）。
        /// </remarks>
        public HObject Erase => _erase;

        /// <summary> 扣除擦除区域后的模板区域，所有权约定同 <see cref="Erase"/> </summary>
        public HObject FindMode => _findMode;

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
            // 不预先 GenEmptyObj：紧接着的 GenCircle(out …) 会覆盖掉它，占位句柄随之泄漏。
            HObject subRegion = null;
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

                // 只扣本次笔刷：_erase 是跨多次 SetUp 累计的显示用区域，
                // 用它做差会把用户之后重新添加回来的部分再扣掉一次。
                if (_findMode.CountObj() > 0)
                {
                    HOperatorSet.Difference(_findMode, subRegion, out HObject regionDifference);
                    _findMode.Dispose();
                    _findMode = regionDifference;
                }
            }
            finally
            {
                _display.SetDraw("margin");
                subRegion?.Dispose();
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
