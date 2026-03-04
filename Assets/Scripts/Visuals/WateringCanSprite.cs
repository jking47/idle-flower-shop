using UnityEngine;

/// <summary>
/// Generates a simple procedural watering can sprite.
/// All coordinates scale relative to size for resolution independence.
/// </summary>
public static class WateringCanSpriteGenerator
{
    public static Sprite Generate(int size = 128)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        var px = new Color[size * size];
        for (int i = 0; i < px.Length; i++) px[i] = Color.clear;

        Color body = new Color(0.45f, 0.55f, 0.7f);
        Color bodyDark = new Color(0.35f, 0.45f, 0.6f);
        Color handle = new Color(0.55f, 0.65f, 0.75f);
        Color spout = new Color(0.4f, 0.5f, 0.65f);
        Color water = new Color(0.3f, 0.6f, 0.9f, 0.8f);

        float s = size / 64f;
        int cx = size / 2;

        // Main body — rounded rectangle
        FillRoundRect(px, size, cx - S(14, s), S(8, s), S(28, s), S(22, s), S(4, s), body);
        // Darker bottom band
        FillRect(px, size, cx - S(14, s), S(8, s), S(28, s), S(6, s), bodyDark);

        // Handle — arc on top
        FillRect(px, size, cx - S(2, s), S(30, s), S(4, s), S(14, s), handle);
        FillRect(px, size, cx - S(10, s), S(40, s), S(20, s), S(4, s), handle);
        FillRect(px, size, cx - S(10, s), S(30, s), S(4, s), S(10, s), handle);

        // Spout — angled going up-right
        int spoutSteps = S(16, s);
        for (int i = 0; i < spoutSteps; i++)
        {
            int sx = cx + S(14, s) + i / 2;
            int sy = S(18, s) + i;
            FillRect(px, size, sx, sy, S(3, s), S(2, s), spout);
        }

        // Spout tip — wider opening
        FillRect(px, size, cx + S(21, s), S(33, s), S(6, s), S(3, s), spout);

        // Water drops from spout
        FillCircle(px, size, cx + S(24, s), S(30, s), S(2, s), water);
        FillCircle(px, size, cx + S(22, s), S(26, s), S(1, s), water);
        FillCircle(px, size, cx + S(26, s), S(24, s), S(1, s), water);

        // Rose (sprinkler head) dots
        for (int dy = 0; dy < 3; dy++)
        {
            for (int dx = 0; dx < 3; dx++)
            {
                int dotX = cx + S(21, s) + dx * S(2, s);
                int dotY = S(34, s) + dy * S(1, s);
                if (dotX < size && dotY < size)
                    px[dotY * size + dotX] = bodyDark;
            }
        }

        tex.SetPixels(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }

    static int S(int val, float scale) => Mathf.RoundToInt(val * scale);

    static void FillCircle(Color[] px, int sz, int cx, int cy, int r, Color col)
    {
        int rSq = r * r;
        for (int y = cy - r; y <= cy + r; y++)
            for (int x = cx - r; x <= cx + r; x++)
            {
                if (x < 0 || x >= sz || y < 0 || y >= sz) continue;
                if ((x - cx) * (x - cx) + (y - cy) * (y - cy) <= rSq)
                    px[y * sz + x] = col;
            }
    }

    static void FillRect(Color[] px, int sz, int x0, int y0, int w, int h, Color col)
    {
        for (int y = y0; y < y0 + h; y++)
            for (int x = x0; x < x0 + w; x++)
            {
                if (x < 0 || x >= sz || y < 0 || y >= sz) continue;
                px[y * sz + x] = col;
            }
    }

    static void FillRoundRect(Color[] px, int sz, int x0, int y0, int w, int h, int r, Color col)
    {
        for (int y = y0; y < y0 + h; y++)
            for (int x = x0; x < x0 + w; x++)
            {
                if (x < 0 || x >= sz || y < 0 || y >= sz) continue;

                bool inCorner = false;
                int cornerX = 0, cornerY = 0;

                if (x < x0 + r && y < y0 + r) { cornerX = x0 + r; cornerY = y0 + r; inCorner = true; }
                else if (x >= x0 + w - r && y < y0 + r) { cornerX = x0 + w - r; cornerY = y0 + r; inCorner = true; }
                else if (x < x0 + r && y >= y0 + h - r) { cornerX = x0 + r; cornerY = y0 + h - r; inCorner = true; }
                else if (x >= x0 + w - r && y >= y0 + h - r) { cornerX = x0 + w - r; cornerY = y0 + h - r; inCorner = true; }

                if (inCorner)
                {
                    if ((x - cornerX) * (x - cornerX) + (y - cornerY) * (y - cornerY) <= r * r)
                        px[y * sz + x] = col;
                }
                else
                {
                    px[y * sz + x] = col;
                }
            }
    }
}