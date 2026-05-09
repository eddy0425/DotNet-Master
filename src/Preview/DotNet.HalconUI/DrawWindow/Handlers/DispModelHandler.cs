using HalconDotNet;


namespace DotNet.HalconUI
{
    /// <summary>
    /// 设置模型绘图处理器
    /// 用于显示模型匹配结果
    /// </summary>
    public class DispModelHandler : IDrawHandler
    {
        public bool NeedReDisp => false;
        private enum SetUpEnum { None, Step1, Step2, Step3, Step4, Step5 }
        private SetUpEnum _phase;
        public void SetUp(DisplayUI display)
        {
            _phase = SetUpEnum.None;
            if (_phase == SetUpEnum.None)
            {
                display.Reset();
                display.ReDispImage();
                _phase = SetUpEnum.Step1;
            }
        }

        public void OnMouseDown(DisplayUI display, HMouseEventArgs e)
        {
            // 无操作
        }

        public void OnMouseUp(DisplayUI display, HMouseEventArgs e)
        {
            // 无操作
        }

        public void OnMouseWheel(DisplayUI display, HMouseEventArgs e)
        {
            // 无操作
        }

        public void OnMouseMove(DisplayUI display, HMouseEventArgs e)
        {
            switch (_phase)
            {
                case SetUpEnum.Step1:
                    display.DrawDispModel(display.AlgoName, display);
                    break;
            }
        }
    }
}
