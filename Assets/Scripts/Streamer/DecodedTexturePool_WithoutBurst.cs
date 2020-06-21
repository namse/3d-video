using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Amazon.S3.Model;
using Dav1dDotnet;
using Dav1dDotnet.Decoder;
using StencilFrameLibrary;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;


public class DecodedTexturePool_WithoutBurst : MonoBehaviour, IDecoder
{
    private readonly Stopwatch _stopwatch = new Stopwatch();
    private Queue<Texture2D> _decodedQueue;
    private Queue<Texture2D> _freeTextureQueue;
    private List<Texture2D> _rawTextures;
    private IvfAv1Decoder _mainIvfAv1Decoder;
    private IvfAv1Decoder _alphaIvfAv1Decoder;
    private byte[] maskTreeBytes;

    private IEnumerable<IvfAv1Decoder> AllDecoders
    {
        get
        {
            yield return _mainIvfAv1Decoder;
            yield return _alphaIvfAv1Decoder;
        }
    }

    private int _frameNumber;

    void Start()
    {
        _decodedQueue = new Queue<Texture2D>();
        _freeTextureQueue = new Queue<Texture2D>();
        _rawTextures = new List<Texture2D>();

        _mainIvfAv1Decoder = LoadDecoder("frames_ivf");
        _alphaIvfAv1Decoder = LoadDecoder("frames_alpha_ivf");
        maskTreeBytes = LoadBytes("masktree");

        for (var i = 0; i < 1; i += 1)
        {
            var texture = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
            _freeTextureQueue.Enqueue(texture);
            _rawTextures.Add(texture);
        }
    }

    void Update()
    {
        ConvertFrameToTexture();
    }

    private IvfAv1Decoder LoadDecoder(string resourceName)
    {
        var bytes = LoadBytes(resourceName);
        var stream = new MemoryStream(bytes);
        return new IvfAv1Decoder(stream);
    }

    private byte[] LoadBytes(string resourceName)
    {
        var asset = Resources.Load(resourceName);
        var textAsset = asset as TextAsset;

        return textAsset.bytes;
    }

    private void ConvertFrameToTexture()
    {
        while (_freeTextureQueue.Count > 0
            && AllDecoders.All(ivfAv1Decoder => ivfAv1Decoder.TryGetAv1Frame(_frameNumber, out _)))
        {
            _stopwatch.Restart();

            _mainIvfAv1Decoder.TryGetAv1Frame(_frameNumber, out var mainAv1Frame);
            _alphaIvfAv1Decoder.TryGetAv1Frame(_frameNumber, out var alphaAv1Frame);
            var maskTree = maskTreeBytes[_frameNumber];

            var stencilFrame = new StencilFrame(
                mainAv1Frame.Picture._data[0],
                mainAv1Frame.Picture._data[1],
                mainAv1Frame.Picture._data[2],
                alphaAv1Frame.Picture._data[0],
                maskTree,
                mainAv1Frame.Picture._p.w,
                mainAv1Frame.Picture._p.h);

            var yuvFrame = StencilPainter.PaintYuvFrame(stencilFrame.Width, stencilFrame.Height, new[] { stencilFrame });

            다비드에서 libyuv 빼셈. 
                // 다비스에서는 yuv까지만.
                // 유니티에서 yuv 합성하고
                // libyuv로 합성한 걸 rgb로 변경.

            yuvFrame.
            //var texture = _freeTextureQueue.Dequeue();

            //texture.SetPixelData(frames[0].Rgb24ByteMemoryOwner.Memory.Buffer, 0);
            //texture.Apply();
            //_decodedQueue.Enqueue(texture);
            //foreach (var ivfAv1Decoder in _ivfAv1Decoders)
            //{
            //    ivfAv1Decoder.CheckConsumedFrameNumber(_frameNumber);
            //}

            //_frameNumber += 1;

            //_stopwatch.Stop();
            //Debug.Log($"ConvertFrameToTexture 1 Cycle {_stopwatch.ElapsedMilliseconds}");
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
    }
}