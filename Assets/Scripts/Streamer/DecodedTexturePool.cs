using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Amazon.S3.Model;
using Dav1dDotnet;
using Dav1dDotnet.Decoder;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;


public class DecodedTexturePool : MonoBehaviour, IDecoder
{
    private readonly Stopwatch _stopwatch = new Stopwatch();
    private IvfAv1Decoder[] _ivfAv1Decoders;
    private Queue<Texture2D> _decodedQueue;
    private Queue<Texture2D> _freeTextureQueue;
    private List<Texture2D> _rawTextures;
    private const int ChunkHeight = 1080 / 8;
    private Queue<JobMemory> _jobMemories;

    private int _frameNumber;

    void Start()
    {
        _ivfAv1Decoders = new IvfAv1Decoder[1];
        _decodedQueue = new Queue<Texture2D>();
        _freeTextureQueue = new Queue<Texture2D>();
        _rawTextures = new List<Texture2D>();
        _jobMemories = new Queue<JobMemory>();

        var asset = Resources.Load("whiteAlpha");
        var textAsset = asset as TextAsset;

        for (var i = 0; i < _ivfAv1Decoders.Length; i += 1)
        {
            var stream = new MemoryStream(textAsset.bytes);
            _ivfAv1Decoders[i] = new IvfAv1Decoder(stream);
        }

        for (var i = 0; i < 4; i += 1)
        {
            var texture = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
            _freeTextureQueue.Enqueue(texture);
            _rawTextures.Add(texture);
        }
    }

    void Update()
    {
        ScheduleAvailableFrames();
        UpdateTextureFromCompletedJob();
    }

    private void UpdateTextureFromCompletedJob()
    {
        while (_jobMemories.Count > 0 && _jobMemories.Peek().jobHandle.IsCompleted && _freeTextureQueue.Count > 0)
        {
            _stopwatch.Restart();

            var jobMemory = _jobMemories.Dequeue();
            jobMemory.jobHandle.Complete();

            var texture = _freeTextureQueue.Dequeue();
            texture.SetPixelData(jobMemory._rgbList, 0);
            texture.Apply();
            _decodedQueue.Enqueue(texture);

            foreach (var ivfAv1Decoder in _ivfAv1Decoders)
            {
                ivfAv1Decoder.CheckConsumedFrameNumber(jobMemory.frameNumber);
            }
            JobMemory.Return(jobMemory);

            _stopwatch.Stop();
            Debug.Log($"UpdateTextureFromCompletedJob 1 Cycle {_stopwatch.ElapsedMilliseconds}");
        }
    }

    private void ScheduleAvailableFrames()
    {
        Debug.Log($"_jobMemories.Count {_jobMemories.Count}");
        const int maxJobMemories = 4;
        while (_jobMemories.Count < maxJobMemories
            && _ivfAv1Decoders.All(ivfAv1Decoder => ivfAv1Decoder.TryGetAv1Frame(_frameNumber, out _)))
        {
            _stopwatch.Restart();

            var frames = _ivfAv1Decoders.Select(ivfAv1Decoder =>
            {
                ivfAv1Decoder.TryGetAv1Frame(_frameNumber, out var av1Frame);
                return av1Frame;
            }).ToList();

            var jobMemory = ScheduleYuvToRgbJob(frames, _frameNumber);
            _jobMemories.Enqueue(jobMemory);

            _frameNumber += 2;

            _stopwatch.Stop();
            Debug.Log($"ScheduleAvailableFrames 1 Cycle {_stopwatch.ElapsedMilliseconds}");
        }
    }

    public bool TryGetNextTexture(out Texture2D texture)
    {
        if (_decodedQueue.Count > 0)
        {
            texture = _decodedQueue.Dequeue();
            return true;
        }

        texture = null;
        return false;
    }

    public void ReturnTexture(Texture2D texture)
    {
        _freeTextureQueue.Enqueue(texture);
    }

    public int AvailableTextureCount => _decodedQueue.Count;

    void OnDestroy()
    {
        _rawTextures.ForEach(Object.Destroy);
        foreach (var ivfAv1Decoder in _ivfAv1Decoders)
        {
            ivfAv1Decoder?.Dispose();
        }
        foreach (var jobMemory in _jobMemories)
        {
            jobMemory.jobHandle.Complete();
            jobMemory.Dispose();
        }

        JobMemory.DestroyPool();
    }


    private class JobMemory : IDisposable
    {
        private readonly NativeArray<IntPtr> _lumaBytesPtrs;
        private readonly NativeArray<IntPtr> _uBytesPtrs;
        private readonly NativeArray<IntPtr> _vBytesPtrs;
        public readonly NativeArray<byte> _rgbList;
        public JobHandle jobHandle;

        private static readonly List<JobMemory> Pool = new List<JobMemory>();
        public int frameNumber;

        public static JobMemory Rent(int frameCount, int frameNumber)
        {
            JobMemory jobMemory;
            if (Pool.Count > 0)
            {
                var index = Pool.Count - 1;
                jobMemory = Pool[index];
                Pool.RemoveAt(index);
            }
            else
            {
                jobMemory = new JobMemory(frameCount);
            }

            jobMemory.frameNumber = frameNumber;

            return jobMemory;
        }

        public static void Return(JobMemory jobMemory)
        {
            Pool.Add(jobMemory);
        }

        public static void DestroyPool()
        {
            for (var i = Pool.Count - 1; i >= 0; i--)
            {
                Pool[i].Dispose();
            }
        }

        private JobMemory(int frameCount)
        {
            _lumaBytesPtrs = new NativeArray<IntPtr>(frameCount, Allocator.Persistent);
            _uBytesPtrs = new NativeArray<IntPtr>(frameCount, Allocator.Persistent);
            _vBytesPtrs = new NativeArray<IntPtr>(frameCount, Allocator.Persistent);
            _rgbList = new NativeArray<byte>(1920 * 1080 * 3, Allocator.Persistent);
        }

        public void ScheduleJob(List<Av1Frame> frames)
        {
            _lumaBytesPtrs.CopyFrom(frames.Select(frame => frame.Picture._data[0]).ToArray());
            _uBytesPtrs.CopyFrom(frames.Select(frame => frame.Picture._data[1]).ToArray());
            _vBytesPtrs.CopyFrom(frames.Select(frame => frame.Picture._data[2]).ToArray());

            var job = new YuvToRgbJob
            {
                LumaBytesPtrs = _lumaBytesPtrs,
                UBytesPtrs = _uBytesPtrs,
                VBytesPtrs = _vBytesPtrs,
                ChunkHeight = ChunkHeight,
                RgbList = _rgbList,
            };

            jobHandle = job.Schedule(1080 / ChunkHeight, 1);
        }

        public void Dispose()
        {
            _lumaBytesPtrs.Dispose();
            _uBytesPtrs.Dispose();
            _vBytesPtrs.Dispose();
            _rgbList.Dispose();
            Pool.Remove(this);
        }
    }

    private JobMemory ScheduleYuvToRgbJob(List<Av1Frame> frames, int frameNumber)
    {
        var jobMemory = JobMemory.Rent(frames.Count, frameNumber);
        jobMemory.ScheduleJob(frames);

        return jobMemory;
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