using System.Collections.Generic;
using System.Linq;

namespace ByteBuffer
{
    public class ByteMemoryPool
    {
        public static ByteMemoryPool Shared { get; } = new ByteMemoryPool();
        private readonly Dictionary<int, Stack<ByteMemory>> _availableMemories = new Dictionary<int, Stack<ByteMemory>>();
        private readonly object _mutex = new object();

        public IByteMemoryOwner Rent(int minSize)
        {
            lock (_mutex)
            {
                var powerOfTwo = GetMinGreaterPowerOfTwo(minSize);
                ByteMemory memory;

                if (_availableMemories.TryGetValue(powerOfTwo, out var stack) && stack.Any())
                {
                    memory = stack.Pop();
                } 
                else
                {
                    memory = new ByteMemory(new byte[powerOfTwo], 0, powerOfTwo);
                }

                var owner = new ByteMemoryOwner(memory, this);
                return owner;
            }
        }

        private static int GetMinGreaterPowerOfTwo(int value)
        {
            var i = 1;

            while (i < value)
            {
                i *= 2;
            }

            return i;
        }

        private void Return(IByteMemoryOwner memoryOwner)
        {
            lock (_mutex)
            {
                if (!_availableMemories.TryGetValue(memoryOwner.Memory.Length, out var stack))
                {
                    stack = new Stack<ByteMemory>();
                    stack.Push(memoryOwner.Memory);
                    _availableMemories[memoryOwner.Memory.Length] = stack;
                }

                stack.Push(memoryOwner.Memory);
            }
        }

        private struct ByteMemoryOwner: IByteMemoryOwner
        {
            private readonly ByteMemoryPool _memoryPool;
            private bool _disposed;

            public ByteMemoryOwner(ByteMemory memory, ByteMemoryPool memoryPool)
            {
                _memoryPool = memoryPool;
                Memory = memory;
                _disposed = false;
            }

            public void Dispose()
            {
                if (_disposed)
                {
                    return;
                }
                _disposed = true;
                _memoryPool.Return(this);
            }

            public ByteMemory Memory { get; }
        }
    }
}