using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Dav1dDotnet;
using Dav1dDotnet.Decoder;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;

public class FrameStreamerTester : MonoBehaviour
{
    private IvfAv1Decoder _ivfAv1Decoder;
    private int _frameNumber;
    private readonly Stopwatch _stopwatch = new Stopwatch();
    public RawImage rawImage;

    void Start()
    {

        var asset = Resources.Load("whiteAlpha");
        var textAsset = asset as TextAsset;
        var stream = new MemoryStream(textAsset.bytes);

        _ivfAv1Decoder = new IvfAv1Decoder(stream);
    }

    void Update()
    {
        if (_frameNumber == 1)
        {
            _stopwatch.Start();
        }
        if (_frameNumber == 179)
        {
            _stopwatch.Stop();
            Debug.Log(_stopwatch.Elapsed);
        }
        if (_ivfAv1Decoder.TryGetAv1Frame(_frameNumber, out var av1Frame))
        {
            rawImage.texture = GetTextureFromAv1Frame(av1Frame);
            _ivfAv1Decoder.CheckConsumedFrameNumber(_frameNumber);

            _frameNumber += 1;
        }
    }

    void OnDestroy()
    {
        _ivfAv1Decoder?.Dispose();
    }

    private Texture GetTextureFromAv1Frame(Av1Frame av1Frame)
    {
        const int chunkHeight = 1080 / 8;
        var rgbList = new NativeArray<byte>(1920 * 1080 * 3, Allocator.TempJob);

        var job = new YuvToRgbJob
        {
            LumaBytesPtr = av1Frame.Picture._data[0],
            UBytesPtr = av1Frame.Picture._data[1],
            VBytesPtr = av1Frame.Picture._data[2],
            ChunkHeight = chunkHeight,
            RgbList = rgbList,
        };

        job.Schedule(1080 / chunkHeight, 1).Complete();

        var texture = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
        texture.SetPixelData(rgbList, 0);
        texture.Apply();

        rgbList.Dispose();
        return texture;
    }

    [BurstCompile(CompileSynchronously = true)]

    private struct YuvToRgbJob : IJobParallelFor
    {
        [ReadOnly]
        [NativeDisableUnsafePtrRestriction]
        public IntPtr LumaBytesPtr;

        [ReadOnly]
        [NativeDisableUnsafePtrRestriction]
        public IntPtr UBytesPtr;

        [ReadOnly]
        [NativeDisableUnsafePtrRestriction]
        public IntPtr VBytesPtr;

        [ReadOnly]
        public int ChunkHeight;

        [WriteOnly]
        [NativeDisableParallelForRestriction]
        public NativeArray<byte> RgbList;


        private int convertYUVtoRGB(int y, int u, int v)
        {
            var c = y - 16;
            var d = u - 128;
            var e = v - 128;

            var r = (298 * c + 409 * e + 128) >> 8;
            var g = (298 * c - 100 * d - 208 * e + 128) >> 8;
            var b = (298 * c + 516 * d + 128) >> 8;

            r = r > 255 ? 255 : r < 0 ? 0 : r;
            g = g > 255 ? 255 : g < 0 ? 0 : g;
            b = b > 255 ? 255 : b < 0 ? 0 : b;

            return r << 16 | g << 8 | b;
        }

        public void Execute(int index)
        {
            unsafe
            {
                var lumaBytesPtr = (byte*)LumaBytesPtr.ToPointer();
                var uBytesPtr = (byte*)UBytesPtr.ToPointer();
                var vBytesPtr = (byte*)VBytesPtr.ToPointer();

                const int width = 1920;
                const int height = 1080;

                for (var dy = 0; dy < ChunkHeight; dy += 1)
                {
                    var y = dy + ChunkHeight * index;
                    if (y >= height)
                    {
                        break;
                    }
                    for (var x = 0; x < width; x += 1)
                    {
                        var xy = x + y * width;
                        var luma = lumaBytesPtr[xy];

                        var uvIndex = x / 2 + (y / 2) * width / 2;
                        var u = uBytesPtr[uvIndex];
                        var v = vBytesPtr[uvIndex];

                        var rgb = convertYUVtoRGB(luma, u, v);
                        RgbList[xy * 3 + 0] = (byte)(rgb >> 16);
                        RgbList[xy * 3 + 1] = (byte)(rgb >> 8);
                        RgbList[xy * 3 + 2] = (byte)(rgb >> 0);
                    }
                }
            }
        }
    }
}
