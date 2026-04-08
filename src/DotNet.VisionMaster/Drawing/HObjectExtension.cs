using HalconDotNet;

namespace DotNet.VisionMaster
{
    public static class HObjectExtension
    {
        public static bool NotNull(this HObject image)
        {
            if ((object)image != null)
            {
                return image.IsInitialized();
            }
            return false;
        }
    }
}
