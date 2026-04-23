using HalconDotNet;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace DotNet.Drawing
{
    /// <summary>
    /// 区域定义（矩形 / 仿射矩形 / 圆 / 椭圆 / 多边形 / 圆环），并持有对应的 Halcon <see cref="HObject"/>。
    /// </summary>
    /// <remarks>
    /// 设计说明：
    /// - <b>可变 class</b>：调用方依赖按引用语义直接修改 (<c>region.Phi = ...</c>、<c>region.Update2Point(...)</c>)，
    ///   且 <see cref="InRegion"/> 被作为 <c>out</c> 参数传给 HOperatorSet（C# 不支持 out 属性），
    ///   因此必须保留 public field + 可变属性形态。
    /// - <b>sealed</b>：派生类若再次扩展状态会破坏本类的 <see cref="Equals(CvRegion)"/> / <see cref="GetHashCode"/> 契约，禁止继承。
    /// - <b>Equals/GetHashCode 契约</b>：本类既然扩展了字段（Phi/Type/Polygon/...），就必须同时重写 <see cref="Equals(object)"/>，
    ///   否则会出现"GetHashCode 不同但 Equals 判等"的契约违反。
    /// - <b>资源管理</b>：<see cref="InRegion"/> 是 HalconDotNet 的托管包装，本身已具有 finalizer，
    ///   所以本类无需自己写 finalizer——只在 <see cref="Dispose()"/> 中主动释放即可。
    /// </remarks>
    [Serializable]
    public sealed class CvRegion : Rect2d, IEquatable<CvRegion>, ICloneable, IDisposable
    {
        public static readonly CvRegion Empty = new CvRegion();

        public CvRegion()
            : base()
        {
            // 注意：这里不能改成 InRegion = new HObject() —— 后续以 out 形式覆盖时旧实例会被丢弃但未释放。
            // GenEmptyObj 内部会创建并初始化句柄，等价的最简形式即一行调用。
            HOperatorSet.GenEmptyObj(out InRegion);
        }

        #region Geometry / Shape Parameters

        /// <summary>
        /// 角度（仅对 Rectangle2 / Ellipse 有意义）
        /// </summary>
        public HTuple Phi { set; get; } = 0;

        /// <summary>
        /// 多边形点 X 数组（仅对 Polygon 有意义）
        /// </summary>
        public HTuple? PolygonX { set; get; }

        /// <summary>
        /// 多边形点 Y 数组（仅对 Polygon 有意义）
        /// </summary>
        public HTuple? PolygonY { set; get; }

        /// <summary>
        /// 是新增区域 (true) 还是减去区域 (false)
        /// </summary>
        public bool AddOrDecrease { set; get; } = true;

        /// <summary>
        /// 最大半径（仅对 Ring 有意义）
        /// </summary>
        public double MaxRadius { set; get; } = 300;

        /// <summary>
        /// 最小半径（仅对 Ring 有意义）
        /// </summary>
        public double MinRadius { set; get; } = 100;

        /// <summary>
        /// 圆环宽度
        /// </summary>
        public double RingWidth { set; get; } = 100;

        /// <summary>
        /// 区域类型
        /// </summary>
        public RectEnum Type { set; get; } = RectEnum.Rectangle;

        #endregion

        #region Halcon HObject

        /// <summary>
        /// 区域 (Halcon HObject)
        /// </summary>
        /// <remarks>
        /// 公有字段（非属性）是有意而为：上层算法需要以 <c>out region.InRegion</c> 形式调用 HOperatorSet 系列方法。
        /// </remarks>
        [JsonIgnore]
        public HObject? InRegion;

        /// <summary>
        /// JSON 序列化用属性，与 <see cref="InRegion"/> 共享底层句柄
        /// </summary>
        [JsonConverter(typeof(JsonConvertHObject))]
        public HObject? HoRegion
        {
            get => InRegion;
            set => InRegion = value;
        }

        #endregion

        /// <summary>
        /// 区域中心点
        /// </summary>
        [JsonIgnore]
        public Point2d Center
        {
            get => new Point2d((Left + Right) / 2, (Top + Bottom) / 2);
            set => this.UpdateCenter(value);
        }

        #region Cloning

        object ICloneable.Clone() => Clone();

        public CvRegion Clone() => TransExpV2<CvRegion, CvRegion>.Trans(this);

        #endregion

        #region Equality

        // 设计要点：
        //   1. base.GetHashCode/Equals 已修改为虚分派 + 容差比较；
        //   2. 本类扩展了字段，必须同时 override Equals(object) 与 Equals(Rect2d)，
        //      否则放进 HashSet<Rect2d>/Dictionary 时会出现哈希与判等不一致 → 数据丢失。
        //   3. HObject (InRegion) 不参与判等：句柄非业务身份；几何参数相同即视为相同 ROI 定义。

        public override bool Equals(Rect2d? obj) => obj is CvRegion other && Equals(other);

        public bool Equals(CvRegion? other)
        {
            if (ReferenceEquals(other, null)) return false;
            if (ReferenceEquals(this, other)) return true;

            // 几何字段（基类）
            if (!base.Equals((Rect2d)other)) return false;

            // CvRegion 扩展字段
            return Type == other.Type
                && AddOrDecrease == other.AddOrDecrease
                && MathHelper.AreEqual(MaxRadius, other.MaxRadius)
                && MathHelper.AreEqual(MinRadius, other.MinRadius)
                && MathHelper.AreEqual(RingWidth, other.RingWidth)
                && HTupleEquals(Phi, other.Phi)
                && HTupleEquals(PolygonX, other.PolygonX)
                && HTupleEquals(PolygonY, other.PolygonY);
        }

        public override int GetHashCode()
        {
            // 注意：哈希码只基于稳定的几何 / 标量字段；
            // InRegion (HObject) 是托管句柄，运行期会变化，纳入哈希会破坏 GetHashCode 的稳定性契约。
            var hash = new HashCode();
            hash.Add(base.GetHashCode());
            hash.Add(Type);
            hash.Add(AddOrDecrease);
            hash.Add(MathHelper.QuantizeToTolerance(MaxRadius));
            hash.Add(MathHelper.QuantizeToTolerance(MinRadius));
            hash.Add(MathHelper.QuantizeToTolerance(RingWidth));
            hash.Add(HTupleHash(Phi));
            hash.Add(HTupleHash(PolygonX));
            hash.Add(HTupleHash(PolygonY));
            return hash.ToHashCode();
        }

        /// <summary>
        /// HTuple 容差比较：同时处理 null、长度、逐元素 double 比较
        /// </summary>
        private static bool HTupleEquals(HTuple? a, HTuple? b)
        {
            if (ReferenceEquals(a, b)) return true;
            if (a is null || b is null) return false;
            if (a.Length != b.Length) return false;
            for (int i = 0; i < a.Length; i++)
            {
                if (!MathHelper.AreEqual(a[i].D, b[i].D)) return false;
            }
            return true;
        }

        /// <summary>
        /// 与 <see cref="HTupleEquals"/> 配套的稳定哈希：用元素数 + 量化值
        /// </summary>
        private static int HTupleHash(HTuple? tuple)
        {
            if (tuple is null) return 0;
            var hash = new HashCode();
            hash.Add(tuple.Length);
            for (int i = 0; i < tuple.Length; i++)
            {
                hash.Add(MathHelper.QuantizeToTolerance(tuple[i].D));
            }
            return hash.ToHashCode();
        }

        #endregion

        #region IDisposable

        private bool _disposed;

        public void Dispose()
        {
            if (_disposed) return;

            // 释放 HObject（HalconDotNet 自身具备 finalizer，但显式 Dispose 能确保 native 句柄即时回收，
            // 避免在 GC 压力大的场景下 native 资源滞留）
            if (InRegion != null)
            {
                InRegion.Dispose();
                InRegion = null;
            }

            _disposed = true;
            GC.SuppressFinalize(this);
        }

        // 不再提供 finalizer：本类没有持有任何裸 native 句柄，
        // 唯一持有的 HObject 自身已具有 finalizer，重复实现只会增加 GC 压力。

        #endregion
    }
}
