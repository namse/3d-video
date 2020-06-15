using System.Reflection.Metadata.Ecma335;

namespace VideoMetaExtractor
{
    public static class MaskTree
    {
        public static byte Make(byte[] alpha32Bytes, int width, int height)
        {
            byte result = 0;

            for (var yIndex = 0; yIndex < 2; yIndex += 1)
            {
                for (var xIndex = 0; xIndex < 4; xIndex += 1)
                {
                    var isExists = false;
                    for (var y = yIndex * height / 2; y < (yIndex + 1) * height / 2; y += 1)
                    {
                        for (var x = xIndex * width / 4; x < (xIndex + 1) * width / 4; x += 1)
                        {
                            for (var i = 0; i < 4; i += 1)
                            {
                                var index = (x + (y * width)) * 4 + i;
                                if (alpha32Bytes[index] != 0)
                                {
                                    isExists = true;
                                    goto done;
                                }
                            }
                        }
                    }
                    done:
                    result <<= 1;
                    if (isExists)
                    {
                        result += 1;
                    }
                }
            }

            return result;
        }
    }
}