//using System;

//namespace DotNet.HWindows
//{
//    public static class DoubleExtension
//    {
//        /// <summary>
//        /// 将弧度转为角度
//        /// </summary>
//        public static double ToAngle(this double radian)
//        {
//            return radian * (180 / Math.PI);
//        }

//        /// <summary>
//        /// 将角度转为弧度
//        /// </summary>
//        /// <param name="d"></param>
//        /// <returns></returns>
//        public static double ToRadians(this double angle)
//        {
//            return angle / (180 / Math.PI);
//        }

//        /// <summary>
//        /// 限制角度范围
//        /// </summary>
//        public static double AngleLimit(this double angle,string angleRange)
//        {
//            double angleTemp = Convert.ToDouble(angle);
//            switch (angleRange)
//            {
//                case "正负360":
//                    {
//                        angleTemp = angleTemp % 360;
//                    }
//                    break;
//                case "正负180":
//                    {
//                        angleTemp = angleTemp % 360;
//                        if (angleTemp > 180) angleTemp = angleTemp - 360;
//                        else if (angleTemp < -180) angleTemp = angleTemp + 360;
//                    }
//                    break;
//                case "正180":
//                    {
//                        angleTemp = angleTemp % 360;
//                        if (angleTemp > 180) angleTemp = angleTemp - 360;
//                        else if (angleTemp < -180) angleTemp = angleTemp + 360;

//                        if (angleTemp < 0) angleTemp = angleTemp + 180;
//                    }
//                    break;
//                case "负180":
//                    {
//                        angleTemp = angleTemp % 360;
//                        if (angleTemp > 180) angleTemp = angleTemp - 360;
//                        else if (angleTemp < -180) angleTemp = angleTemp + 360;

//                        if (angleTemp > 0) angleTemp = angleTemp - 180;
//                    }
//                    break;
//                case "正负90":
//                    {
//                        angleTemp = angleTemp % 360;
//                        if (angleTemp > 180) angleTemp = angleTemp - 360;
//                        else if (angleTemp < -180) angleTemp = angleTemp + 360;

//                        if (angleTemp > 90) angleTemp = angleTemp - 180;
//                        else if (angleTemp < -90) angleTemp = angleTemp + 180;
//                    }
//                    break;
//            }
//            return angleTemp;
//        }
//    }
//}
