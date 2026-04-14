using DotNet.Drawing;
using DotNet.HalconUI;
using HalconDotNet;
using System;
using System.Collections.Generic;

namespace DotNet.VisionMaster
{
    public class RegionMergeStrategy : ParaStrategyBase<RegionMerge>
    {
        public override string Name => "区域合并";
        public override bool Fun_action(DisplayUI display, List<IParaStrategy> strategys)
        {
            HObject regionGet = new HObject(); HOperatorSet.GenEmptyObj(out regionGet);
            HObject imgReduce = new HObject(); HOperatorSet.GenEmptyObj(out imgReduce);

            try
            {
                var region = strategys.ResolveFrom<CvRegion>(inPara.RegionIn);
                var coord = strategys.ResolveFrom<CvCoord>(inPara.CoordIn);

                return true;
            }
            catch
            {
                throw;
            }
            finally
            {
                regionGet.Dispose();
                imgReduce.Dispose();
            }
        }
        public override void GenTreeNode(TreeVisualizer tree)
        {
            tree.Branch(Name, branch => branch
                       .Node("坐标系", OutEnum.Coord, line => line
                           .Branch("原点", pt => pt
                               .Node("行", OutEnum.Number)
                               .Node("列", OutEnum.Number)
                           )
                           .Node("角度", OutEnum.Array)
                       )
                       .Node("区域", OutEnum.Region)
                       .CommonNodes()
                   );

            ClearResolvers();
            RegisterOutput("坐标系", () => inPara.Coord);
            RegisterOutput("坐标系/原点", () => inPara.Coord.Center);
            RegisterOutput("坐标系/原点/行", () => inPara.Coord.Y);
            RegisterOutput("坐标系/原点/列", () => inPara.Coord.X);
            RegisterOutput("坐标系/角度", () => inPara.Coord.Angle);
            RegisterOutput("区域", () => inPara.HoRect);

        }
        public override void DispPara(ParaForm form, Dictionary<string, VsControlModel> VsControls)
        {
            form.ShowTabs(TabPageEnum.Parameter, TabPageEnum.Display);

            VsControls.ShowComboBox(form, "cmb_CoordIn", inPara.CoordIn.ToString(), false);

            for (int i = 0; i < inPara.RegionSources.Length; i++)
            {
                int id = 100 + i;
                VsControls.ShowLabel(form, $"lbl_{id}", $"输入区域{i}");
                VsControls.ShowComboBox(form, $"cmb_{id}", inPara.RegionSources[i], false);
                VsControls.ShowButton(form, $"btn_{id}", true);
            }
        }
        public override void SavePara(ParaForm form, Dictionary<string, VsControlModel> VsControls)
        {
            //基本参数
            inPara.CoordIn = VsControls["cmb_CoordIn"].Text;

            for (int i = 0; i < inPara.RegionSources.Length; i++)
            {
                inPara.RegionSources[i] = VsControls[$"cmb_{100 + i}"].Text;
            }
        }

    }

    public class RegionMerge
    {
        /// <summary> 指令类型 </summary>
        public readonly string Algorithm = "区域合并";

        /// <summary> 跟随坐标 </summary>
        public string CoordIn { set; get; } = "默认";

        /// <summary> 区域来源 </summary>
        public string RegionIn { set; get; } = "默认";

        /// <summary> 区域来源 </summary>
        public string[] RegionSources { get; set; } = new string[6];

        /// <summary> 坐标系 </summary>
        public CvCoord Coord { get; set; } = new CvCoord();

        /// <summary> 区域 </summary>
        public CvRegion HoRect { set; get; } = new CvRegion();
    }

}
