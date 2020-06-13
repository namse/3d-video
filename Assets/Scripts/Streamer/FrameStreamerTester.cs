using System.Diagnostics;
using System.Resources;
using UnityEngine;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;

public class FrameStreamerTester : MonoBehaviour
{
    private int _frameNumber;
    private readonly Stopwatch _stopwatch = new Stopwatch();
    public RawImage rawImage;
    public Text text;
    public DecodedTexturePool decodedTexturePool;
    public Yuv2RgbComputeShader yuv2RgbComputeShader;
    private Texture2D _lastTexture;

    void Start()
    {
        Application.targetFrameRate = 60;
    }

    void OnDestroy()
    {
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

        var decoder = _frameNumber % 2 == 0 ? decodedTexturePool : (IDecoder) yuv2RgbComputeShader;

        if (decoder.TryGetNextTexture(out var texture))
        {
            rawImage.texture = texture;
            _frameNumber += 1;

            if (!(_lastTexture is null))
            {
                var returnDecoder = _frameNumber % 2 == 0 ? decodedTexturePool : (IDecoder)yuv2RgbComputeShader;
                returnDecoder.ReturnTexture(_lastTexture);
            }

            _lastTexture = texture;
        }
        else
        {
            Debug.Log($"cannot get texture {_frameNumber}");
        }
    }
}
