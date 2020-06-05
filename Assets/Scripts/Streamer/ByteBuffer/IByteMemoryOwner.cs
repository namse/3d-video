using System;

namespace ByteBuffer
{
    public interface IByteMemoryOwner: IDisposable
    {
        ByteMemory Memory { get; }
    }
}