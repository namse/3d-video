using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ByteBuffer;
using UnityEngine;

public delegate void OnBufferReceived(ByteMemory buffer);

public class StreamBufferProvider: IDisposable
{
    private readonly Stream _stream;
    private readonly int _minBufferSize;
    private readonly Dictionary<ByteMemory, IByteMemoryOwner> _memoryOwners = new Dictionary<ByteMemory, IByteMemoryOwner>();
    private readonly object _mutex = new object();
    private int _allocatedBytes;
    private readonly SemaphoreSlim _throttleSemaphore;
    private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

    public StreamBufferProvider(Stream stream, int minBufferSize, int throttleBufferCounts)
    {
        _stream = stream;
        _minBufferSize = minBufferSize;
        _throttleSemaphore = new SemaphoreSlim(throttleBufferCounts);
    }

    public async Task RunAsync(OnBufferReceived onBufferReceived)
    {
        while (!_cancellationTokenSource.IsCancellationRequested)
        {
            await _throttleSemaphore.WaitAsync(_cancellationTokenSource.Token);

            var bufferOwner = ByteMemoryPool.Shared.Rent(_minBufferSize);
            Interlocked.Add(ref _allocatedBytes, bufferOwner.Memory.Length);
            
            try
            {
                var memory = bufferOwner.Memory;
                var readLength = await _stream.ReadAsync(memory.OriginalArray, memory.OriginalArrayOffset,
                    memory.Length, _cancellationTokenSource.Token);

                if (readLength == 0)
                {
                    break;
                }

                var actualBuffer = memory.Slice(0, readLength);

                lock (_mutex)
                {
                    _memoryOwners.Add(actualBuffer, bufferOwner);
                }

                onBufferReceived(actualBuffer);
            }
            catch (Exception exception)
            {
                Debug.LogWarning(exception);
                ReturnMemoryOwner(bufferOwner);
            }
        }
    }

    private void ReturnMemoryOwner(IByteMemoryOwner memoryOwner)
    {
        _throttleSemaphore.Release();
        Interlocked.Add(ref _allocatedBytes, -memoryOwner.Memory.Length);
        memoryOwner.Dispose();
    }

    public void Return(ByteMemory memory)
    {
        IByteMemoryOwner owner;
        lock (_mutex)
        {
            if (!_memoryOwners.TryGetValue(memory, out owner))
            {
                Debug.LogWarning("cannot find memory");
                return;
            }
        }
        ReturnMemoryOwner(owner);
    }

    public void Dispose()
    {
        _cancellationTokenSource.Cancel();
        lock (_mutex)
        {
            foreach (var keyValuePair in _memoryOwners)
            {
                ReturnMemoryOwner(keyValuePair.Value);
            }
        }
        _stream?.Dispose();
        _throttleSemaphore?.Dispose();
    }
}
