using HalconDotNet;
using Newtonsoft.Json;
using System;

namespace DotNet.Drawing
{
    [Serializable]
    public class CvRegion : Rect2d, ICloneable, IDisposable
    {
        public static readonly CvRegion Empty = new CvRegion();

        public CvRegion()
          : base()
        {
            InRegion = new HObject(); HOperatorSet.GenEmptyObj(out InRegion);
        }

        /// <summary>
        /// 角度
        /// </summary>
        public HTuple Phi { set; get; } = 0;

        /// <summary>
        /// 多边型点X数组
        /// </summary>
        public HTuple PolygonX { set; get; }

        /// <summary>
        /// 多边型点Y数组
        /// </summary>
        public HTuple PolygonY { set; get; }

        ///// <summary> 多边形点集合 </summary>
        //public List<Point2d> Polygons { get; set; } = new List<Point2d>();

        /// <summary>
        /// 新增或者减少
        /// </summary>
        public bool AddOrDecrease { set; get; } = true;

        /// <summary>
        /// 最大半径
        /// </summary>
        public double MaxRadius { set; get; } = 300;

        /// <summary>
        /// 最小半径
        /// </summary>
        public double MinRadius { set; get; } = 100;

        /// <summary>
        /// 圆环宽度
        /// </summary>
        public double RingWidth { set; get; } = 100;

        /// <summary>
        /// 类型
        /// </summary>
        public RectEnum Type { set; get; } = RectEnum.Rectangle;

        [JsonIgnore]
        public HObject InRegion;              //区域

        /// <summary>
        /// 区域
        /// </summary>
        [JsonConverter(typeof(JsonConvertHObject))]
        public HObject HoRegion { set { InRegion = value; } get { return InRegion; } }

        [JsonIgnore]
        public Point2d Center { set { this.UpdateCenter(value); } get { return new Point2d((Left + Right) / 2, (Top + Bottom) / 2); } } //区域中心

        #region  克隆
        object ICloneable.Clone() => (object)this.Clone();
        public CvRegion Clone()
        {
            return TransExpV2<CvRegion, CvRegion>.Trans(this);
        }
        #endregion

        #region Dispose

        bool _disposed = false;

        // 实现 Dispose 模式
        protected void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // 清理托管资源

                }

                // 清理非托管资源
                if (InRegion == null)
                {
                    InRegion?.Dispose();
                    InRegion = null;
                }

                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~CvRegion()
        {
            Dispose(false);
        }

        #endregion

        public override int GetHashCode() => HashCode.Combine(Phi, PolygonX, PolygonY, MaxRadius, MinRadius, RingWidth, Type, InRegion);

        //public override int GetHashCode()
        //{
        //    // 使用质数和逐个字段进行哈希计算
        //    int hash = 17;
        //    hash = hash * 31 + Centre.GetHashCode();          // 中心点
        //    hash = hash * 31 + Phi.GetHashCode();             // 角度
        //    hash = hash * 31 + MaxRadius.GetHashCode();       // 最大半径
        //    hash = hash * 31 + MinRadius.GetHashCode();       // 最小半径
        //    hash = hash * 31 + RingWidth.GetHashCode();       // 圆环宽度
        //    hash = hash * 31 + AddOrDecrease.GetHashCode();   // 新增或减少
        //    hash = hash * 31 + Type.GetHashCode();            // 类型

        //    if (PolygonX != null)
        //        hash = hash * 31 + PolygonX.GetHashCode();    // 多边形点X数组

        //    if (PolygonY != null)
        //        hash = hash * 31 + PolygonY.GetHashCode();    // 多边形点Y数组

        //    return hash;
        //}


    }
}
