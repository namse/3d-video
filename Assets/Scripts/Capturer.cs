using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.UI;

public class Capturer : MonoBehaviour
{
    public Camera camera1;
    public RenderTexture renderTexture1;
    public Camera camera2;
    public RenderTexture renderTexture2;
    public RenderTexture renderTextureResult;
    public RawImage rawImage;
    public Shader shader;
    public ComputeShader mergeShader;

    public const int ScreenWidth = 1920;

    void Start()
    {
        camera1.depthTextureMode = DepthTextureMode.Depth;
        camera2.depthTextureMode = DepthTextureMode.Depth;
    }
    //public void Capture()
    //{
    //    var currentRT = active;
    //    active = camera.targetTexture;
    //    camera.Render();
    //    var image = new Texture2D(camera.targetTexture.width, camera.targetTexture.height);
    //    image.ReadPixels(
    //        new Rect(0, 0, camera.targetTexture.width, camera.targetTexture.height),
    //        0, 0);
    //    image.Apply();
    //    active = currentRT;

    //    texture2D = image;
    //}

    private static List<Color> ToColors(RenderTexture renderTexture)
    {
        var colors = new List<Color>(1920 * 1080);

        var colorBufferPtr = renderTexture.colorBuffer.GetNativeRenderBufferPtr();

        Debug.Log(Marshal.ReadInt32(colorBufferPtr));

        const int width = ScreenWidth;

        for (var y = 0; y < 1080; y += 1)
        {
            for (var x = 0; x < width; x += 1)
            {
                var color32 = Marshal.ReadInt32(colorBufferPtr + x + y * width);
                var b = ((color32) & 0xFF) / 255f;
                var g = ((color32 >> 8) & 0xFF) / 255f;
                var r = ((color32 >> 16) & 0xFF) / 255f;
                var a = ((color32 >> 24) & 0xFF) / 255f;

                colors.Add(new Color(b, 0, 0, 1));
            }
        }

        return colors;
    }

    private static List<int> ToDepths(RenderTexture renderTexture)
    {
        var depths = new List<int>(1920 * 1080 / 2);

        Debug.Log(renderTexture.depth);

        var depthBufferPtr = renderTexture.depthBuffer.GetNativeRenderBufferPtr();

        const int width = ScreenWidth / 2;

        for (var y = 0; y < 1080; y += 1)
        {
            for (var x = 0; x < width; x += 1)
            {
                var twoDepth = Marshal.ReadInt32(depthBufferPtr + x + y * width);
                var depthOne = (twoDepth >> 16) & 0xFFFF;
                var depthTwo = (twoDepth) & 0xFFFF;

                depths.Add(depthOne);
                depths.Add(depthTwo);
            }
        }

        return depths;
    }

    private (Texture2D colorTexture, Texture2D depthTexture) GetTextures(Camera camera)
    {
        var colorTexture = new Texture2D(1920, 1080, GraphicsFormat.R8G8B8A8_UNorm, 1, TextureCreationFlags.None);
        var depthTexture = new Texture2D(1920, 1080, GraphicsFormat.R8G8B8A8_UNorm, 1, TextureCreationFlags.None);
        
        var rollbackRenderTexture = RenderTexture.active;
        RenderTexture.active = camera.targetTexture;

        camera.Render();
        colorTexture.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0);
        colorTexture.Apply();

        camera.RenderWithShader(shader, string.Empty);
        
        depthTexture.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0);
        depthTexture.Apply();


        RenderTexture.active = rollbackRenderTexture;

        return (colorTexture, depthTexture);
    }


    public void MergeRenderTexture()
    {
        var (colorTexture1, depthTexture1) = GetTextures(camera1);
        var (colorTexture2, depthTexture2) = GetTextures(camera2);
        var resultTexture = new Texture2D(1920, 1080, GraphicsFormat.R8G8B8A8_UNorm, 1, TextureCreationFlags.None);

        var kernel = mergeShader.FindKernel("Merge");
        mergeShader.SetTexture(kernel, "colorTexture1", colorTexture1);
        mergeShader.SetTexture(kernel, "colorTexture2", colorTexture2);
        mergeShader.SetTexture(kernel, "depthTexture1", depthTexture1);
        mergeShader.SetTexture(kernel, "depthTexture2", depthTexture2);

        renderTextureResult = new RenderTexture(1920, 1080, 1);
        renderTextureResult.enableRandomWrite = true;
        renderTextureResult.Create();
        mergeShader.SetTexture(kernel, "resultTexture", renderTextureResult);

        mergeShader.Dispatch(kernel, 1920 / 8, 1080 / 8, 1);

        //for (var y = 0; y < 1080; y += 1)
        //{
        //    for (var x = 0; x < ScreenWidth; x += 1)
        //    {
        //        var index = x + y * ScreenWidth;

        //        var color1 = colorTexture1.GetPixel(x, y);
        //        var color2 = colorTexture2.GetPixel(x, y);

        //        var depth1 = depthTexture1.GetPixel(x, y);
        //        var depth2 = depthTexture2.GetPixel(x, y);

        //        var color = depth1.r > depth2.r ? color1 : color2;
        //        // var color = depth1;
        //        resultTexture.SetPixel(x, y, color);
        //    }
        //}

        // resultTexture.Apply();

        rawImage.texture = renderTextureResult;
        // rawImage.texture = colorTexture1;
        // rawImage.texture = colorTexture2;
    }

    private void Good()
    {
        var rollbackRenderTexture = RenderTexture.active;
        RenderTexture.active = camera1.targetTexture;

        camera1.RenderWithShader(shader, string.Empty);
        
        var depthTexture1 = new Texture2D(1920, 1080, GraphicsFormat.R8G8B8A8_UNorm, 1, TextureCreationFlags.None);
        depthTexture1.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0);
        depthTexture1.Apply();

        rawImage.texture = depthTexture1;

        RenderTexture.active = rollbackRenderTexture;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.A))
        {
            MergeRenderTexture();
        }
        if (Input.GetKeyDown(KeyCode.S))
        {
            var (colorTexture1, _) = GetTextures(camera1);
            rawImage.texture = colorTexture1;
        }
        if (Input.GetKeyDown(KeyCode.D))
        {
            var (colorTexture2, _) = GetTextures(camera2);
            rawImage.texture = colorTexture2;
        }
    }
}
