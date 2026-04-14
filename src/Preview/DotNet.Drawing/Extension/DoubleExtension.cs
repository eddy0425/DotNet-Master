using System;

namespace DotNet.Drawing
{
    public static class DoubleExtension
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
        /// <param name="angle">要限制的角度</param>
        /// <param name="rangeType">范围类型，可选值为："±360"、"±180"、"0-180"、"0至-180"、"±90"</param>
        /// <returns>限制后的角度</returns>
        public static double LimitAngle(this double angle, string rangeType)
        {
            // 确保角度值在一个周期范围内（-360 到 360）
            angle %= FullCircleDegrees;

            switch (rangeType)
            {
                case "±360": // -360 到 360
                    return angle;

                case "±180": // -180 到 180
                    if (angle > HalfCircleDegrees) return angle - FullCircleDegrees;
                    if (angle < -HalfCircleDegrees) return angle + FullCircleDegrees;
                    return angle;

                case "0-180": // 0 到 180
                    angle = NormalizeToPositive180(angle);
                    return angle >= 0 ? angle : angle + HalfCircleDegrees;

                case "0至-180": // 0 到 -180
                    angle = NormalizeToPositive180(angle);
                    return angle <= 0 ? angle : angle - HalfCircleDegrees;

                case "±90": // -90 到 90
                    angle = NormalizeToAbsolute90(angle);
                    return angle;

                default:
                    throw new ArgumentException($"不支持的角度范围类型：{rangeType}. 有效值为：±360, ±180, 0-180, 0至-180, ±90");
            }
        }

        /// <summary>
        /// 归一化角度到范围 -180 到 180 的辅助方法。
        /// </summary>
        /// <param name="angle">角度</param>
        /// <returns>归一化角度</returns>
        private static double NormalizeToPositive180(double angle)
        {
            // 限制角度到范围 -180 到 180
            if (angle > HalfCircleDegrees) angle -= FullCircleDegrees;
            if (angle < -HalfCircleDegrees) angle += FullCircleDegrees;
            return angle;
        }

        /// <summary>
        /// 归一化角度到范围 -90 到 90 的辅助方法。
        /// </summary>
        /// <param name="angle">角度</param>
        /// <returns>归一化角度</returns>
        private static double NormalizeToAbsolute90(double angle)
        {
            angle = NormalizeToPositive180(angle); // 首先确保在 -180 到 180 内
            if (angle > QuarterCircleDegrees) return angle - HalfCircleDegrees;
            if (angle < -QuarterCircleDegrees) return angle + HalfCircleDegrees;
            return angle;
        }
    }
}
