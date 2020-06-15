using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VideoMetaExtractor;

namespace Test
{
    [TestClass]
    [DeploymentItem(@"TestImage.png", "Resources")]
    public class UnitTest1
    {
        private MemoryStream GeneratePngStream()
        {
            var memoryStream = new MemoryStream();

            var bitmap = new Bitmap(4, 2);
            bitmap.SetPixel(0, 0, Color.FromArgb(0xFF, 0x00, 0x00, 0x00));
            bitmap.SetPixel(1, 0, Color.FromArgb(0x00, 0xFF, 0x00, 0x00));
            bitmap.SetPixel(2, 0, Color.FromArgb(0x00, 0x00, 0xFF, 0x00));
            bitmap.SetPixel(3, 0, Color.FromArgb(0x00, 0x00, 0x00, 0xFF));
            bitmap.SetPixel(0, 1, Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF));
            bitmap.SetPixel(1, 1, Color.FromArgb(0xFF, 0x00, 0x00, 0xFF));
            bitmap.SetPixel(2, 1, Color.FromArgb(0xFF, 0xFF, 0x00, 0xFF));
            bitmap.SetPixel(3, 1, Color.FromArgb(0x00, 0x00, 0x00, 0x00));
            bitmap.Save(memoryStream, ImageFormat.Png);

            return memoryStream;
        }

        [TestMethod]
        public unsafe void ConvertPngToRgba_should_work()
        {
            var memoryStream = GeneratePngStream();
            var argbBytes = PngToArgb.Convert(memoryStream);
            Assert.AreEqual(argbBytes.Length, 4 * 2* 4);

            var argbUints = argbBytes.ToUintPtr();

            Assert.IsTrue(argbUints[0] == 0xFF000000);
            Assert.IsTrue(argbUints[1] == 0x00FF0000);
            Assert.IsTrue(argbUints[2] == 0x0000FF00);
            Assert.IsTrue(argbUints[3] == 0x000000FF);
            Assert.IsTrue(argbUints[4] == 0xFFFFFFFF);
            Assert.IsTrue(argbUints[5] == 0xFF0000FF);
            Assert.IsTrue(argbUints[6] == 0xFFFF00FF);
            Assert.IsTrue(argbUints[7] == 0x00000000);

            memoryStream.Dispose();
        }
        
        [TestMethod]
        public unsafe void ExtractAlpha_should_work()
        {
            var memoryStream = GeneratePngStream();
            var argbBytes = PngToArgb.Convert(memoryStream);

            var alpha32Bytes = ExtractAlpha.ArgbToAlpha32(argbBytes);

            var argbUints = alpha32Bytes.ToUintPtr();
            
            Assert.IsTrue(argbUints[0] == 0xFFFFFFFF);
            Assert.IsTrue(argbUints[1] == 0x00000000);
            Assert.IsTrue(argbUints[2] == 0x00000000);
            Assert.IsTrue(argbUints[3] == 0x00000000);

            Assert.IsTrue(argbUints[4] == 0xFFFFFFFF);
            Assert.IsTrue(argbUints[5] == 0xFFFFFFFF);
            Assert.IsTrue(argbUints[6] == 0xFFFFFFFF);
            Assert.IsTrue(argbUints[7] == 0x00000000);

            memoryStream.Dispose();
        }
    }
}
