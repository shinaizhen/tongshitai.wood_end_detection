using Image = SixLabors.ImageSharp.Image;

namespace EndFaceDetection.Services.MInterfaces
{
    public interface ICamera
    {
        Action<string, bool> StateChanged { get; set; }
        Action<ImageInfo> CBImage { get; set; }
    }
}
