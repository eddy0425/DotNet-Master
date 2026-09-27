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

        #region 带旋转角度 / 缩放模型 / 通用模型

        // 用 L 形而不是矩形：矩形关于轴对称，旋转方向反了外接框也一样，测不出来
        private const double SceneRow = 130, SceneCol = 128;

        /// <summary>以 (ModelRow, ModelCol) 为参考点的 L 形，按 (angle, scale) 变换到 (row, col)。</summary>
        private static HObject LShape(double row, double col, double angle, double scale = 1)
        {
            using (var vertical = Rectangle1(ModelRow - 30, ModelCol - 40, ModelRow + 30, ModelCol - 20))
            using (var bottom = Rectangle1(ModelRow + 10, ModelCol - 40, ModelRow + 30, ModelCol + 40))
            {
                HOperatorSet.Union2(vertical, bottom, out HObject l);
                using (l)
                    return Transform(l, row, col, angle, scale);
            }
        }

        private static HObject Transform(HObject region, double row, double col, double angle, double scale = 1)
        {
            HOperatorSet.HomMat2dIdentity(out HTuple mat);
            HOperatorSet.HomMat2dScale(mat, scale, scale, ModelRow, ModelCol, out mat);
            HOperatorSet.HomMat2dRotate(mat, angle, ModelRow, ModelCol, out mat);
            HOperatorSet.HomMat2dTranslate(mat, row - ModelRow, col - ModelCol, out mat);
            HOperatorSet.AffineTransRegion(region, out HObject moved, mat, "nearest_neighbor");
            return moved;
        }

        private static HObject LImage(double row = ModelRow, double col = ModelCol, double angle = 0, double scale = 1)
        {
            HOperatorSet.GenImageConst(out HObject blank, "byte", 256, 256);
            using (blank)
            using (var l = LShape(row, col, angle, scale))
            {
                HOperatorSet.PaintRegion(l, blank, out HObject image, 255, "fill");
                return image;
            }
        }

        private static double[] Box(HObject obj)
        {
            HOperatorSet.GetObjClass(obj, out HTuple cls);
            HTuple r1, c1, r2, c2;
            if (cls.S == "region") HOperatorSet.SmallestRectangle1(obj, out r1, out c1, out r2, out c2);
            else HOperatorSet.SmallestRectangle1Xld(obj, out r1, out c1, out r2, out c2);
            return new[] { r1.TupleMin().D, c1.TupleMin().D, r2.TupleMax().D, c2.TupleMax().D };
        }

        /// <summary>亚像素边缘与像素中心差半个像素，再加最近邻取整，默认给 1.5 像素容差。</summary>
        private static void AssertBox(double[] expected, double[] actual, double tolerance = 1.5)
        {
            string msg = $"期望 [{string.Join(",", expected)}] 实际 [{string.Join(",", actual)}]";
            for (int i = 0; i < 4; i++)
                Assert.AreEqual(expected[i], actual[i], tolerance, msg);
        }

        private static double[] ExpectedLBox(double angle, double scale = 1)
        {
            using (var l = LShape(SceneRow, SceneCol, angle, scale))
                return Box(l);
        }

        [DataTestMethod]
        [DataRow(0.0)]
        [DataRow(Math.PI / 6)]
        [DataRow(-Math.PI / 2)]
        public void ShapeModel_RotatedResult_ContoursCoverRotatedObject(double angle)
        {
            using (var image = LImage())
            using (var template = ModelTemplate(image))
            {
                HOperatorSet.CreateShapeModel(template, "auto", -Math.PI, 2 * Math.PI, "auto", "auto", "use_polarity", "auto", "auto", out HTuple modelId);
                try
                {
                    var result = new ModelResult { Row = SceneRow, Column = SceneCol, Angle = angle };
                    ModelType.ShapeModel.GetModelContours(modelId, result, out HObject contours);
                    using (contours)
                        AssertBox(ExpectedLBox(angle), Box(contours));
                }
                finally
                {
                    HOperatorSet.ClearShapeModel(modelId);
                }
            }
        }

        [DataTestMethod]
        [DataRow(0.0)]
        [DataRow(Math.PI / 6)]
        [DataRow(-Math.PI / 2)]
        public void ScaledModel_RotatedResult_ContoursCoverRotatedObject(double angle)
        {
            using (var image = LImage())
            using (var template = ModelTemplate(image))
            {
                HOperatorSet.CreateScaledShapeModel(template, "auto", -Math.PI, 2 * Math.PI, "auto", 0.8, 1.2, "auto",
                    "auto", "use_polarity", "auto", "auto", out HTuple modelId);
                try
                {
                    var result = new ModelResult { Row = SceneRow, Column = SceneCol, Angle = angle };
                    ModelType.ScaledModel.GetModelContours(modelId, result, out HObject contours);
                    using (contours)
                    {
                        HOperatorSet.GetObjClass(contours, out HTuple cls);
                        Assert.AreEqual("xld_cont", cls.S);
                        AssertBox(ExpectedLBox(angle), Box(contours));
                    }
                }
                finally
                {
                    HOperatorSet.ClearShapeModel(modelId);
                }
            }
        }

        [TestMethod]
        public void ScaledModel_FoundScaledMatch_ContoursStayAtModelSize()
        {
            // ModelResult 没有缩放字段，这里只能给出 1 倍大小的轮廓；
            // 按实际缩放显示由 ScaledModelStrategy 自行计算（见其 ScaledContour）。
            using (var image = LImage())
            using (var template = ModelTemplate(image))
            {
                HOperatorSet.CreateScaledShapeModel(template, "auto", -Math.PI, 2 * Math.PI, "auto", 0.8, 1.2, "auto",
                    "auto", "use_polarity", "auto", "auto", out HTuple modelId);
                try
                {
                    using (var scene = LImage(SceneRow, SceneCol, Math.PI / 6, 1.15))
                    {
                        HOperatorSet.FindScaledShapeModel(scene, modelId, -Math.PI, 2 * Math.PI, 0.8, 1.2, 0.5, 1, 0.5,
                            "least_squares", 0, 0.9, out HTuple row, out HTuple col, out HTuple angle, out HTuple scale, out HTuple score);
                        Assert.AreEqual(1, row.Length, "场景中应找到缩放后的目标");
                        Assert.AreEqual(Math.PI / 6, angle.D, 0.02);
                        Assert.AreEqual(1.15, scale.D, 0.03);

                        var result = new ModelResult(row.D, col.D, angle.D, score.D);
                        ModelType.ScaledModel.GetModelContours(modelId, result, out HObject contours);
                        using (contours)
                            AssertBox(ExpectedLBox(angle.D), Box(contours), 2);
                    }
                }
                finally
                {
                    HOperatorSet.ClearShapeModel(modelId);
                }
            }
        }

        [DataTestMethod]
        [DataRow(Math.PI / 6)]
        [DataRow(-Math.PI / 2)]
        public void NccModel_RotatedResult_RegionIsRotatedTemplateDomain(double angle)
        {
            using (var image = LImage())
            using (var template = ModelTemplate(image))
            {
                HOperatorSet.CreateNccModel(template, "auto", -Math.PI, 2 * Math.PI, "auto", "use_polarity", out HTuple modelId);
                try
                {
                    var result = new ModelResult { Row = SceneRow, Column = SceneCol, Angle = angle };
                    ModelType.NccModel.GetModelContours(modelId, result, out HObject region);
                    using (region)
                    using (var roi = Rectangle1(ModelRow - 50, ModelCol - 60, ModelRow + 50, ModelCol + 60))
                    using (var expected = Transform(roi, SceneRow, SceneCol, angle))
                    {
                        // NCC 只有模板区域没有轮廓：结果应是模板 ROI 按同一位姿旋转平移后的区域
                        AssertBox(Box(expected), Box(region));
                        Assert.AreEqual(Area(expected), Area(region), Area(expected) * 0.02);
                    }
                }
                finally
                {
                    HOperatorSet.ClearNccModel(modelId);
                }
            }
        }

        [DataTestMethod]
        [DataRow(0.0)]
        [DataRow(Math.PI / 6)]
        public void GenericModel_ReturnsContoursOfFoundMatch(double sceneAngle)
        {
            using (var image = LImage())
            using (var template = ModelTemplate(image))
            using (var scene = LImage(SceneRow, SceneCol, sceneAngle))
            {
                HOperatorSet.CreateGenericShapeModel(out HTuple modelId);
                HTuple matchId = null;
                try
                {
                    HOperatorSet.SetGenericShapeModelParam(modelId, "metric", "use_polarity");
                    HOperatorSet.TrainGenericShapeModel(template, modelId);
                    HOperatorSet.SetGenericShapeModelParam(modelId, "angle_start", -Math.PI);
                    HOperatorSet.SetGenericShapeModelParam(modelId, "angle_end", Math.PI);
                    HOperatorSet.SetGenericShapeModelParam(modelId, "min_score", 0.5);
                    HOperatorSet.SetGenericShapeModelParam(modelId, "num_matches", 1);

                    HOperatorSet.FindGenericShapeModel(scene, modelId, out matchId, out HTuple num);
                    Assert.AreEqual(1, num.I);
                    HOperatorSet.GetGenericShapeModelResult(matchId, 0, "angle", out HTuple angle);
                    Assert.AreEqual(sceneAngle, angle.D, 0.02);

                    // 通用模型不看 Row/Column/Angle，只认 ResultID —— 故意填错位姿以证明这一点
                    var result = new ModelResult(0, 0, 0, 1) { ResultID = matchId };
                    ModelType.GenericModel.GetModelContours(modelId, result, out HObject contours);
                    using (contours)
                    {
                        Assert.IsTrue(CountObj(contours) > 0);
                        AssertBox(ExpectedLBox(sceneAngle), Box(contours));
                    }
                }
                finally
                {
                    if (matchId != null && matchId.Length > 0) HOperatorSet.ClearHandle(matchId);
                    HOperatorSet.ClearHandle(modelId);
                }
            }
        }

        [TestMethod]
        public void GenericModel_WithoutResultHandle_ThrowsArgumentException()
        {
            // 构造函数给空 tuple；default(ModelResult) 则是 null —— 两种都是调用方漏传了匹配结果
            foreach (var result in new[] { new ModelResult(1, 2, 0, 1), default(ModelResult) })
            {
                HObject contours = null;
                var ex = Assert.ThrowsException<ArgumentException>(() =>
                    ModelType.GenericModel.GetModelContours(new HTuple(), result, out contours),
                    "应给出指明 ResultID 的参数异常，而不是 HALCON #1401");
                Assert.AreEqual("result", ex.ParamName);
                Assert.IsNull(contours);
            }
        }

        #endregion

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
