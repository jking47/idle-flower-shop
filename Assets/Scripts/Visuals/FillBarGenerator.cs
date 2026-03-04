using UnityEngine;

/// <summary>
/// Generates clean rounded fill bar sprites for progress indicators.
/// </summary>
public static class FillBarSpriteGenerator
{
    /// <summary>
    /// Creates a rounded bar sprite. Use as both the background and fill image.
    /// Set Image Type to Filled, Fill Method to Horizontal.
    /// </summary>
    public static Sprite Generate(int width, int height, Color color, int cornerRadius = 4)
    {
        var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        var px = new Color[width * height];
        for (int i = 0; i < px.Length; i++) px[i] = Color.clear;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                bool inside = true;
                int cr = cornerRadius;

                // Corner checks
                if (x < cr && y < cr)
                    inside = (x - cr) * (x - cr) + (y - cr) * (y - cr) <= cr * cr;
                else if (x >= width - cr && y < cr)
                    inside = (x - (width - cr - 1)) * (x - (width - cr - 1)) + (y - cr) * (y - cr) <= cr * cr;
                else if (x < cr && y >= height - cr)
                    inside = (x - cr) * (x - cr) + (y - (height - cr - 1)) * (y - (height - cr - 1)) <= cr * cr;
                else if (x >= width - cr && y >= height - cr)
                    inside = (x - (width - cr - 1)) * (x - (width - cr - 1)) + (y - (height - cr - 1)) * (y - (height - cr - 1)) <= cr * cr;

                if (inside)
                    px[y * width + x] = color;
            }
        }

        tex.SetPixels(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), height);
    }
}
