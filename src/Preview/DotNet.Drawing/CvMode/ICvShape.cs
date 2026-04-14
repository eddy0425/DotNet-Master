namespace DotNet.Drawing
{
    /// <summary>
    /// CV 形状基础接口 - 定义所有几何形状的通用行为
    /// </summary>
    /// <remarks>
    /// 设计原则：
    /// - 不可变性：所有实现必须是不可变的，确保线程安全
    /// - 函数式更新：通过 With*/Transform 方法返回新实例
    /// </remarks>
    public interface ICvShape
    {
        /// <summary>
        /// 形状的中心点
        /// </summary>
        Point2d Center { get; }

        /// <summary>
        /// 形状的边界框
        /// </summary>
        Rect2d BoundingBox { get; }
    }

    /// <summary>
    /// 可平移接口
    /// </summary>
    /// <typeparam name="T">实现类型</typeparam>
    public interface ICvTranslatable<T> where T : ICvTranslatable<T>
    {
        /// <summary>
        /// 平移（返回新实例）
        /// </summary>
        /// <param name="dx">X方向位移</param>
        /// <param name="dy">Y方向位移</param>
        /// <returns>平移后的新实例</returns>
        T Translate(double dx, double dy);

        /// <summary>
        /// 平移（返回新实例）
        /// </summary>
        /// <param name="offset">偏移量</param>
        /// <returns>平移后的新实例</returns>
        T Translate(Point2d offset);
    }

    /// <summary>
    /// 可缩放接口
    /// </summary>
    /// <typeparam name="T">实现类型</typeparam>
    public interface ICvScalable<T> where T : ICvScalable<T>
    {
        /// <summary>
        /// 统一缩放（返回新实例）
        /// </summary>
        /// <param name="scale">缩放因子</param>
        /// <returns>缩放后的新实例</returns>
        T Scale(double scale);
    }

    /// <summary>
    /// 可旋转接口
    /// </summary>
    /// <typeparam name="T">实现类型</typeparam>
    public interface ICvRotatable<T> where T : ICvRotatable<T>
    {
        /// <summary>
        /// 绕中心旋转（返回新实例）
        /// </summary>
        /// <param name="angle">旋转角度（弧度）</param>
        /// <returns>旋转后的新实例</returns>
        T Rotate(double angle);

        /// <summary>
        /// 绕指定点旋转（返回新实例）
        /// </summary>
        /// <param name="angle">旋转角度（弧度）</param>
        /// <param name="pivot">旋转中心点</param>
        /// <returns>旋转后的新实例</returns>
        T RotateAround(double angle, Point2d pivot);
    }

    /// <summary>
    /// 完整变换接口 - 组合平移、缩放和旋转
    /// </summary>
    /// <typeparam name="T">实现类型</typeparam>
    public interface ICvTransformable<T> : ICvTranslatable<T>, ICvScalable<T>, ICvRotatable<T>
        where T : ICvTransformable<T>
    {
    }

    /// <summary>
    /// 包含判断接口
    /// </summary>
    public interface ICvContainable
    {
        /// <summary>
        /// 判断点是否在形状内
        /// </summary>
        /// <param name="point">待检查的点</param>
        /// <returns>是否包含</returns>
        bool Contains(Point2d point);

        /// <summary>
        /// 判断点是否在形状边界上（带容差）
        /// </summary>
        /// <param name="point">待检查的点</param>
        /// <param name="tolerance">容差值</param>
        /// <returns>是否在边界上</returns>
        bool IsOnBoundary(Point2d point, double tolerance = 0.01);
    }
}

