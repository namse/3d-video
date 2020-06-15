using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

namespace VideoMetaExtractor
{
    public static class PngToArgb
    {
        public static byte[] Convert(Stream pngImageStream)
        {
            using var bitmap = new Bitmap(pngImageStream);
            var rect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);

            var argbBytes = new byte[bitmap.Width * bitmap.Height * 4];

            var bitmapData = bitmap.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

            Marshal.Copy(bitmapData.Scan0, argbBytes, 0, argbBytes.Length);

            return argbBytes;
        }
    }
}