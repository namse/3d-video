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
    private IDecoder _decoder;

    void Start()
    {
        _decoder = decodedTexturePool ?? (IDecoder)yuv2RgbComputeShader;
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
        
        if (_decoder.TryGetNextTexture(out var texture))
        {
            rawImage.texture = texture;
            _frameNumber += 1;

            if (!(_lastTexture is null))
            {
                _decoder.ReturnTexture(_lastTexture);
            }

            _lastTexture = texture;
        }
        else
        {
            Debug.Log($"cannot get texture {_frameNumber}");
        }
    }
}
