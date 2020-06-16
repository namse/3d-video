using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace StencilFrameLibrary
{
    public static class StencilPainter
    {
        public static uint[] ConvertOrderByteToOrderIndexes(uint orderByte)
        {
            var orderIndexes = new List<uint>();
            for (var i = 0; i < 8; i += 1)
            {
                var index = (orderByte & 0xF0000000) >> 28;
                orderIndexes.Add(index);
                orderByte <<= 4;
            }

            return orderIndexes.Distinct().ToArray();
        }

        public static StencilFrame[] OrderStencilFrames8(StencilFrame[] stencilFrames, uint[] orderIndexes)
        {
            var returnFrames = new StencilFrame[orderIndexes.Length];
            for (var i = 0; i < orderIndexes.Length; i += 1)
            {
                var orderIndex = orderIndexes[i];
                returnFrames[i] = stencilFrames[orderIndex];
            }

            return returnFrames;
        }

        public static Yuv444Frame PaintYuvFrame(int width, int height, StencilFrame[] stencilFrames)
        {
            var resultYuv444 = new Yuv444Frame(width, height);

            foreach (var stencilFrame in stencilFrames)
            {
                PaintYuvFrame(resultYuv444, stencilFrame);
            }

            return resultYuv444;
        }

        public static void PaintYuvFrame(Yuv444Frame yuv444Frame, StencilFrame stencilFrame)
        {
            for (var maskYIndex = 0; maskYIndex < 2; maskYIndex += 1)
            {
                for (var maskXIndex = 0; maskXIndex < 4; maskXIndex += 1)
                {
                    var maskIndex = maskYIndex * 4 + maskXIndex;
                    var maskValue = stencilFrame.MaskTree & (1 << maskIndex);
                     var isPassed = maskValue == 0;
                    if (isPassed)
                    {
                        continue;
                    }

                    var startX = maskXIndex * stencilFrame.Width / 4;
                    var startY = maskYIndex * stencilFrame.Height / 2;
                    var width = stencilFrame.Width / 4;
                    var height = stencilFrame.Height / 2;
                    PaintYuvFrame(yuv444Frame, stencilFrame, startX, startY, width, height);
                }
            }
        }

        public static unsafe void PaintYuvFrame(Yuv444Frame yuv444Frame, StencilFrame stencilFrame,
            int startX, int startY, int width, int height)
        {
            var yPtr = (byte*)stencilFrame.YPtr.ToPointer();
            var uPtr = (byte*)stencilFrame.UPtr.ToPointer();
            var vPtr = (byte*)stencilFrame.VPtr.ToPointer();
            var alphaPtr = (byte*) stencilFrame.AlphaPtr.ToPointer();


            for (var y = startY; y < startY + height; y += 1)
            {
                for (var x = startX; x < startX + width; x += 1)
                {
                    var pixelIndex = stencilFrame.Width * y + x;
                    if (alphaPtr[pixelIndex] <= 0 || yuv444Frame.ColoredBytes[pixelIndex])
                    {
                        continue;
                    }

                    var stencilChromaIndex = (stencilFrame.Width / 2) * (y / 2) + (x / 2);

                    yuv444Frame.YBytes[pixelIndex] = yPtr[pixelIndex];
                    yuv444Frame.UBytes[pixelIndex] = uPtr[stencilChromaIndex];
                    yuv444Frame.VBytes[pixelIndex] = vPtr[stencilChromaIndex];
                    yuv444Frame.ColoredBytes[pixelIndex] = true;
                }
            }
        }
    }
}