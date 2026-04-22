using HalconDotNet;

namespace DotNet.HalconUI
{
    /// <summary>
    /// 绘图处理器接口
    /// 所有绘图类型都需要实现此接口
    /// </summary>
    public interface IModelHandler
    {
        /// <summary>
        /// 获取是否需要重绘图像
        /// </summary>
        bool NeedReDisp { get; }

        /// <summary>
        /// 初始设置
        /// </summary>
        void SetUp(EditModelForm display);

        /// <summary>
        /// 处理鼠标按下事件
        /// </summary>
        void OnMouseDown(EditModelForm display, HMouseEventArgs e);

        /// <summary>
        /// 处理鼠标释放事件
        /// </summary>
        void OnMouseUp(EditModelForm display, HMouseEventArgs e);

        /// <summary>
        /// 处理鼠标滚轮事件
        /// </summary>
        void OnMouseWheel(EditModelForm display, HMouseEventArgs e);

        /// <summary>
        /// 处理鼠标移动事件
        /// </summary>
        void OnMouseMove(EditModelForm display, HMouseEventArgs e);

    }
}
