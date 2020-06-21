using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dav1dDotnet.Decoder;

namespace StencilFrameLibrary
{
    public readonly struct StencilFrame
    {
        public readonly byte MaskTree;
        public readonly int Width;
        public readonly int Height;
        public readonly IntPtr YPtr; // yuv420
        public readonly IntPtr UPtr; // yuv420
        public readonly IntPtr VPtr; // yuv420
        public readonly IntPtr AlphaPtr; // 1byte per 1 pixel

        public StencilFrame(IntPtr yPtr, IntPtr uPtr, IntPtr vPtr, IntPtr alphaPtr, byte maskTree, int width, int height)
        {
            YPtr = yPtr;
            UPtr = uPtr;
            VPtr = vPtr;
            AlphaPtr = alphaPtr;
            MaskTree = maskTree;
            Width = width;
            Height = height;
        }
    }
}
