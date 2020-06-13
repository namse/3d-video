using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
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
    private ComputeBuffer _lumaBuffer;
    private ComputeBuffer _uBuffer;
    private ComputeBuffer _vBuffer;
    private ComputeBuffer _rgbaBuffer;
    private readonly byte[] _rgbaTempBuffer = new byte[1920 * 1080 * 4];
    private readonly byte[] _lumaBytes = new byte[1920 * 1080];
    private readonly byte[] _uBytes = new byte[1920 * 1080 / 4];
    private readonly byte[] _vBytes = new byte[1920 * 1080 / 4];

    void Start()
    {
        _ivfAv1Decoders = new IvfAv1Decoder[1];
        _decodedQueue = new Queue<Texture2D>();
        _freeTextureQueue = new Queue<Texture2D>();
        _rawTextures = new List<Texture2D>();

        var asset = Resources.Load("whiteAlpha");
        var textAsset = asset as TextAsset;

        for (var i = 0; i < _ivfAv1Decoders.Length; i += 1)
        {
            var stream = new MemoryStream(textAsset.bytes);
            _ivfAv1Decoders[i] = new IvfAv1Decoder(stream);
        }

        for (var i = 0; i < 3; i += 1)
        {
            var texture = new Texture2D(1920, 1080, TextureFormat.RGBA32, false);
            _freeTextureQueue.Enqueue(texture);
            _rawTextures.Add(texture);
        }
        _kernel = computeShader.FindKernel("Yuv2Rgb");
        _lumaBuffer = new ComputeBuffer(1920 * 1080 / 16, 16);
        _uBuffer = new ComputeBuffer(1920 * 1080 / 64, 16);
        _vBuffer = new ComputeBuffer(1920 * 1080 / 64, 16);
        _rgbaBuffer = new ComputeBuffer(1920 * 1080 / 4, 16);
    }

    void Update()
    {
        ConvertAvailableFrames();
    }

    void OnDestroy()
    {
        _rawTextures.ForEach(Object.Destroy);
        foreach (var ivfAv1Decoder in _ivfAv1Decoders)
        {
            ivfAv1Decoder?.Dispose();
        }
        _lumaBuffer?.Dispose();
        _uBuffer?.Dispose();
        _vBuffer?.Dispose();
        _rgbaBuffer?.Dispose();
    }

    private void ConvertAv1FrameToTexture2D(Av1Frame av1Frame, Texture2D texture)
    {
        _stopwatch.Restart();
        Marshal.Copy(av1Frame.Picture._data[0], _lumaBytes, 0, 1920 * 1080);
        _lumaBuffer.SetData(_lumaBytes);
        computeShader.SetBuffer(_kernel, "lumaBuffer", _lumaBuffer);

        Marshal.Copy(av1Frame.Picture._data[1], _uBytes, 0, 1920 * 1080 / 4);
        _uBuffer.SetData(_uBytes);
        computeShader.SetBuffer(_kernel, "uBuffer", _uBuffer);

        Marshal.Copy(av1Frame.Picture._data[2], _vBytes, 0, 1920 * 1080 / 4);
        _vBuffer.SetData(_vBytes);
        computeShader.SetBuffer(_kernel, "vBuffer", _vBuffer);

        _stopwatch.Stop();
        Debug.Log($"Copy {_stopwatch.ElapsedMilliseconds}");
        _stopwatch.Restart();

        computeShader.SetBuffer(_kernel, "rgbaBuffer", _rgbaBuffer);

        _stopwatch.Stop();
        Debug.Log($"Copy {_stopwatch.ElapsedMilliseconds}");
        _stopwatch.Restart();

        computeShader.Dispatch(_kernel, 1920 / 4 / 8, 1080 / 8, 1);

        _stopwatch.Stop();
        Debug.Log($"Dispatch {_stopwatch.ElapsedMilliseconds}");
        _stopwatch.Restart();

        _rgbaBuffer.GetData(_rgbaTempBuffer);
        
        _stopwatch.Stop();
        Debug.Log($"GetData {_stopwatch.ElapsedMilliseconds}");
        _stopwatch.Restart();
        
        texture.SetPixelData(_rgbaTempBuffer, 0);
        texture.Apply(false);

        _stopwatch.Stop();
        Debug.Log($"Set Pixel{_stopwatch.ElapsedMilliseconds}");
    }

    private void ConvertAvailableFrames()
    {
        while (_freeTextureQueue.Count > 0
            && _ivfAv1Decoders.All(ivfAv1Decoder => ivfAv1Decoder.TryGetAv1Frame(_frameNumber, out _)))
        {

            var frames = _ivfAv1Decoders.Select(ivfAv1Decoder =>
            {
                ivfAv1Decoder.TryGetAv1Frame(_frameNumber, out var av1Frame);
                return av1Frame;
            }).ToList();
            var texture = _freeTextureQueue.Dequeue();

            ConvertAv1FrameToTexture2D(frames[0], texture);
            _decodedQueue.Enqueue(texture);

            _frameNumber += 1;

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
}
