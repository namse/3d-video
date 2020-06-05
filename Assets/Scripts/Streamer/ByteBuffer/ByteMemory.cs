using System;

namespace ByteBuffer
{
    public readonly struct ByteMemory
    {
        public byte[] OriginalArray { get; }
        public int OriginalArrayOffset { get; }
        public IntPtr Pointer { get; }
        public int Length { get; }

        public ByteMemory(byte[] originalArray, int originalArrayOffset, int count)
        {
            OriginalArray = originalArray;
            OriginalArrayOffset = originalArrayOffset;
            unsafe
            {
                fixed (byte* pointer = originalArray)
                {
                    Pointer = ((IntPtr)pointer) + originalArrayOffset;
                    Length = count - originalArrayOffset;
                }
            }
        }

        public ByteMemory Slice(int offset, int count)
        {
            return new ByteMemory(OriginalArray, OriginalArrayOffset + offset, count);
        }
    }
}
