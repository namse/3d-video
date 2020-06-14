using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Resources;
using Dav1dDotnet;
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
    public DecodedTexturePool_WithoutBurst decodedTexturePoolWithoutBurst;
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

        var decoder = decodedTexturePoolWithoutBurst;

        if (decoder.TryGetNextTexture(out var texture))
        {
            rawImage.texture = texture;
            _frameNumber += 1;

            if (!(_lastTexture is null))
            {
                decoder.ReturnTexture(_lastTexture);
            }

            _lastTexture = texture;
        }
        else
        {
            Debug.Log($"cannot get texture {_frameNumber}");
        }
    }
}
