using System.Collections;
using System.Collections.Generic;
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
    private RenderTexture renderTextureResult;

    // Start is called before the first frame update
    void Start()
    {
        renderTextureResult = new RenderTexture(604, 538, 1);
        renderTextureResult.enableRandomWrite = true;
        renderTextureResult.Create();

        rawImage.texture = renderTextureResult;
    }

    // Update is called once per frame

    private void Merge()
    {
        rawImage.texture = null;
        for (var i = 0; i < 1000; i += 1)
        {
            var kernel = mergeShader.FindKernel("Merge");
            mergeShader.SetTexture(kernel, "TextureA", VideoPlayerA.texture);
            mergeShader.SetTexture(kernel, "TextureB", VideoPlayerB.texture);
            mergeShader.SetTexture(kernel, "TextureBackground", VideoPlayerBackground.texture);


            mergeShader.SetTexture(kernel, "resultTexture", renderTextureResult);

            mergeShader.Dispatch(kernel, 1920 / 8, 1080 / 8, 1);
        }
        rawImage.texture = renderTextureResult;
    }
    void Update()
    {
        if (VideoPlayerA.isPrepared
            && VideoPlayerB.isPrepared
            && VideoPlayerBackground.isPrepared)
        {
            Merge();
        }
    }
}
