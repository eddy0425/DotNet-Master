using System;
using HalconDotNet;

namespace DotNet.Drawing
{
    /// <summary>
    /// Halcon 模板匹配模型配置
    /// </summary>
    /// <remarks>
    /// 设计特点：
    /// - sealed record class: 不可变引用类型，线程安全
    /// - 使用 with 表达式进行函数式更新，避免冗余的 With* 方法
    /// - 提供丰富的预设配置和工厂方法
    /// </remarks>
    public sealed record CvModel
    {
        #region Properties

        /// <summary>
        /// 模板ID
        /// </summary>
        public HTuple? ModelID { get; init; }

        /// <summary>
        /// 是否锁定中心
        /// </summary>
        public bool LockCenter { get; init; } = true;

        /// <summary>
        /// 起始角度（弧度）
        /// </summary>
        public double AngleStart { get; init; } = -Math.PI / 2;

        /// <summary>
        /// 终点角度（弧度）
        /// </summary>
        public double AngleEnd { get; init; } = Math.PI;

        /// <summary>
        /// 角度范围（弧度）
        /// </summary>
        public double AngleExtent { get; init; } = Math.PI;

        /// <summary>
        /// 最小匹配分数 (0.0 - 1.0)
        /// </summary>
        public double MinScore { get; init; } = 0.6;

        /// <summary>
        /// 匹配数量
        /// </summary>
        public int NumMatches { get; init; } = 1;

        /// <summary>
        /// 最大重叠率 (0.0 - 1.0)
        /// </summary>
        public double MaxOverlap { get; init; } = 0.5;

        /// <summary>
        /// 亚像素精度设置
        /// </summary>
        public SubPixelMode SubPixel { get; init; } = SubPixelMode.LeastSquares;

        /// <summary>
        /// 金字塔层数（0 表示自动）
        /// </summary>
        public int NumLevels { get; init; } = 0;

        /// <summary>
        /// 贪婪系数 (0.0 - 1.0)
        /// 0: 安全但缓慢
        /// 1: 快速但可能错过匹配
        /// </summary>
        public double Greediness { get; init; } = 0.7;

        /// <summary>
        /// 最小缩放比例
        /// </summary>
        public double ScaleMin { get; init; } = 0.8;

        /// <summary>
        /// 最大缩放比例
        /// </summary>
        public double ScaleMax { get; init; } = 1.2;

        /// <summary>
        /// 对比度阈值（用于边缘检测）
        /// </summary>
        public int Contrast { get; init; } = 30;

        /// <summary>
        /// 最小对比度阈值
        /// </summary>
        public int MinContrast { get; init; } = 10;

        /// <summary>
        /// 匹配度量类型
        /// </summary>
        public MetricType Metric { get; init; } = MetricType.UsePolarity;

        /// <summary>
        /// 是否启用缩放匹配
        /// </summary>
        public bool EnableScaling { get; init; } = false;

        #endregion

        #region Computed Properties

        /// <summary>
        /// 角度起始（度数）
        /// </summary>
        public double AngleStartDegrees => AngleStart * 180.0 / Math.PI;

        /// <summary>
        /// 角度终止（度数）
        /// </summary>
        public double AngleEndDegrees => AngleEnd * 180.0 / Math.PI;

        /// <summary>
        /// 角度范围（度数）
        /// </summary>
        public double AngleExtentDegrees => AngleExtent * 180.0 / Math.PI;

        /// <summary>
        /// 是否为全角度搜索
        /// </summary>
        public bool IsFullAngleSearch => MathHelper.AreEqual(Math.Abs(AngleExtent), 2 * Math.PI);

        /// <summary>
        /// 获取缩放比例范围
        /// </summary>
        /// <param name="min">最小缩放</param>
        /// <param name="max">最大缩放</param>
        public void GetScaleRange(out double min, out double max)
        {
            min = ScaleMin;
            max = ScaleMax;
        }

        /// <summary>
        /// 亚像素模式字符串（用于 Halcon）
        /// </summary>
        public string SubPixelString
        {
            get
            {
                switch (SubPixel)
                {
                    case SubPixelMode.None: return "none";
                    case SubPixelMode.Interpolation: return "interpolation";
                    case SubPixelMode.LeastSquares: return "least_squares";
                    case SubPixelMode.LeastSquaresHigh: return "least_squares_high";
                    case SubPixelMode.LeastSquaresVeryHigh: return "least_squares_very_high";
                    default: return "least_squares";
                }
            }
        }

