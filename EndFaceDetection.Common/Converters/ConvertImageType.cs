using SixLabors.ImageSharp.Formats;
using System;
using System.Collections.Generic;
using System.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Image = SixLabors.ImageSharp.Image;
using EndFaceDetection.LogModule;

namespace EndFaceDetection.Common.Converters
{
    public static class ConvertImageType
    {
        public static byte[] ConvertSixLaborsImage2ByteArray(Image image)
        {
            if (image == null)
            {
                return null;
            }
            try
            {
                using (var memoryStream = new MemoryStream())
                {
                    image.Save(memoryStream, image.Configuration.ImageFormats.First(f => f.Name == "PNG"));
                    return memoryStream.ToArray();
                }
            }
            catch (Exception ex) 
            {
                LoggerHelper._.Error("",ex);
                return null;
            }
        }

        public static Image ConvertByteArray2SixLaborsImage(byte[] imageData,int width,int height)
        {
            return Image.LoadPixelData<L8>(imageData, width, height);
        }
        
        public static Image LoadImage(string path)
        {
            return Image.Load(path);
        }
    }
}
