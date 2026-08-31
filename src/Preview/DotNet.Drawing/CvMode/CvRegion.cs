using HalconDotNet;
using Newtonsoft.Json;
using System;


namespace DotNet.Drawing
{
    /// <summary>
    /// 区域定义（矩形 / 仿射矩形 / 圆 / 椭圆 / 多边形 / 圆环），并持有对应的 Halcon <see cref="HObject"/>。
    /// </summary>
    /// <remarks>
    /// 设计说明：
    /// - <b>可变 class</b>：调用方依赖按引用语义直接修改 (<c>region.Phi = ...</c>、<c>region.SetRect(...)</c>)，
    ///   且 <see cref="HoRegion"/> 被作为 <c>out</c> 参数传给 HOperatorSet（C# 不支持 out 属性），
    ///   因此必须保留 public field + 可变属性形态。
    /// - <b>sealed</b>：派生类若再次扩展状态会破坏本类的 <see cref="Equals(CvRegion)"/> / <see cref="GetHashCode"/> 契约，禁止继承。
    /// - <b>Equals/GetHashCode 契约</b>：本类既然扩展了字段（Phi/Type/Polygon/...），就必须同时重写 <see cref="Equals(object)"/>，
    ///   否则会出现"GetHashCode 不同但 Equals 判等"的契约违反。
    /// - <b>资源管理</b>：<see cref="HoRegion"/> 是 HalconDotNet 的托管包装，本身已具有 finalizer，
    ///   所以本类无需自己写 finalizer——只在 <see cref="Dispose()"/> 中主动释放即可。
    /// - <b>组合而非继承</b>：本类曾经 <c>: Rect2d</c>。但 ROI 可以是圆 / 椭圆 / 多边形 / 圆环，
    ///   还持有 Halcon 句柄并实现 <see cref="IDisposable"/>，与"矩形"并不构成 is-a 关系——
    ///   那次继承只是为了白拿 X/Y/Width/Height 四个分量。继承还带来两个实际问题：
    ///   覆写 <c>Equals(Rect2d)</c> 破坏了判等的对称性（<c>rect.Equals(region)</c> 与
    ///   <c>region.Equals(rect)</c> 结果不同）；而基类不可变化之后，可变的 ROI 也无法再继承它。
    ///   现在改为持有一个 <see cref="Bounds"/>，外部访问 <c>X/Y/Width/Height/Left/Top/...</c> 的写法不变。
    /// </remarks>
    [Serializable]
    public sealed class CvRegion : IEquatable<CvRegion>, ICloneable, IDisposable
    {
        // 注意：这里刻意不提供 `public static readonly CvRegion Empty`。
        // 本类可变且实现 IDisposable，任何一处对共享实例调用 Dispose()/改字段，
        // 都会让全进程的"空区域"变成已释放或被污染的状态。需要空区域请 new 一个。

        public CvRegion()
        {
            // 注意：这里不能改成 InRegion = new HObject() —— 后续以 out 形式覆盖时旧实例会被丢弃但未释放。
            // GenEmptyObj 内部会创建并初始化句柄，等价的最简形式即一行调用。
            HOperatorSet.GenEmptyObj(out HoRegion);
        }

        #region Bounds (组合持有的外接矩形)

        private Rect2d _bounds = new Rect2d();

        /// <summary>
        /// ROI 的外接矩形。<see cref="Rect2d"/> 不可变，因此可以安全地整体读写，不必担心被别处改掉。
        /// </summary>
        /// <remarks>
        /// 需要一次改动多个分量时请整体赋值（<c>region.Bounds = rect;</c>），
        /// 而不是逐个写 X/Y/Width/Height——后者每写一次都会重建一个中间矩形，
        /// 且中间状态可能是不合法的（例如新的 Width 还没写进去就先写了新的 X）。
        /// </remarks>
        /// <exception cref="ArgumentNullException">赋值为 null</exception>
        [JsonIgnore]
        public Rect2d Bounds
        {
            get => _bounds;
            set => _bounds = value ?? throw new ArgumentNullException(nameof(value));
        }

        /// <summary> 左上角X </summary>
        public double X
        {
            get => _bounds.X;
            set => _bounds = new Rect2d(value, _bounds.Y, _bounds.Width, _bounds.Height);
        }

        /// <summary> 左上角Y </summary>
        public double Y
        {
            get => _bounds.Y;
            set => _bounds = new Rect2d(_bounds.X, value, _bounds.Width, _bounds.Height);
        }

        /// <summary> 区域宽（非负，赋负值抛异常） </summary>
        /// <exception cref="ArgumentOutOfRangeException">赋值为负</exception>
        public double Width
        {
            get => _bounds.Width;
            set => _bounds = new Rect2d(_bounds.X, _bounds.Y, value, _bounds.Height);
        }

        /// <summary> 区域高（非负，赋负值抛异常） </summary>
        /// <exception cref="ArgumentOutOfRangeException">赋值为负</exception>
        public double Height
        {
            get => _bounds.Height;
            set => _bounds = new Rect2d(_bounds.X, _bounds.Y, _bounds.Width, value);
        }

        // 以下均为 Bounds 的派生量, 只读转发, 不参与 JSON 序列化

        /// <summary> 上边界Y </summary>
        [JsonIgnore] public double Top => _bounds.Top;

        /// <summary> 下边界Y (Y + Height) </summary>
        [JsonIgnore] public double Bottom => _bounds.Bottom;

        /// <summary> 左边界X </summary>
        [JsonIgnore] public double Left => _bounds.Left;

        /// <summary> 右边界X (X + Width) </summary>
        [JsonIgnore] public double Right => _bounds.Right;

        /// <summary> 中心X </summary>
        [JsonIgnore] public double CenterX => _bounds.CenterX;

