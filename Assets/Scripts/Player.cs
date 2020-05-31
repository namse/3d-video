using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.UI;
using UnityEngine.Video;

public class Player : MonoBehaviour
{
    public RawImage rawImage;
    public VideoPlayer VideoPlayerA;
    public VideoPlayer VideoPlayerB;
    public VideoPlayer VideoPlayerBackground;
    public ComputeShader mergeShader;
    public TextAsset depthFile;

    private readonly Dictionary<int, int[]> _frameDepths = new Dictionary<int, int[]>();
    private RenderTexture _renderTextureResult;

    // Start is called before the first frame update
    void Start()
    {
        _renderTextureResult = new RenderTexture(700, 538, 1);
        _renderTextureResult.enableRandomWrite = true;
        _renderTextureResult.Create();

        rawImage.texture = _renderTextureResult;
        InitFrameDepths();
    }

    private void InitFrameDepths()
    {
        var lines = depthFile.text
            .Replace("\r\n", "\n")
            .Split('\n');

        foreach (var line in lines)
        {
            try
            {
                var chunks = line.Split(',');
                var frame = int.Parse(chunks[0]);
                var depths = chunks.Skip(1).Select(int.Parse).ToArray();

                _frameDepths[frame] = depths;
            }
            catch
            {
                // ignore
            }
        }
    }

    private int[] GetOrders(int frame)
    {
        return _frameDepths[frame]
            .Select((depth, index) => (depth, index))
            .OrderBy(tuple => tuple.depth)
            .Select(tuple => tuple.index)
            .ToArray();
    }

    // Update is called once per frame
    private void Merge()
    {
        var frame = VideoPlayerA.frame;
        if (frame < 0)
        {
            return;
        }

        var orders = GetOrders((int)frame);

        var kernel = mergeShader.FindKernel("Merge");
        mergeShader.SetTexture(kernel, "TextureA", VideoPlayerA.texture);
        mergeShader.SetTexture(kernel, "TextureB", VideoPlayerB.texture);
        mergeShader.SetInt("orderA", orders[0]);
        mergeShader.SetInt("orderB", orders[1]);

        mergeShader.SetTexture(kernel, "resultTexture", _renderTextureResult);

        mergeShader.Dispatch(kernel, 700 / 8, 538 / 8, 1);
    }
    void Update()
    {
        if (VideoPlayerA.isPrepared
            && VideoPlayerB.isPrepared)
        {
            Merge();
        }
    }
}
