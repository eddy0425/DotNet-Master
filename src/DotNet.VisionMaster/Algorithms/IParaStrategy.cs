using HalconDotNet;
using System.Collections.Generic;

namespace DotNet.VisionMaster
{
    /// <summary>
    /// 算法参数策略接口
    /// </summary>
    public interface IParaStrategy
    {
        string Name { get; }
        void Init(DrawContext draw);
        void Close(DrawContext draw);
        void GenTreeNode(TreeVisualizer tree);
        object GetTreeNode(string tree);
        void DispPara(ParaForm form, Dictionary<string, VsControlModel> VsControls);
        void SavePara(ParaForm form, Dictionary<string, VsControlModel> VsControls);
        void DispROI(DisplayForm display);
        bool Fun_action(DisplayForm display, List<IParaStrategy> strategys);
    }

    /// <summary>
    /// 策略抽象基类：自动初始化参数实例，子类只需实现 DispPara / SavePara
    /// </summary>
    public abstract class ParaStrategyBase<T> : IParaStrategy where T : class, new()
    {
        public abstract string Name { get; }
        public T inPara { get; set; } = new T();
        public virtual void Init(DrawContext draw) { }
        public virtual void Close(DrawContext draw) { }
        public abstract void GenTreeNode(TreeVisualizer tree);
        public abstract object GetTreeNode(string tree);
        public abstract void DispPara(ParaForm form, Dictionary<string, VsControlModel> VsControls);
        public abstract void SavePara(ParaForm form, Dictionary<string, VsControlModel> VsControls);
        public virtual void DispROI(DisplayForm display) { }
        public abstract bool Fun_action(DisplayForm display, List<IParaStrategy> strategys);
    }
}