        /// <summary> 中心Y </summary>
        [JsonIgnore] public double CenterY => _bounds.CenterY;

        /// <summary> 左上角点 </summary>
        [JsonIgnore] public Point2d TopLeft => _bounds.TopLeft;

        /// <summary> 右下角点 </summary>
        [JsonIgnore] public Point2d BottomRight => _bounds.BottomRight;

        /// <summary> 左上角位置 </summary>
        [JsonIgnore] public Point2d Location => _bounds.Location;

        /// <summary> 外接矩形大小 </summary>
        [JsonIgnore] public Size2d Size => _bounds.Size;

        /// <summary> 判断坐标是否落在外接矩形内（右开 / 下开区间） </summary>
        public bool Contains(double x, double y) => _bounds.Contains(x, y);

        /// <summary> 判断点是否落在外接矩形内（右开 / 下开区间） </summary>
        public bool Contains(Point2d pt) => _bounds.Contains(pt);

        /// <summary> 外接矩形转换为整数矩形 </summary>
        public Rect ToRect() => _bounds.ToRect();

        #endregion

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
        /// 区域的 Halcon 句柄。
        /// </summary>
        /// <remarks>
        /// 必须是<b>字段</b>而非属性：它要作为 <c>out</c> 参数传给 <c>HOperatorSet.*</c>，
        /// 而 C# 不允许属性用作 <c>out</c> 实参。这也是 <see cref="Clone"/> 不能依赖
        /// <see cref="TransExpV2{TIn,TOut}"/>（只枚举属性）的原因。
        /// </remarks>
        [JsonConverter(typeof(JsonConvertHObject))]
        public HObject HoRegion;

        #endregion

        /// <summary>
        /// 区域中心点
        /// </summary>
        [JsonIgnore]
        public Point2d Center
        {
            get => new Point2d(_bounds.CenterX, _bounds.CenterY);
            set => this.SetCenter(value);
        }

        #region Cloning

        object ICloneable.Clone() => Clone();

        /// <summary>
        /// 深拷贝：几何参数逐项复制，<see cref="HoRegion"/> 通过 <c>CopyObj</c> 生成独立句柄。
        /// </summary>
        /// <remarks>
        /// <b>不能</b>用 <see cref="TransExpV2{TIn,TOut}"/> 实现：它只枚举可写<b>属性</b>，
        /// 而 <see cref="HoRegion"/> 是<b>字段</b>（必须是字段才能作为 <c>out</c> 参数传给
        /// HOperatorSet），会被静默跳过，克隆结果的句柄为 null，后续显示 / 运算直接 NRE。
        /// <para>
        /// <b>所有权</b>：返回的实例独立持有一份 Halcon 句柄，由调用方负责 <see cref="Dispose"/>。
        /// </para>
        /// </remarks>
        public CvRegion Clone()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(CvRegion));

            var clone = new CvRegion
            {
                // 外接矩形：Rect2d 不可变，直接共享同一个实例即可，无需逐分量复制
                Bounds = _bounds,

                // 本类扩展参数（HTuple 是可变容器，必须拷贝而非共享引用）
                Phi = Phi?.Clone() ?? new HTuple(0),
                PolygonX = PolygonX?.Clone(),
                PolygonY = PolygonY?.Clone(),
                AddOrDecrease = AddOrDecrease,
                MaxRadius = MaxRadius,
                MinRadius = MinRadius,
                RingWidth = RingWidth,
                Type = Type,
            };

            if (HoRegion.NotNull())
            {
                // 构造函数已经用 GenEmptyObj 建了一个空句柄，覆盖前必须先释放，否则泄漏。
                clone.HoRegion.Dispose();
                clone.HoRegion = HoRegion.CopyObj(1, -1);
            }

            return clone;
        }

        #endregion

        #region Equality

        // 设计要点：
        //   1. 曾经继承 Rect2d 并覆写 `Equals(Rect2d)`，那会破坏对称性——
        //      rect.Equals(region) 走 Rect2d 的实现只比四个分量, 判 true;
        //      region.Equals(rect) 走覆写版本要求对方也是 CvRegion, 判 false。
        //      两者放进同一个 HashSet/Dictionary 行为未定义。改为组合后 CvRegion 与 Rect2d
        //      不再有继承关系, 互相判等一律为 false, 对称性自然成立。
        //   2. 判等只覆盖本类型：Equals(object) 显式收窄到 CvRegion。
        //   3. HObject (HoRegion) 不参与判等：句柄非业务身份；几何参数相同即视为相同 ROI 定义。

        public override bool Equals(object? obj) => Equals(obj as CvRegion);

        public static bool operator ==(CvRegion? lhs, CvRegion? rhs)
        {
            if (ReferenceEquals(lhs, null)) return ReferenceEquals(rhs, null);
            return lhs.Equals(rhs);
        }

        public static bool operator !=(CvRegion? lhs, CvRegion? rhs) => !(lhs == rhs);

        public bool Equals(CvRegion? other)
        {
            if (ReferenceEquals(other, null)) return false;
            if (ReferenceEquals(this, other)) return true;

            // 外接矩形（容差比较）
            if (!_bounds.Equals(other._bounds)) return false;

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
            // HoRegion (HObject) 是托管句柄，运行期会变化，纳入哈希会破坏 GetHashCode 的稳定性契约。
            var hash = new HashCode();
            hash.Add(_bounds.GetHashCode());
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
            if (HoRegion != null)
            {
                HoRegion.Dispose();
                HoRegion = null;
            }

            _disposed = true;
            GC.SuppressFinalize(this);
        }

        // 不再提供 finalizer：本类没有持有任何裸 native 句柄，
        // 唯一持有的 HObject 自身已具有 finalizer，重复实现只会增加 GC 压力。

        #endregion
    }
}
