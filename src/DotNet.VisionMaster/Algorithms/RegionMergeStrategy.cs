using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace DotNet.VisionMaster
{
    public class RegionMergeStrategy : ParaStrategyBase<RegionMerge>
    {
        public override string Name => "区域合并";

        public override bool Fun_action(DisplayForm display, List<IParaStrategy> strategys)
        {
            throw new NotImplementedException();
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
        }
        public override void DispPara(ParaForm form, Dictionary<string, VsControlModel> VsControls)
        {
            form.ShowTabs(TabPageEnum.Parameter, TabPageEnum.Display);

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

        /// <summary> 区域来源 </summary>
        public string[] RegionSources { get; set; } = new string[6];
    }

}
