using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace VideoMetaExtractor
{
    // REMEMBER ARGB !!
    public static class ExtractAlpha
    {
        private static readonly Vector128<uint> alphaOnlyVector128;

        static unsafe ExtractAlpha()
        {
            var alphaOnly128 = new uint[8];
            for (var i = 0; i < alphaOnly128.Length; i += 1)
            {
                alphaOnly128[i] = 0xFF000000;
            }

            fixed (void* alphaOnlyPtr = alphaOnly128)
            {
                alphaOnlyVector128 = Avx2.LoadVector128((uint*)alphaOnlyPtr);
            }
        }

        public static unsafe byte[] ArgbToAlpha32(byte[] argbBytes)
        {
            var result = new byte[argbBytes.Length];
            var resultPtr = result.ToPtr();
            var bytePtr = argbBytes.ToPtr();

            for (var ptrIndex = 0; ptrIndex < argbBytes.Length; ptrIndex += 16)
            {
                var vector128 = Avx2.LoadVector128(bytePtr + ptrIndex).AsUInt32();

                vector128 = Avx2.And(vector128, alphaOnlyVector128);
                vector128 = Avx2.Or(vector128, Avx2.ShiftRightLogical128BitLane(vector128, 1));
                vector128 = Avx2.Or(vector128, Avx2.ShiftRightLogical128BitLane(vector128, 2));


                Avx2.Store(resultPtr + ptrIndex, vector128.AsByte());
            }

            return result;
        }
    }
}