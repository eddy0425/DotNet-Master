using DotNet.Excel;
using DotNet.Json;
using DotNet.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotNet.VisionMaster
{
    public class LogFile
    {
        public LogFile()
        {
            //LogHelper.Initialize("VisionMaster");

            JsonLog.Logged += JsonLog_Logged;
            //ExcelLog.Logged += ExcelLog_Logged;

        }

        //private void ExcelLog_Logged(ExcelLogArgs args)
        //{
        //    switch (args.Level)
        //    {
        //        case ExcelLogLevel.Debug:
        //            LogHelper.Debug(args.Tag, args.Message);
        //            break;
        //        case ExcelLogLevel.Information:
        //            LogHelper.Info(args.Tag, args.Message);
        //            break;
        //        case ExcelLogLevel.Warning:
        //            LogHelper.Warning(args.Tag, args.Message);
        //            break;
        //        case ExcelLogLevel.Error:
        //            LogHelper.Error(args.Tag, args.Message);
        //            break;
        //        case ExcelLogLevel.Exception:
        //            LogHelper.Exception(args.Tag, args.Exception, args.Message);
        //            break;
        //    }
        //}

        private void JsonLog_Logged(JsonLogArgs args)
        {
            switch (args.Level)
            {
               case JsonLogLevel.Debug:
                    JsonLog.Debug(args.Tag, args.Message);
                    break;
                case JsonLogLevel.Information:
                    JsonLog.Info(args.Tag, args.Message);
                    break;
                case JsonLogLevel.Warning:
                    JsonLog.Warning(args.Tag, args.Message);
                    break;
                case JsonLogLevel.Error:
                    JsonLog.Error(args.Tag, args.Message);
                    break;
                case JsonLogLevel.Exception:
                    JsonLog.Exception(args.Tag, args.Exception, args.Message);
                    break;
            }
        }
    }
}
