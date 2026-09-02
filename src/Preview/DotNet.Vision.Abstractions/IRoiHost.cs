using DotNet.Drawing;
using HalconDotNet;

namespace DotNet.Vision.Abstractions
{
    /// <summary>
    /// ROI 交互宿主：算法策略在「画 ROI / 设模板」时需要宿主提供的全部能力。
    /// </summary>
    /// <remarks>
    /// 原签名直接写 <c>HDisplayUI</c>（一个 WinForms UserControl），把整个算法层
    /// 钉死在 UI 上。实测算法层真正用到的只有下面 6 个成员，故按最小面抽成接口；
    /// <c>HDisplayUI</c> 实现它，算法层不再认识任何控件类型。
    /// </remarks>
    public interface IRoiHost
    {
        /// <summary> 底层绘制窗口 </summary>
        IHDisplay Display { get; }

        /// <summary> 交互式绘制（新建）橡皮筋区域 </summary>
        void DrawRegion(CvRegion hRegion);

        /// <summary> 交互式绘制（修改）橡皮筋区域 </summary>
        void DrawRegionMod(CvRegion hRegion);

        /// <summary> 把 ROI 几何参数回填到宿主的参数面板 </summary>
        void SetRectPara(CvRegion shrRegion);

        /// <summary> 把模板参数回填到宿主的参数面板 </summary>
        void SetModelPara(HObject shrFindMode, HObject shrContour, CvCoord shrCoord);

        /// <summary> 通知宿主：模板创建完成 </summary>
        void DrawDone(string modelPath, HObject ho_ModeRect, HObject ho_Contour, ModelResult result);
    }
}
