namespace VideoMetaExtractor
{
    public static unsafe class BytesConverterExtensions
    {
        public static uint* ToUintPtr(this byte[] bytes)
        {
            fixed (void* ptr = bytes)
            {
                return (uint*) ptr;
            }
        }
        public static byte* ToPtr(this byte[] bytes)
        {
            fixed (void* ptr = bytes)
            {
                return (byte*)ptr;
            }
        }
    }
}