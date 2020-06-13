using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Dav1dDotnet;
using Dav1dDotnet.Decoder;
using UnityEngine;
using UnityEngine.Rendering;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

public class Yuv2RgbComputeShader : MonoBehaviour, IDecoder
{
    private readonly Stopwatch _stopwatch = new Stopwatch();
    private IvfAv1Decoder[] _ivfAv1Decoders;
    private Queue<Texture2D> _decodedQueue;
    private Queue<Texture2D> _freeTextureQueue;
    private List<Texture2D> _rawTextures;
    public ComputeShader computeShader;

    private int _frameNumber;
    private int _kernel;
    
    private byte[] _lumaBytes = new byte[1920 * 1080];
    private byte[] _uBytes = new byte[1920 * 1080 / 4];
    private byte[] _vBytes = new byte[1920 * 1080 / 4];
    private List<GpuJob> _workingGpuJobs;

    void Start()
    {
        _ivfAv1Decoders = new IvfAv1Decoder[1];
        _decodedQueue = new Queue<Texture2D>();
        _freeTextureQueue = new Queue<Texture2D>();
        _rawTextures = new List<Texture2D>();

        _lumaBytes = new byte[1920 * 1080];
        _uBytes = new byte[1920 * 1080 / 4];
        _vBytes = new byte[1920 * 1080 / 4];
        _workingGpuJobs = new List<GpuJob>();

        var asset = Resources.Load("whiteAlpha");
        var textAsset = asset as TextAsset;

        for (var i = 0; i < _ivfAv1Decoders.Length; i += 1)
        {
            var stream = new MemoryStream(textAsset.bytes);
            _ivfAv1Decoders[i] = new IvfAv1Decoder(stream);
        }

        for (var i = 0; i < 20; i += 1)
        {
            var texture = new Texture2D(1920, 1080, TextureFormat.RGBA32, false);
            _freeTextureQueue.Enqueue(texture);
            _rawTextures.Add(texture);
        }
        _kernel = computeShader.FindKernel("Yuv2Rgb");
    }

    void OnDestroy()
    {
        _rawTextures.ForEach(Object.Destroy);
        foreach (var ivfAv1Decoder in _ivfAv1Decoders)
        {
            ivfAv1Decoder?.Dispose();
        }

        foreach (var gpuJob in _workingGpuJobs)
        {
            gpuJob.Dispose();
        }
        GpuJob.DestroyPool();
    }

    void Update()
    {
        DispatchAvailableFrames();
        ConvertDoneRequestToTexture2D();
    }

    private void ConvertDoneRequestToTexture2D()
    {
        var doneJobs = _workingGpuJobs.Where(job => job.request.done);
        foreach (var gpuJob in doneJobs)
        {
            if (gpuJob.request.hasError)
            {
                Debug.Log("Has Error");
                continue;
            }

            _stopwatch.Restart();
            var rgba = gpuJob.request.GetData<byte>();
            
            _stopwatch.Stop();
            Debug.Log($"GetData {_stopwatch.ElapsedMilliseconds}");
            _stopwatch.Restart();

            var texture = gpuJob.texture2D;

            texture.SetPixelData(rgba, 0);
            texture.Apply(false);
            
            _stopwatch.Stop();
            Debug.Log($"SetPixelData Apply {_stopwatch.ElapsedMilliseconds}");
            _stopwatch.Restart();

            _decodedQueue.Enqueue(texture);

            GpuJob.Return(gpuJob);
        }

        _workingGpuJobs = _workingGpuJobs.Where(job => !job.request.done).ToList();
    }

    private void DispatchAvailableFrames()
    {
        while (_freeTextureQueue.Count > 0
            && _ivfAv1Decoders.All(ivfAv1Decoder => ivfAv1Decoder.TryGetAv1Frame(_frameNumber, out _))
            && GpuJob.TryRent(out var gpuJob))
        {
            var frames = _ivfAv1Decoders.Select(ivfAv1Decoder =>
            {
                ivfAv1Decoder.TryGetAv1Frame(_frameNumber, out var av1Frame);
                return av1Frame;
            }).ToList();

            var texture = _freeTextureQueue.Dequeue();
            DispatchConvertFrame(frames[0], gpuJob, texture);

            _frameNumber += 1;
        }
    }

    private void DispatchConvertFrame(Av1Frame av1Frame, GpuJob gpuJob, Texture2D texture2D)
    {
        _stopwatch.Restart();
        Marshal.Copy(av1Frame.Picture._data[0], _lumaBytes, 0, 1920 * 1080);
        gpuJob.LumaBuffer.SetData(_lumaBytes);
        computeShader.SetBuffer(_kernel, "lumaBuffer", gpuJob.LumaBuffer);

        Marshal.Copy(av1Frame.Picture._data[1], _uBytes, 0, 1920 * 1080 / 4);
        gpuJob.UBuffer.SetData(_uBytes);
        computeShader.SetBuffer(_kernel, "uBuffer", gpuJob.UBuffer);

        Marshal.Copy(av1Frame.Picture._data[2], _vBytes, 0, 1920 * 1080 / 4);
        gpuJob.VBuffer.SetData(_vBytes);
        computeShader.SetBuffer(_kernel, "vBuffer", gpuJob.VBuffer);

        _stopwatch.Stop();
        Debug.Log($"Copy {_stopwatch.ElapsedMilliseconds}");
        _stopwatch.Restart();

        computeShader.SetBuffer(_kernel, "rgbaBuffer", gpuJob.RgbaBuffer);

        _stopwatch.Stop();
        Debug.Log($"Copy {_stopwatch.ElapsedMilliseconds}");
        _stopwatch.Restart();

        computeShader.Dispatch(_kernel, 1920 / 4 / 8, 1080 / 8, 1);

        _stopwatch.Stop();
        Debug.Log($"Dispatch {_stopwatch.ElapsedMilliseconds}");
        _stopwatch.Restart();

        gpuJob.request = AsyncGPUReadback.Request(gpuJob.RgbaBuffer);
        gpuJob.texture2D = texture2D;
        _workingGpuJobs.Add(gpuJob);
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

    public class GpuJob: IDisposable
    {
        public readonly ComputeBuffer LumaBuffer = new ComputeBuffer(1920 * 1080 / 16, 16);
        public readonly ComputeBuffer UBuffer = new ComputeBuffer(1920 * 1080 / 64, 16);
        public readonly ComputeBuffer VBuffer = new ComputeBuffer(1920 * 1080 / 64, 16);
        public readonly ComputeBuffer RgbaBuffer = new ComputeBuffer(1920 * 1080 / 4, 16);
        public AsyncGPUReadbackRequest request;
        public Texture2D texture2D;

        private static readonly Queue<GpuJob> Pool = new Queue<GpuJob>();

        public static bool TryRent(out GpuJob gpuJob)
        {
            gpuJob = Pool.Count > 0 ? Pool.Dequeue() : null;
            return !(gpuJob is null);
        }

        public static void Return(GpuJob gpuJob)
        {
            Pool.Enqueue(gpuJob);
        }

        public static void DestroyPool()
        {
            foreach (var gpuJob in Pool)
            {
                gpuJob.Dispose();
            }
        }

        static GpuJob()
        {
            for (var i = 0; i < 20; i +=1)
            {
                Pool.Enqueue(new GpuJob());
            }
        }

        private GpuJob()
        {

        }

        public void Dispose()
        {
            LumaBuffer?.Dispose();
            UBuffer?.Dispose();
            VBuffer?.Dispose();
            RgbaBuffer?.Dispose();
        }
    }
}
