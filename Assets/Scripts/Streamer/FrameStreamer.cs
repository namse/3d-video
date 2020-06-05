using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ByteBuffer;
using UnityEngine;

public class FrameStreamer: IFrameStreamer
{
    private readonly StreamBufferProvider _bufferProvider;
    private readonly Dictionary<int, Texture> _frameTextures = new Dictionary<int, Texture>();
    private readonly object _mutex = new object();
    private int _lastFrameNumberToSkip = -1;
    private int _nextFrameNumber = 0;
    private readonly CancellationTokenSource _pipeReadingCancellationTokenSource = new CancellationTokenSource();
    private readonly Dav1dDecoder _dav1dDecoder = Dav1dDecoder.Instance;

    public FrameStreamer(StreamBufferProvider bufferProvider)
    {
        _bufferProvider = bufferProvider;
        _dav1dDecoder.BufferReturned += Dav1dDecoderOnBufferReturned;

        _ = Task.Run(async () =>
        {
            try
            {
                await _bufferProvider.RunAsync(OnBufferReceived);
            }
            catch (Exception exception)
            {
                Debug.Log(exception);
            }
            
        });
    }

    private void Dav1dDecoderOnBufferReturned(ByteMemory buffer)
    {
        _bufferProvider.Return(buffer);
    }

    private void OnBufferReceived(ByteMemory buffer)
    {
        _dav1dDecoder.SendData(buffer);
        if (_dav1dDecoder.TryGetNextFrameTexture(out var frameTexture))
        {
            OnFrameReceived(frameTexture);
        }
    }

    public void Dispose()
    {
        _bufferProvider.Dispose();
        _pipeReadingCancellationTokenSource.Cancel();
    }

    public bool TryGetFrame(int frameNumber, out Texture frameTexture)
    {
        lock (_mutex)
        {
            return _frameTextures.TryGetValue(frameNumber, out frameTexture);
        }
    }

    public void SkipTo(int frameNumber)
    {
        _lastFrameNumberToSkip = frameNumber;
        RemoveSkippedFrames();
    }

    private void RemoveSkippedFrames()
    {
        lock (_mutex)
        {
            var frameTuplesToSkip = _frameTextures
                .Where(tuple=> tuple.Key <= _lastFrameNumberToSkip)
                .ToList();

            foreach (var frameTuple in frameTuplesToSkip)
            {
                _dav1dDecoder.ReturnFrameTexture(frameTuple.Value);
                _frameTextures.Remove(frameTuple.Key);
            }
        }
    }

    private void OnFrameReceived(Texture frameTexture)
    {
        var frameNumber = _nextFrameNumber;
        lock (_mutex)
        {
            _frameTextures.Add(frameNumber, frameTexture);
        }

        _nextFrameNumber += 1;
    }
}