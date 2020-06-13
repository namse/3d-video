using UnityEngine;

public interface IDecoder
{
    bool TryGetNextTexture(out Texture2D texture);
    void ReturnTexture(Texture2D texture);
    int AvailableTextureCount { get; }
}