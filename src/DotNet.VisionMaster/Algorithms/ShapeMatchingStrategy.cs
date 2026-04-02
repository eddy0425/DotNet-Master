using HalconDotNet;
using DotNet.HWindows;
using System;
using System.Collections.Generic;

namespace DotNet.VisionMaster
{
    public class ShapeMatchingStrategy : ParaStrategyBase<ShapeMatching>
    {
        char variable = '/';
        char value = ';';

        public override string Name => "形状匹配";
        public override void Init(DrawContext draw)
        {
            draw.RectangleEvent += RectEvent; //AddEvent
        }
        public override void Close(DrawContext draw)
        {
            draw.RectangleEvent -= RectEvent;  //RemoveEvent
        }
        private void RectEvent(object sender, DrawContext.DrawRectangleArgs e)
        {
            if (e.Name == Name)
            {
                inPara.HContext.Re2Point(e.TopLeft, e.BottomRight);
                inPara.HContext.GenRegion();
            }
        }
        public override bool Fun_action(DisplayForm display, List<IParaStrategy> strategys)
        {
            HObject regionGet = new HObject(); HOperatorSet.GenEmptyObj(out regionGet);
            HObject imgReduce = new HObject(); HOperatorSet.GenEmptyObj(out imgReduce);

            try
            {
                var imageSplit = inPara.ImageIn.Split(variable);
                foreach (var strategy in strategys)
                {
                    if (strategy.Name == imageSplit[0])
                    {
                        var value = strategy.GetTreeNode(imageSplit[1]);
                        break;
                    }
                }

                var regionSplit = inPara.RegionIn.Split(variable);
                foreach (var strategy in strategys)
                {
                    if (strategy.Name == regionSplit[0])
                    {
                        var value = strategy.GetTreeNode(regionSplit[1]);
                        break;
                    }
                }

                var coordSplit = inPara.CoordIn.Split(variable);
                foreach (var strategy in strategys)
                {
                    if (strategy.Name == coordSplit[0])
                    {
                        var value = strategy.GetTreeNode(coordSplit[1]);
                        break;
                    }
                }

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
                   );
        }
        public override object GetTreeNode(string tree)
        {
            switch (tree)
            {
                case "坐标系":
                    return inPara.Coord;
                case "原点":
                    return inPara.Coord.center;
                case "行":
                    return inPara.Coord.Y;
                case "列":
                    return inPara.Coord.X;
                case "角度":
                    return inPara.Coord.angle;
                default:
                    return null;
            }
        }
        public override void DispPara(ParaForm form, Dictionary<string, VsControlModel> VsControls)
        {
            form.ShowTabs(TabPageEnum.Parameter, TabPageEnum.Region, TabPageEnum.Matching, TabPageEnum.Display);

            HObjContext hRegion = inPara.HContext;
            VsControls.ShowComboBox(form, "cmb_Width", hRegion.Width.ToString(), false);
            VsControls.ShowComboBox(form, "cmb_Height", hRegion.Height.ToString(), false);
            VsControls.ShowComboBox(form, "cmb_TopLeft", $"{hRegion.TopLeft.X};{hRegion.TopLeft.Y}", false);
            VsControls.ShowComboBox(form, "cmb_BottomRight", $"{hRegion.BottomRight.X};{hRegion.BottomRight.Y}", false);
            VsControls.ShowComboBox(form, "cmb_Center", $"{hRegion.Center.X};{hRegion.Center.Y}", false);

            VsControls.ShowComboBox(form, "cmb_CoordIn", inPara.CoordIn.ToString(), false);

            //基本参数
            VsControls.ShowLabel(form, "lbl_100", "图像来源");
            VsControls.ShowComboBox(form, "cmb_100", inPara.ImageIn.ToString(), false);
            VsControls.ShowButton(form, "btn_100", true);

            VsControls.ShowLabel(form, "lbl_101", "区域来源");
            VsControls.ShowComboBox(form, "cmb_101", inPara.RegionIn, false);
            VsControls.ShowButton(form, "btn_101", true);

            VsControls.ShowLabel(form, "lbl_102", "起始角度");
            VsControls.ShowComboBoxList(form, "cmb_102", inPara.AngleStart.ToString(), new[] { "-90", "-45" });
            VsControls.ShowButton(form, "btn_102", false);

            VsControls.ShowLabel(form, "lbl_103", "增量角度");
            VsControls.ShowComboBoxList(form, "cmb_103", inPara.AngleExtent.ToString(), new[] { "90", "180" });
            VsControls.ShowButton(form, "btn_103", false);

            VsControls.ShowLabel(form, "lbl_104", "最大重叠率");
            VsControls.ShowComboBoxList(form, "cmb_104", inPara.MaxOverlap.ToString(), new[] { "0", "0.3", "0.5" });
            VsControls.ShowButton(form, "btn_104", false);

            VsControls.ShowLabel(form, "lbl_110", "匹配数量");
            VsControls.ShowComboBoxList(form, "cmb_110", inPara.NumMatches == 0 ? "多个" : inPara.NumMatches.ToString(), new[] { "1", "2", "3", "多个" });
            VsControls.ShowButton(form, "btn_110", false);

            VsControls.ShowLabel(form, "lbl_111", "得分");
            VsControls.ShowComboBoxDropDown(form, "cmb_111", inPara.MinScore.ToString(), new[] { "0.5", "0.7" });

            VsControls.ShowLabel(form, "lbl_112", "金字塔");
            VsControls.ShowComboBoxList(form, "cmb_112", inPara.numLevels.ToString(), new[] { "0", "2" });
        }
        public override void SavePara(ParaForm form, Dictionary<string, VsControlModel> VsControls)
        {
            //基本参数
            inPara.CoordIn = VsControls["cmb_CoordIn"].Text;
            inPara.ImageIn = VsControls["cmb_100"].Text;
            inPara.RegionIn = VsControls["cmb_101"].Text;
            inPara.AngleStart = Convert.ToDouble(VsControls["cmb_102"].Text);
            inPara.AngleExtent = Convert.ToDouble(VsControls["cmb_103"].Text);
            inPara.MaxOverlap = Convert.ToDouble(VsControls["cmb_104"].Text);

            inPara.NumMatches = VsControls["cmb_110"].Text == "多个" ? 0 : Convert.ToInt32(VsControls["cmb_110"].Text);
            inPara.MinScore = Convert.ToDouble(VsControls["cmb_111"].Text);
            inPara.numLevels = Convert.ToInt16(VsControls["cmb_112"].Text);
        }
        public override void DispROI(DisplayForm display)
        {
            display.SetDrawMode(Name, inPara.HContext, WinDrawType.DispRect);
        }

    }

    public class ShapeMatching
    {   
        /// <summary> 指令类型 </summary>
        public readonly string Algorithm = "形状匹配";

        /// <summary> 图像来源 </summary>
        public string ImageIn { set; get; } = "默认";

        /// <summary> 区域来源 </summary>
        public string RegionIn { set; get; } = "默认";

        /// <summary> 跟随坐标 </summary>
        public string CoordIn { set; get; } = "默认";

        /// <summary> 坐标系 </summary>
        public CvCoord Coord { get; set; } = new CvCoord();

        /// <summary> 区域 </summary>
        public HObjContext HContext { set; get; } = new HObjContext();

        /// <summary> 起始角度 </summary>
        public double AngleStart { set; get; }

        /// <summary> 增量角度 </summary>
        public double AngleExtent { set; get; }

        /// <summary> 最大重叠率 </summary>
        public double MaxOverlap { set; get; }

        /// <summary> 匹配数量 </summary>
        public int NumMatches { set; get; }

        /// <summary> 得分 </summary>
        public double MinScore { set; get; }

        /// <summary> 金字塔层数 </summary>
        public HTuple numLevels { set; get; } = 1;
    }

}
