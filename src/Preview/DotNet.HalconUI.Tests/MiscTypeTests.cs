using System;
using DotNet.Vision.Abstractions;
using HalconDotNet;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNet.HalconUI.Tests
{
    /// <summary>简单数据类型：默认值与枚举取值（枚举值会被序列化进配置文件，改动即不兼容）。</summary>
    [TestClass]
    public class MiscTypeTests
    {
        [TestMethod]
        public void ZoomImage_Defaults()
        {
            var z = new ZoomImage();
            Assert.AreEqual(1248, z.width.I);
            Assert.AreEqual(2200, z.height.I);
            Assert.IsTrue(z.parent.IsEmpty);
        }

        [TestMethod]
        public void ModelType_Values_AreStable()
        {
            Assert.AreEqual(0, (int)ModelType.NccModel);
            Assert.AreEqual(1, (int)ModelType.ShapeModel);
            Assert.AreEqual(2, (int)ModelType.ScaledModel);
            Assert.AreEqual(3, (int)ModelType.GenericModel);
        }

        [TestMethod]
        public void DrawEnum_Order_IsStable()
        {
            CollectionAssert.AreEqual(
                new[] { DrawEnum.None, DrawEnum.Erase, DrawEnum.DispRect, DrawEnum.DispModel },
                (DrawEnum[])Enum.GetValues(typeof(DrawEnum)));
        }

        [TestMethod]
        public void DrawModelUIArgs_ExposesConstructorArguments()
        {
            var rect = new HObject();
            var contour = new HObject();
            var result = new ModelResult { Row = 1, Column = 2, Angle = 0.5, Score = 0.9 };

            var args = new DrawModelUIArgs("model.shm", rect, contour, result);

            Assert.AreEqual("model.shm", args.ModelPath);
            Assert.AreSame(rect, args.HoModeRect);
            Assert.AreSame(contour, args.HoContour);
            Assert.AreEqual(2, args.Result.Column);
            Assert.AreEqual(0.9, args.Result.Score);
            Assert.IsInstanceOfType(args, typeof(EventArgs));
        }
    }

    /// <summary><see cref="ModelExtension.GetModelContours"/>：按模板类型取轮廓并变换到匹配位姿。</summary>
    [TestClass]
    public class ModelExtensionTests : HalconTestBase
    {
        private const double ModelRow = 100, ModelCol = 120;

        private static HObject SyntheticImage()
        {
            HOperatorSet.GenImageConst(out HObject blank, "byte", 256, 256);
            using (blank)
            using (var rect = Rectangle1(ModelRow - 30, ModelCol - 40, ModelRow + 30, ModelCol + 40))
            {
                HOperatorSet.PaintRegion(rect, blank, out HObject image, 255, "fill");
                return image;
            }
        }

        private static HObject ModelTemplate(HObject image)
        {
            using (var roi = Rectangle1(ModelRow - 50, ModelCol - 60, ModelRow + 50, ModelCol + 60))
            {
                HOperatorSet.ReduceDomain(image, roi, out HObject reduced);
                return reduced;
            }
        }

        private static void Centroid(HObject obj, out double row, out double col)
        {
            HOperatorSet.GetObjClass(obj, out HTuple cls);
            if (cls.S == "region")
            {
                HOperatorSet.AreaCenter(obj, out _, out HTuple r, out HTuple c);
                row = r.D; col = c.D;
                return;
            }
            // 轮廓可能被拆成多段，取全部轮廓外接框的中心
            HOperatorSet.SmallestRectangle1Xld(obj, out HTuple r1, out HTuple c1, out HTuple r2, out HTuple c2);
            row = (r1.TupleMin().D + r2.TupleMax().D) / 2;
            col = (c1.TupleMin().D + c2.TupleMax().D) / 2;
        }

        [TestMethod]
        public void ShapeModel_ContoursAreMovedToResultPose()
        {
            using (var image = SyntheticImage())
            using (var template = ModelTemplate(image))
            {
                HOperatorSet.CreateShapeModel(template, "auto", 0, 0, "auto", "auto", "use_polarity", "auto", "auto", out HTuple modelId);
                try
                {
                    var result = new ModelResult { Row = 30, Column = 40, Angle = 0 };
                    ModelType.ShapeModel.GetModelContours(modelId, result, out HObject contours);
                    using (contours)
                    {
                        Assert.IsTrue(CountObj(contours) > 0);
                        // 模板轮廓以模板参考点为原点，变换后应以 (Row, Column) 为中心
                        Centroid(contours, out double row, out double col);
                        Assert.AreEqual(30, row, 2);
                        Assert.AreEqual(40, col, 2);
                    }
                }
                finally
                {
                    HOperatorSet.ClearShapeModel(modelId);
                }
            }
        }

        [TestMethod]
        public void NccModel_RegionIsMovedToResultPose()
        {
            using (var image = SyntheticImage())
            using (var template = ModelTemplate(image))
            {
                HOperatorSet.CreateNccModel(template, "auto", 0, 0, "auto", "use_polarity", out HTuple modelId);
                try
                {
                    var result = new ModelResult { Row = 60, Column = 70, Angle = 0 };
                    ModelType.NccModel.GetModelContours(modelId, result, out HObject region);
                    using (region)
                    {
                        HOperatorSet.GetObjClass(region, out HTuple cls);
                        Assert.AreEqual("region", cls.S);
                        Centroid(region, out double row, out double col);
                        Assert.AreEqual(60, row, 2);
                        Assert.AreEqual(70, col, 2);
                    }
                }
                finally
                {
                    HOperatorSet.ClearNccModel(modelId);
                }
            }
        }

        [TestMethod]
        public void UnknownModelType_ReturnsEmptyObject()
        {
            ((ModelType)99).GetModelContours(new HTuple(), new ModelResult(), out HObject contours);
            using (contours)
            {
                Assert.IsTrue(contours.IsInitialized());
                Assert.AreEqual(0, CountObj(contours));
            }
        }
    }
}
