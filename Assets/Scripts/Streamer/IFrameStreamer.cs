using System;
using UnityEngine;

public interface IFrameStreamer: IDisposable
{
    bool TryGetFrame(int frameNumber, out Texture frameTexture);
    void SkipTo(int frameNumber);
}