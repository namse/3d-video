namespace StencilFrameLibrary
{
    public readonly struct Yuv444Frame
    {
        public readonly byte[] YBytes;
        public readonly byte[] UBytes;
        public readonly byte[] VBytes;
        public readonly bool[] ColoredBytes;
        public readonly int Width;
        public readonly int Height;

        public Yuv444Frame(byte[] yBytes, byte[] uBytes, byte[] vBytes, bool[] coloredBytes, int width, int height)
        {
            YBytes = yBytes;
            UBytes = uBytes;
            VBytes = vBytes;
            ColoredBytes = coloredBytes;
            Width = width;
            Height = height;
        }

        public Yuv444Frame(int width, int height)
        {
            YBytes = new byte[width * height];
            UBytes = new byte[width * height];
            VBytes = new byte[width * height];
            ColoredBytes = new bool[width * height];
            Width = width;
            Height = height;
        }
    }
}