using HalconDotNet;

namespace DotNet.VisionMaster
{
    /// <summary>
    /// 绘图处理器接口
    /// 所有绘图类型都需要实现此接口
    /// </summary>
    public interface IDrawHandler
    {
        /// <summary>
        /// 获取是否需要重绘图像
        /// </summary>
        bool NeedReDispImage { get; }

        /// <summary>
        /// 初始设置
        /// </summary>
        void SetUp(DrawContext context);

        /// <summary>
        /// 处理鼠标按下事件
        /// </summary>
        void OnMouseDown(DrawContext context, HMouseEventArgs e);

        /// <summary>
        /// 处理鼠标释放事件
        /// </summary>
        void OnMouseUp(DrawContext context, HMouseEventArgs e);

        /// <summary>
        /// 处理鼠标滚轮事件
        /// </summary>
        void OnMouseWheel(DrawContext context, HMouseEventArgs e);

        /// <summary>
        /// 处理鼠标移动事件
        /// </summary>
        void OnMouseMove(DrawContext context, HMouseEventArgs e);

    }
}
