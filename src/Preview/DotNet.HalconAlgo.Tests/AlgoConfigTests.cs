using System.IO;
using DotNet.Drawing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNet.HalconAlgo.Tests
{
    [TestClass]
    public class AlgoPathsTests
    {
        private string _savedProjectDir;

        [TestInitialize]
        public void Save() => _savedProjectDir = AlgoPaths.ProjectDir;

        [TestCleanup]
        public void Restore() => AlgoPaths.ProjectDir = _savedProjectDir;

        [TestMethod]
        public void DerivedPaths_FollowProjectDir()
        {
            AlgoPaths.ProjectDir = Path.Combine("root", "proj");

            Assert.AreEqual(Path.Combine("root", "proj", "Scheme"), AlgoPaths.SchemeDir);
            Assert.AreEqual(Path.Combine("root", "proj", "Scheme", "Job"), AlgoPaths.JobDir);
            Assert.AreEqual(Path.Combine("root", "proj", "System.json"), AlgoPaths.System);
        }

        [TestMethod]
        public void FileNames_AreFixed()
        {
            Assert.AreEqual("SchemeInfo.json", AlgoPaths.SchemeInfo);
            Assert.AreEqual("JobInfo.json", AlgoPaths.JobInfo);
        }
    }

    [TestClass]
    public class AlgoFontTests
    {
        [TestMethod]
        public void Defaults()
        {
            var font = new AlgoFont();
            Assert.IsTrue(font.DispText);
            Assert.AreEqual(50, font.FontX);
            Assert.AreEqual(50, font.FontY);
            Assert.AreEqual(15, font.FontSize);
        }
    }

    [TestClass]
    public class EdgeMeasureSetupTests
    {
        private static EdgeMeasureSetup Make(int stepPace = 10, int stepWidth = 5, string contourType = "first")
            => new EdgeMeasureSetup(new Point2d(3, 4), Angle.FromDegrees(30), 20, 40,
                stepPace, stepWidth, 1, 80, "positive", contourType, 640, 480);

        [TestMethod]
        public void Constructor_CopiesGeometry()
        {
            var s = Make();
            Assert.AreEqual(new Point2d(3, 4), s.Center);
            Assert.AreEqual(30, s.Phi.Degrees, 1e-9);
            Assert.AreEqual(20, s.HalfLength);
            Assert.AreEqual(40, s.HalfHeight);
            Assert.AreEqual(1, s.Sigma);
            Assert.AreEqual(80, s.Threshold);
            Assert.AreEqual("positive", s.Transition);
            Assert.AreEqual(640, s.ImageWidth);
            Assert.AreEqual(480, s.ImageHeight);
        }

        [TestMethod]
        public void HalfWidth_IsHalfStepWidth_ClampedToOne()
        {
            Assert.AreEqual(2.5, Make(stepWidth: 5).HalfWidth);
            Assert.AreEqual(1.0, Make(stepWidth: 1).HalfWidth, "半宽不足 1 像素时 gen_measure_rectangle2 会报错，应夹到 1");
            Assert.AreEqual(1.0, Make(stepWidth: 0).HalfWidth);
            Assert.AreEqual(1.0, Make(stepWidth: -4).HalfWidth);
        }

        [TestMethod]
        public void StepPace_ClampedToOne()
        {
            Assert.AreEqual(10, Make(stepPace: 10).StepPace);
            Assert.AreEqual(1, Make(stepPace: 0).StepPace, "步距为 0 会让步数除零");
            Assert.AreEqual(1, Make(stepPace: -3).StepPace);
        }

        [TestMethod]
        public void Second_MapsToAllWithPickIndexOne()
        {
            var s = Make(contourType: "second");
            Assert.AreEqual("all", s.MeasureSelect, "measure_pos 没有 second 选项，需取 all 再挑第 2 条");
            Assert.AreEqual(1, s.PickIndex);
        }

        [DataTestMethod]
        [DataRow("first")]
        [DataRow("last")]
        [DataRow("all")]
        public void OtherSelections_PassThroughWithPickIndexZero(string contourType)
        {
            var s = Make(contourType: contourType);
            Assert.AreEqual(contourType, s.MeasureSelect);
            Assert.AreEqual(0, s.PickIndex);
        }
    }

    [TestClass]
    public class FitParaMappingTests : HalconTestBase
    {
        [DataTestMethod]
        [DataRow("由黑到白", "positive")]
        [DataRow("由白到黑", "negative")]
        [DataRow("全部", "all")]
        [DataRow("未知", "")]
        public void Transition_Mapping(string text, string expected)
        {
            Assert.AreEqual(expected, new FitLine { Transition = text }.GetTransition);
            Assert.AreEqual(expected, new FitArcMidpoint { Transition = text }.GetTransition);
        }

        [DataTestMethod]
        [DataRow("第一条边", "first")]
        [DataRow("第二条边", "second")]
        [DataRow("最后一条", "last")]
        [DataRow("全部", "all")]
        [DataRow("未知", "")]
        public void ContourType_Mapping(string text, string expected)
        {
            Assert.AreEqual(expected, new FitLine { ContourType = text }.GetContourType);
            Assert.AreEqual(expected, new FitArcMidpoint { ContourType = text }.GetContourType);
        }

        [DataTestMethod]
        [DataRow("是", true)]
        [DataRow("否", false)]
        [DataRow("", false)]
        public void TrimEnds_Mapping(string text, bool expected)
        {
            Assert.AreEqual(expected, new FitLine { TrimEnds = text }.IsTrimEnds);
            Assert.AreEqual(expected, new FitArcMidpoint { TrimEnds = text }.IsTrimEnds);
        }

        [TestMethod]
        public void FitLine_Defaults()
        {
            var p = new FitLine();
            Assert.AreEqual(RectEnum.AffRect, p.HoRect.Type, "拟合 ROI 默认是带角度的矩形");
            Assert.AreEqual("默认", p.ImageIn);
            Assert.AreEqual("默认", p.RegionIn);
            Assert.AreEqual("默认", p.CoordIn);
            Assert.AreEqual("positive", p.GetTransition);
            Assert.AreEqual("first", p.GetContourType);
            Assert.AreEqual(80, p.Threshold);
            Assert.IsTrue(p.IsTrimEnds);
            Assert.IsTrue(p.Line.IsDegenerate);
        }

        [TestMethod]
        public void FitArcMidpoint_Defaults()
        {
            var p = new FitArcMidpoint();
            Assert.AreEqual(RectEnum.AffRect, p.HoRect.Type);
            Assert.AreEqual(60, p.Threshold);
            Assert.AreEqual(15.0, p.CoarseGate);
            Assert.IsTrue(p.IsTrimEnds);
        }
    }
}
