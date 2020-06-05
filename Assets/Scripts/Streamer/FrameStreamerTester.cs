using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

public class FrameStreamerTester : MonoBehaviour
{
    void Start()
    {
        var data = new byte[100]; 
        for (var i = 0; i < data.Length; i += 1)
        {
            data[i] = (byte)(i % 256);
        }
        var stream = new MemoryStream(data);

        var streamBufferProvider = new StreamBufferProvider(stream, 5, 10);
        var frameStreamer = new FrameStreamer(streamBufferProvider);
    }
}
