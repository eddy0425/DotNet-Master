using System;

namespace DotNet.Drawing
{
    public static class AngleExtension
    {
        private const double FullCircleDegrees = 360.0;  // 360 度完整角
        private const double HalfCircleDegrees = 180.0; // 180 度半圆
        private const double QuarterCircleDegrees = 90.0; // 90 度四分之一圆

        /// <summary>
        /// 将弧度转换为角度
        /// </summary>
        /// <param name="radian">弧度值</param>
        /// <returns>角度值</returns>
        public static double ToDegrees(this double radian)
        {
            return radian * (180.0 / Math.PI);
        }

        /// <summary>
        /// 将角度转换为弧度
        /// </summary>
        /// <param name="angle">角度值</param>
        /// <returns>弧度值</returns>
        public static double ToRadians(this double angle)
        {
            return angle * (Math.PI / 180.0);
        }

        /// <summary>
        /// 限制角度范围
        /// </summary>
        /// <param name="angle">要限制的角度；NaN / 无穷大原样返回</param>
        /// <param name="range">角度范围</param>
        /// <returns>限制后的角度</returns>
        public static double LimitAngle(this double angle, AngleRange range)
        {
            // 非有限值原样返回，交由调用方按业务判定（如 Halcon 拟合失败会返回 NaN），
            // 不在底层工具里升级成异常，避免上层 catch(Exception) 吞掉真实失败原因
            if (double.IsNaN(angle) || double.IsInfinity(angle)) return angle;

            switch (range)
            {
                case AngleRange.Minus360To360: // (-360, 360)，仅取模
                    angle %= FullCircleDegrees;
                    return angle == 0.0 ? 0.0 : angle; // 消除 -0.0

                case AngleRange.Minus180To180: // [-180, 180]
                    return NormalizeToSigned180(angle);

                case AngleRange.Range0To180: // [0, 180)
                    return FoldAngleToNonNegative180(angle);

                case AngleRange.Minus180To0: // (-180, 0]
                    return FoldAngleToNonPositive180(angle);

                case AngleRange.Minus90To90: // [-90, 90]
                    return FoldAngleToSigned90(angle);

                default:
                    throw new ArgumentOutOfRangeException(nameof(range), range, "不支持的角度范围类型。");
            }
        }

        /// <summary>
        /// 归一化角度到范围 -180 到 180 的辅助方法。
        /// </summary>
        /// <param name="angle">角度</param>
        /// <returns>归一化角度</returns>
        private static double NormalizeToSigned180(double angle)
        {
            // 限制角度到范围 -180 到 180
            angle %= FullCircleDegrees;
            if (angle > HalfCircleDegrees) angle -= FullCircleDegrees;
            else if (angle < -HalfCircleDegrees) angle += FullCircleDegrees;
            return angle;
        }

        /// <summary>
        /// 按 180 度周期将角度折叠到 [0, 180) 的范围。
        /// </summary>
        /// <param name="angle">角度</param>
        /// <returns>折叠后的角度</returns>
        private static double FoldAngleToNonNegative180(double angle)
        {
            angle = NormalizeToSigned180(angle); // 首先确保在 -180 到 180 内
            if (angle < 0) angle += HalfCircleDegrees;
            // ±180 在 mod 180 下同向，统一折到 0，避免因输入符号不同得到 0 / 180 两个端点
            if (angle >= HalfCircleDegrees) angle -= HalfCircleDegrees;
            return angle == 0.0 ? 0.0 : angle; // 消除 -0.0
        }

        /// <summary>
        /// 按 180 度周期将角度折叠到 (-180, 0] 的范围。
        /// </summary>
        /// <param name="angle">角度</param>
        /// <returns>折叠后的角度</returns>
        private static double FoldAngleToNonPositive180(double angle)
        {
            angle = NormalizeToSigned180(angle); // 首先确保在 -180 到 180 内
            if (angle > 0) angle -= HalfCircleDegrees;
            // ±180 在 mod 180 下同向，统一折到 0，避免因输入符号不同得到 0 / -180 两个端点
            if (angle <= -HalfCircleDegrees) angle += HalfCircleDegrees;
            return angle == 0.0 ? 0.0 : angle; // 消除 -0.0
        }

        /// <summary>
        /// 按 180 度周期将角度折叠到 -90 到 90 的范围。
        /// </summary>
        /// <param name="angle">角度</param>
        /// <returns>折叠后的角度</returns>
        private static double FoldAngleToSigned90(double angle)
        {
            angle = NormalizeToSigned180(angle); // 首先确保在 -180 到 180 内
            if (angle > QuarterCircleDegrees) return angle - HalfCircleDegrees;
            if (angle < -QuarterCircleDegrees) return angle + HalfCircleDegrees;
            return angle;
        }
    }
}
