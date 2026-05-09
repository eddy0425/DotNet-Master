using HalconDotNet;

namespace DotNet.HalconUI
{
    /// <summary>
    /// 绘图处理器接口
    /// 所有绘图类型都需要实现此接口
    /// </summary>
    public interface IDrawHandler
    {
        //private enum SetUpEnum { None, Step1, Step2, Step3, Step4, Step5 }
        //private enum CycleMoveEnum { None, Start, StartMove, End, EndMove, Center, CenterMove }

        /// <summary>
        /// 获取是否需要重绘图像
        /// </summary>
        bool NeedReDisp { get; }

        /// <summary>
        /// 初始设置
        /// </summary>
        void SetUp(DisplayUI display);

        /// <summary>
        /// 处理鼠标按下事件
        /// </summary>
        void OnMouseDown(DisplayUI display, HMouseEventArgs e);

        /// <summary>
        /// 处理鼠标释放事件
        /// </summary>
        void OnMouseUp(DisplayUI display, HMouseEventArgs e);

        /// <summary>
        /// 处理鼠标滚轮事件
        /// </summary>
        void OnMouseWheel(DisplayUI display, HMouseEventArgs e);

        /// <summary>
        /// 处理鼠标移动事件
        /// </summary>
        void OnMouseMove(DisplayUI display, HMouseEventArgs e);

    }
}
