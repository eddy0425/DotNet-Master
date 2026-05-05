using System.IO;

namespace DotNet.HalconAlgo
{
    public static class AlgoPaths
    {
        public static string ProjectDir = "Config";
        public static string SchemeDir = Path.Combine(ProjectDir, "Scheme");
        public static string JobDir = Path.Combine(SchemeDir, "Job");

        public static string System = Path.Combine(ProjectDir, "System.json");
        public static string SchemeInfo = "SchemeInfo.json";
        public static string JobInfo = "JobInfo.json";

        public static bool UIBlock = true;
    }
}
