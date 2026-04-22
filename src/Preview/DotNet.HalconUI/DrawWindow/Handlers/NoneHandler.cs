using HalconDotNet;


namespace DotNet.HalconUI
{
    /// <summary>
    /// 空绘图处理器
    /// 当不需要绘图时使用
    /// </summary>
    internal class NoneHandler : IModelHandler
    {
        public bool NeedReDisp => false;

        public void SetUp(EditModelForm display)
        {
            // 无操作
        }

        public void OnMouseDown(EditModelForm display, HMouseEventArgs e)
        {
            // 无操作
        }

        public void OnMouseUp(EditModelForm display, HMouseEventArgs e)
        {
            // 无操作
        }

        public void OnMouseWheel(EditModelForm display, HMouseEventArgs e)
        {
            // 无操作
        }

        public void OnMouseMove(EditModelForm display, HMouseEventArgs e)
        {
            // 无操作
        }

    }
}
