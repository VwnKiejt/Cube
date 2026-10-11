using UnityEngine;

public sealed class ResultsShapeGraphic : UnityEngine.UI.Image
{
    public bool star;
    private static Sprite circleSprite;
    private static Sprite starSprite;

    public static Sprite GetIcon(bool isStar)
    {
        var cached = isStar ? starSprite : circleSprite;
        if (cached != null) return cached;
        const int size = 128;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Clamp;
        var vertices = new Vector2[10];
        for (int i = 0; i < 10; i++)
        {
            float angle = Mathf.PI * 0.5f + i * Mathf.PI * 2f / 10f;
            vertices[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * (i % 2 == 0 ? 1f : 0.43f);
        }
        var pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                Vector2 p = new Vector2((x + 0.5f) / size * 2f - 1f, (y + 0.5f) / size * 2f - 1f);
                bool inside = p.sqrMagnitude <= 1f;
                if (isStar)
                {
                    inside = false;
                    for (int i = 0, j = 9; i < 10; j = i++)
                    {
                        Vector2 a = vertices[i], b = vertices[j];
                        if ((a.y > p.y) != (b.y > p.y) && p.x < (b.x - a.x) * (p.y - a.y) / (b.y - a.y) + a.x)
                            inside = !inside;
                    }
                }
                pixels[y * size + x] = new Color32(255, 255, 255, inside ? (byte)255 : (byte)0);
            }
        texture.SetPixels32(pixels);
        texture.Apply(false, true);
        cached = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        if (isStar) starSprite = cached; else circleSprite = cached;
        return cached;
    }
}