        /// <summary>
        /// 度量类型字符串（用于 Halcon）
        /// </summary>
        public string MetricString
        {
            get
            {
                switch (Metric)
                {
                    case MetricType.UsePolarity: return "use_polarity";
                    case MetricType.IgnoreGlobalPolarity: return "ignore_global_polarity";
                    case MetricType.IgnoreLocalPolarity: return "ignore_local_polarity";
                    case MetricType.IgnoreColorPolarity: return "ignore_color_polarity";
                    default: return "use_polarity";
                }
            }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// 默认构造函数
        /// </summary>
        public CvModel()
        {
        }

        /// <summary>
        /// 从模板ID构造
        /// </summary>
        public CvModel(HTuple modelID)
        {
            ModelID = modelID;
        }

        /// <summary>
        /// 常用参数构造
        /// </summary>
        public CvModel(HTuple modelID, double minScore, int numMatches = 1)
        {
            ModelID = modelID;
            MinScore = ValidateScore(minScore);
            NumMatches = ValidatePositive(numMatches, nameof(numMatches));
        }

        #endregion

        #region Factory Methods

        /// <summary>
        /// 创建高精度匹配配置
        /// </summary>
        public static CvModel HighPrecision() => new()
        {
            MinScore = 0.8,
            Greediness = 0.5,
            SubPixel = SubPixelMode.LeastSquaresHigh,
            NumLevels = 0
        };

        /// <summary>
        /// 创建高速匹配配置
        /// </summary>
        public static CvModel HighSpeed() => new()
        {
            MinScore = 0.5,
            Greediness = 0.9,
            SubPixel = SubPixelMode.None,
            NumLevels = 4
        };

        /// <summary>
        /// 创建多目标匹配配置
        /// </summary>
        public static CvModel MultiTarget(int numMatches = 10, double maxOverlap = 0.3) => new()
        {
            NumMatches = numMatches,
            MaxOverlap = maxOverlap,
            MinScore = 0.5
        };

        /// <summary>
        /// 创建全角度搜索配置
        /// </summary>
        public static CvModel FullAngle() => new()
        {
            AngleStart = -Math.PI,
            AngleEnd = Math.PI,
            AngleExtent = 2 * Math.PI
        };

        /// <summary>
        /// 创建缩放匹配配置
        /// </summary>
        public static CvModel WithScaling(double scaleMin = 0.8, double scaleMax = 1.2) => new()
        {
            EnableScaling = true,
            ScaleMin = scaleMin,
            ScaleMax = scaleMax
        };

        /// <summary>
        /// 创建稳健匹配配置（容忍更多变化）
        /// </summary>
        public static CvModel Robust() => new()
        {
            MinScore = 0.4,
            Greediness = 0.6,
            Metric = MetricType.IgnoreGlobalPolarity,
            MinContrast = 5
        };

        #endregion

        #region Fluent Configuration Methods

        /// <summary>
        /// 创建修改了最小匹配分数的新实例
        /// </summary>
        public CvModel WithMinScore(double minScore)
        {
            return this with { MinScore = ValidateScore(minScore) };
        }

        /// <summary>
        /// 创建修改了匹配数量的新实例
        /// </summary>
        public CvModel WithNumMatches(int numMatches)
        {
            return this with { NumMatches = ValidatePositive(numMatches, nameof(numMatches)) };
        }

        /// <summary>
        /// 创建修改了模板ID的新实例
        /// </summary>
        public CvModel WithModelID(HTuple? modelID)
        {
            return this with { ModelID = modelID };
        }

        /// <summary>
        /// 创建修改了贪婪系数的新实例
        /// </summary>
        public CvModel WithGreediness(double greediness)
        {
            return this with { Greediness = ValidateScore(greediness) };
        }

        /// <summary>
        /// 创建修改了最大重叠率的新实例
        /// </summary>
        public CvModel WithMaxOverlap(double maxOverlap)
        {
            return this with { MaxOverlap = ValidateScore(maxOverlap) };
        }

        /// <summary>
        /// 创建修改了亚像素模式的新实例
        /// </summary>
        public CvModel WithSubPixel(SubPixelMode subPixel)
        {
            return this with { SubPixel = subPixel };
        }

        /// <summary>
        /// 创建修改了金字塔层数的新实例
        /// </summary>
        public CvModel WithNumLevels(int numLevels)
        {
            if (numLevels < 0) throw new ArgumentOutOfRangeException(nameof(numLevels));
            return this with { NumLevels = numLevels };
        }

        /// <summary>
        /// 创建修改了锁定中心的新实例
        /// </summary>
        public CvModel WithLockCenter(bool lockCenter)
        {
            return this with { LockCenter = lockCenter };
        }

        /// <summary>
        /// 设置角度范围（弧度）
        /// </summary>
        public CvModel WithAngleRange(double start, double end)
        {
            return this with
            {
                AngleStart = start,
                AngleEnd = end,
                AngleExtent = Math.Abs(end - start)
            };
        }

        /// <summary>
        /// 设置角度范围（度数）
        /// </summary>
        public CvModel WithAngleRangeDegrees(double startDeg, double endDeg)
        {
            return WithAngleRange(startDeg * Math.PI / 180.0, endDeg * Math.PI / 180.0);
        }

        /// <summary>
        /// 设置缩放范围
        /// </summary>
        public CvModel WithScaleRange(double min, double max)
        {
            if (min < 0 || max < 0 || min > max)
                throw new ArgumentException("Invalid scale range");

            return this with
            {
                EnableScaling = true,
                ScaleMin = min,
                ScaleMax = max
            };
        }

        /// <summary>
        /// 设置匹配参数
        /// </summary>
        public CvModel WithMatchParameters(double minScore, int numMatches = 1, double maxOverlap = 0.5)
        {
            return this with
            {
                MinScore = ValidateScore(minScore),
                NumMatches = ValidatePositive(numMatches, nameof(numMatches)),
                MaxOverlap = ValidateScore(maxOverlap)
            };
        }

        /// <summary>
        /// 设置搜索参数
        /// </summary>
        public CvModel WithSearchParameters(double greediness, int numLevels = 0)
        {
            return this with
            {
                Greediness = ValidateScore(greediness),
                NumLevels = numLevels >= 0 ? numLevels : throw new ArgumentOutOfRangeException(nameof(numLevels))
            };
        }

        #endregion

        #region Halcon Parameter Conversion

        /// <summary>
        /// 获取 find_shape_model 参数
        /// </summary>
        public void GetFindShapeModelParams(
            out HTuple angleStart, out HTuple angleExtent, out HTuple minScore, out HTuple numMatches,
            out HTuple maxOverlap, out HTuple subPixel, out HTuple numLevels, out HTuple greediness)
        {
            angleStart = new HTuple(AngleStart);
            angleExtent = new HTuple(AngleExtent);
            minScore = new HTuple(MinScore);
            numMatches = new HTuple(NumMatches);
            maxOverlap = new HTuple(MaxOverlap);
            subPixel = new HTuple(SubPixelString);
            numLevels = new HTuple(NumLevels);
            greediness = new HTuple(Greediness);
        }

        /// <summary>
        /// 获取 find_shape_model 参数（HTuple 数组形式）
        /// </summary>
        public HTuple[] GetFindShapeModelParamsArray()
        {
            return new HTuple[]
            {
                new HTuple(AngleStart),
                new HTuple(AngleExtent),
                new HTuple(MinScore),
                new HTuple(NumMatches),
                new HTuple(MaxOverlap),
                new HTuple(SubPixelString),
                new HTuple(NumLevels),
                new HTuple(Greediness)
            };
        }

        /// <summary>
        /// 获取 find_scaled_shape_model 参数
        /// </summary>
        public void GetFindScaledShapeModelParams(
            out HTuple angleStart, out HTuple angleExtent, out HTuple scaleMin, out HTuple scaleMax,
            out HTuple minScore, out HTuple numMatches, out HTuple maxOverlap, out HTuple subPixel,
            out HTuple numLevels, out HTuple greediness)
        {
            angleStart = new HTuple(AngleStart);
            angleExtent = new HTuple(AngleExtent);
            scaleMin = new HTuple(ScaleMin);
            scaleMax = new HTuple(ScaleMax);
            minScore = new HTuple(MinScore);
            numMatches = new HTuple(NumMatches);
            maxOverlap = new HTuple(MaxOverlap);
            subPixel = new HTuple(SubPixelString);
            numLevels = new HTuple(NumLevels);
            greediness = new HTuple(Greediness);
        }

        /// <summary>
        /// 获取 find_scaled_shape_model 参数（HTuple 数组形式）
        /// </summary>
        public HTuple[] GetFindScaledShapeModelParamsArray()
        {
            return new HTuple[]
            {
                new HTuple(AngleStart),
                new HTuple(AngleExtent),
                new HTuple(ScaleMin),
                new HTuple(ScaleMax),
                new HTuple(MinScore),
                new HTuple(NumMatches),
                new HTuple(MaxOverlap),
                new HTuple(SubPixelString),
                new HTuple(NumLevels),
                new HTuple(Greediness)
            };
        }

        #endregion

        #region Validation

        private static double ValidateScore(double value)
        {
            if (value < 0 || value > 1)
                throw new ArgumentOutOfRangeException(nameof(value), "Score must be between 0 and 1");
            return value;
        }

        private static int ValidatePositive(int value, string paramName)
        {
            if (value <= 0)
                throw new ArgumentOutOfRangeException(paramName, "Value must be positive");
            return value;
        }

        #endregion

        #region Equality

        public bool Equals(CvModel? other)
        {
            if (other is null) return false;
            return ModelID?.D == other.ModelID?.D &&
                   LockCenter == other.LockCenter &&
                   MathHelper.AreEqual(AngleStart, other.AngleStart) &&
                   MathHelper.AreEqual(AngleEnd, other.AngleEnd) &&
                   MathHelper.AreEqual(AngleExtent, other.AngleExtent) &&
                   MathHelper.AreEqual(MinScore, other.MinScore) &&
                   NumMatches == other.NumMatches &&
                   MathHelper.AreEqual(MaxOverlap, other.MaxOverlap) &&
                   SubPixel == other.SubPixel &&
                   NumLevels == other.NumLevels &&
                   MathHelper.AreEqual(Greediness, other.Greediness) &&
                   MathHelper.AreEqual(ScaleMin, other.ScaleMin) &&
                   MathHelper.AreEqual(ScaleMax, other.ScaleMax);
        }

        public override int GetHashCode()
        {
            // 必须覆盖 Equals 中比较的所有字段；浮点字段量化到容差网格保证与 MathHelper.AreEqual 一致
            var hash = new HashCode();
            hash.Add(ModelID?.D);
            hash.Add(LockCenter);
            hash.Add(MathHelper.QuantizeToTolerance(AngleStart));
            hash.Add(MathHelper.QuantizeToTolerance(AngleEnd));
            hash.Add(MathHelper.QuantizeToTolerance(AngleExtent));
            hash.Add(MathHelper.QuantizeToTolerance(MinScore));
            hash.Add(NumMatches);
            hash.Add(MathHelper.QuantizeToTolerance(MaxOverlap));
            hash.Add(SubPixel);
            hash.Add(NumLevels);
            hash.Add(MathHelper.QuantizeToTolerance(Greediness));
            hash.Add(MathHelper.QuantizeToTolerance(ScaleMin));
            hash.Add(MathHelper.QuantizeToTolerance(ScaleMax));
            return hash.ToHashCode();
        }

        #endregion

        #region Formatting

        public override string ToString()
        {
            return $"CvModel[Score≥{MinScore:P0}, Matches={NumMatches}, " +
                   $"Angle=[{AngleStartDegrees:F0}°,{AngleEndDegrees:F0}°], " +
                   $"Greed={Greediness:P0}]";
        }

        #endregion

        #region Static Members

        /// <summary>
        /// 默认配置
        /// </summary>
        public static readonly CvModel Default = new();

        #endregion
    }

    #region Enums

    /// <summary>
    /// 亚像素精度模式
    /// </summary>
    public enum SubPixelMode
    {
        /// <summary>
        /// 无亚像素
        /// </summary>
        None,

        /// <summary>
        /// 插值
        /// </summary>
        Interpolation,

        /// <summary>
        /// 最小二乘法
        /// </summary>
        LeastSquares,

        /// <summary>
        /// 高精度最小二乘法
        /// </summary>
        LeastSquaresHigh,

        /// <summary>
        /// 超高精度最小二乘法
        /// </summary>
        LeastSquaresVeryHigh
    }

    /// <summary>
    /// 匹配度量类型
    /// </summary>
    public enum MetricType
    {
        /// <summary>
        /// 使用极性
        /// </summary>
        UsePolarity,

        /// <summary>
        /// 忽略全局极性
        /// </summary>
        IgnoreGlobalPolarity,

        /// <summary>
        /// 忽略局部极性
        /// </summary>
        IgnoreLocalPolarity,

        /// <summary>
        /// 忽略颜色极性
        /// </summary>
        IgnoreColorPolarity
    }

    #endregion
}
