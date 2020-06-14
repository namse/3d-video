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
using UnityEngine.UI;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;


public class FrameStreamerTester_withoutPool : MonoBehaviour
{
    private int _frameNumber;
    private readonly Stopwatch _stopwatch = new Stopwatch();
    private readonly Stopwatch _singleUpdateStopwatch = new Stopwatch();
    public RawImage rawImage;
    private IvfAv1Decoder[] _ivfAv1Decoders;
    public Text text;

    void Start()
    {
        Application.targetFrameRate = 60;
        _ivfAv1Decoders = new IvfAv1Decoder[1];

        var asset = Resources.Load("whiteAlpha");
        var textAsset = asset as TextAsset;

        for (var i = 0; i < _ivfAv1Decoders.Length; i += 1)
        {
            var stream = new MemoryStream(textAsset.bytes);
            _ivfAv1Decoders[i] = new IvfAv1Decoder(stream);
        }

        rawImage.texture = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
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

        if (_ivfAv1Decoders.All(decoder => decoder.TryGetAv1Frame(_frameNumber, out var frame)))
        {
            _singleUpdateStopwatch.Restart();
            
            _ivfAv1Decoders[0].TryGetAv1Frame(_frameNumber, out var frame);

            _singleUpdateStopwatch.Stop();
            Debug.Log($"TryGetAv1Frame {_singleUpdateStopwatch.ElapsedMilliseconds}");
            _singleUpdateStopwatch.Restart();

            var texture = rawImage.texture as Texture2D;
            
            texture.SetPixelData(frame.Rgb24ByteMemoryOwner.Memory.Buffer, 0);

            _singleUpdateStopwatch.Stop();
            Debug.Log($"SetPixelData {_singleUpdateStopwatch.ElapsedMilliseconds}");
            _singleUpdateStopwatch.Restart();


            texture.Apply(false);

            _singleUpdateStopwatch.Stop();
            Debug.Log($"Apply {_singleUpdateStopwatch.ElapsedMilliseconds}");
            _singleUpdateStopwatch.Restart();


            foreach (var ivfAv1Decoder in _ivfAv1Decoders)
            {
                ivfAv1Decoder.CheckConsumedFrameNumber(_frameNumber);
            }

            _singleUpdateStopwatch.Stop();
            Debug.Log($"CheckConsumedFrameNumber {_singleUpdateStopwatch.ElapsedMilliseconds}");
            _singleUpdateStopwatch.Restart();


            _frameNumber += 1;
        }
        else
        {
            Debug.Log($"cannot get texture {_frameNumber}");
        }
    }

    void OnDestroy()
    {
    }
}