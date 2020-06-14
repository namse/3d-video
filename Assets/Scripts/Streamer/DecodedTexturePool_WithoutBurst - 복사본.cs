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


public class DecodedTexturePool_WithoutBurst : MonoBehaviour, IDecoder
{
    private readonly Stopwatch _stopwatch = new Stopwatch();
    private IvfAv1Decoder[] _ivfAv1Decoders;
    private Queue<Texture2D> _decodedQueue;
    private Queue<Texture2D> _freeTextureQueue;
    private List<Texture2D> _rawTextures;

    private int _frameNumber;

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

    private void ConvertFrameToTexture()
    {
        while (_freeTextureQueue.Count > 0
            && _ivfAv1Decoders.All(ivfAv1Decoder => ivfAv1Decoder.TryGetAv1Frame(_frameNumber, out _)))
        {
            _stopwatch.Restart();

            var frames = _ivfAv1Decoders.Select(ivfAv1Decoder =>
            {
                ivfAv1Decoder.TryGetAv1Frame(_frameNumber, out var av1Frame);
                return av1Frame;
            }).ToList();

            var texture = _freeTextureQueue.Dequeue();

            texture.SetPixelData(frames[0].Rgb24ByteMemoryOwner.Memory.Buffer, 0);
            texture.Apply();
            _decodedQueue.Enqueue(texture);
            foreach (var ivfAv1Decoder in _ivfAv1Decoders)
            {
                ivfAv1Decoder.CheckConsumedFrameNumber(_frameNumber);
            }

            _frameNumber += 1;

            _stopwatch.Stop();
            Debug.Log($"ConvertFrameToTexture 1 Cycle {_stopwatch.ElapsedMilliseconds}");
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