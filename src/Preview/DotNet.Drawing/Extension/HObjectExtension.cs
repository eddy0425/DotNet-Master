using HalconDotNet;

namespace DotNet.Drawing
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
