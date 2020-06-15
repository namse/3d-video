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
        private const int Width = 16;
        private const int Height = 2;
        private MemoryStream GeneratePngStream()
        {
            var memoryStream = new MemoryStream();

            var bitmap = new Bitmap(Width, Height);
            for (var i =0; i < Width / 4; i += 1)
            {
                bitmap.SetPixel(0 + i * Width / 4, 0, Color.FromArgb(0xFF, 0x00, 0x00, 0x00));
                bitmap.SetPixel(1 + i * Width / 4, 0, Color.FromArgb(0x00, 0xFF, 0x00, 0x00));
                bitmap.SetPixel(2 + i * Width / 4, 0, Color.FromArgb(0x00, 0x00, 0xFF, 0x00));
                bitmap.SetPixel(3 + i * Width / 4, 0, Color.FromArgb(0x00, 0x00, 0x00, 0xFF));
                bitmap.SetPixel(0 + i * Width / 4, 1, Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF));
                bitmap.SetPixel(1 + i * Width / 4, 1, Color.FromArgb(0xFF, 0x00, 0x00, 0xFF));
                bitmap.SetPixel(2 + i * Width / 4, 1, Color.FromArgb(0xFF, 0xFF, 0x00, 0xFF));
                bitmap.SetPixel(3 + i * Width / 4, 1, Color.FromArgb(0x00, 0x00, 0x00, 0x00));
            }

            for (var i = 0; i < Width / 4; i += 1)
            {
                bitmap.SetPixel(Width - 1 - i, Height - 1, Color.FromArgb(0x00, 0xFF, 0xFF, 0xFF));
            }
            

            bitmap.Save(memoryStream, ImageFormat.Png);

            return memoryStream;
        }

        [TestMethod]
        public unsafe void ConvertPngToRgba_should_work()
        {
            var memoryStream = GeneratePngStream();
            var argbBytes = PngToArgb.Convert(memoryStream);
            Assert.AreEqual(argbBytes.Length, 16 * 2* 4);

            var argbUints = argbBytes.ToUintPtr();

            Assert.IsTrue(argbUints[0] == 0xFF000000);
            Assert.IsTrue(argbUints[1] == 0x00FF0000);
            Assert.IsTrue(argbUints[2] == 0x0000FF00);
            Assert.IsTrue(argbUints[3] == 0x000000FF);
            Assert.IsTrue(argbUints[4 + Width] == 0xFFFFFFFF);
            Assert.IsTrue(argbUints[5 + Width] == 0xFF0000FF);
            Assert.IsTrue(argbUints[6 + Width] == 0xFFFF00FF);
            Assert.IsTrue(argbUints[7 + Width] == 0x00000000);

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

            Assert.IsTrue(argbUints[4 + Width] == 0xFFFFFFFF);
            Assert.IsTrue(argbUints[5 + Width] == 0xFFFFFFFF);
            Assert.IsTrue(argbUints[6 + Width] == 0xFFFFFFFF);
            Assert.IsTrue(argbUints[7 + Width] == 0x00000000);

            memoryStream.Dispose();
        }

        [TestMethod]
        public void CreateMaskTree_should_work()
        {
            var memoryStream = GeneratePngStream();
            var argbBytes = PngToArgb.Convert(memoryStream);
            var alpha32Bytes = ExtractAlpha.ArgbToAlpha32(argbBytes);
            var maskTree = MaskTree.Make(alpha32Bytes, Width, Height);

            Assert.IsTrue(maskTree == 0b11111110);
            
            memoryStream.Dispose();
        }
    }
}
