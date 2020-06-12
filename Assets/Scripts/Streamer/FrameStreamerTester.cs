using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using AOT;
using Dav1dDotnet;
using Dav1dDotnet.Dav1d.Definitions;
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
    private IvfAv1Decoder[] _ivfAv1Decoders = {};

    private int _frameNumber;
    private readonly Stopwatch _stopwatch = new Stopwatch();
    public RawImage rawImage;
    public Text text;

    void Start()
    {
        _ivfAv1Decoders = new IvfAv1Decoder[1];
        var asset = Resources.Load("whiteAlpha");
        var textAsset = asset as TextAsset;
        for (var i = 0; i < _ivfAv1Decoders.Length; i += 12)
        {
            var stream = new MemoryStream(textAsset.bytes);

            var ivfAv1Decoder = new IvfAv1Decoder(stream);
            _ivfAv1Decoders[i] = ivfAv1Decoder;
        }
        
        rawImage.texture = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
    }

    void OnDestroy()
    {
        foreach (var ivfAv1Decoder in _ivfAv1Decoders)
        {
            ivfAv1Decoder?.Dispose();
        }
    }

    void Update()
    {
        text.text = _frameNumber.ToString();
        if (_frameNumber == 1)
        {
            _stopwatch.Start();
        }
        if (_frameNumber == 179)
        {
            _stopwatch.Stop();
            Debug.Log(_stopwatch.Elapsed);
        }

        if (_ivfAv1Decoders.All(ivfAv1Decoder => ivfAv1Decoder.TryGetAv1Frame(_frameNumber, out _)))
        {
            //var frames = _ivfAv1Decoders.Select(ivfAv1Decoder =>
            //{
            //    ivfAv1Decoder.TryGetAv1Frame(_frameNumber, out var av1Frame);
            //    return av1Frame;
            //}).ToList();

            //var texture = rawImage.texture as Texture2D;
            
            //GetTextureFromAv1Frames(frames, ref texture);
            

            //foreach (var ivfAv1Decoder in _ivfAv1Decoders)
            //{
            //    ivfAv1Decoder.CheckConsumedFrameNumber(_frameNumber);
            //}

            _frameNumber += 1;
        }
    }

    private void GetTextureFromAv1Frames(List<Av1Frame> frames, ref Texture2D texture)
    {
        const int chunkHeight = 1080 / 32;

        using (var lumaBytesPtrs = new NativeArray<IntPtr>(frames.Select(frame => frame.Picture._data[0]).ToArray(), Allocator.TempJob))
        using (var uBytesPtrs = new NativeArray<IntPtr>(frames.Select(frame => frame.Picture._data[1]).ToArray(), Allocator.TempJob))
        using (var vBytesPtrs = new NativeArray<IntPtr>(frames.Select(frame => frame.Picture._data[2]).ToArray(), Allocator.TempJob))
        using (var rgbList = new NativeArray<byte>(1920 * 1080 * 3, Allocator.TempJob))
        {
            var job = new YuvToRgbJob
            {
                LumaBytesPtrs = lumaBytesPtrs,
                UBytesPtrs = uBytesPtrs,
                VBytesPtrs = vBytesPtrs,
                ChunkHeight = chunkHeight,
                RgbList = rgbList,
            };

            job.Schedule(1080 / chunkHeight, 1).Complete();

            texture.SetPixelData(rgbList, 0);
            texture.Apply();
        }
    }

    [BurstCompile(CompileSynchronously = true)]

    private struct YuvToRgbJob : IJobParallelFor
    {
        [ReadOnly]
        [NativeDisableParallelForRestriction]
        [NativeDisableUnsafePtrRestriction]
        public NativeArray<IntPtr> LumaBytesPtrs;

        [ReadOnly]
        [NativeDisableParallelForRestriction]
        [NativeDisableUnsafePtrRestriction]
        public NativeArray<IntPtr> UBytesPtrs;

        [ReadOnly]
        [NativeDisableParallelForRestriction]
        [NativeDisableUnsafePtrRestriction]
        public NativeArray<IntPtr> VBytesPtrs;

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
                        var lumaBytesPtr = (byte*)LumaBytesPtrs[0].ToPointer();
                        var uBytesPtr = (byte*)UBytesPtrs[0].ToPointer();
                        var vBytesPtr = (byte*)VBytesPtrs[0].ToPointer();

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
