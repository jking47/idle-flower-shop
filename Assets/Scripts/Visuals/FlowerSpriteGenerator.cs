using UnityEngine;

/// <summary>
/// Generates procedural flower sprites at runtime with growth stages.
/// All settings are tweakable via the public fields on FlowerSpriteSettings.
/// </summary>
public static class FlowerSpriteGenerator
{
    // --- Tweakable defaults (override via GenerateWithSettings) ---
    public static int SpriteSize = 128;
    public static int PetalCount = 5;
    public static int PetalRadius = 20;
    public static int PetalDistance = 18;
    public static int CenterRadius = 10;
    public static int StemWidth = 4;
    public static int LeafRx = 12;
    public static int LeafRy = 6;

    /// <summary>
    /// Growth stages for visual progression.
    /// </summary>
    public enum GrowthStage { Seed, Sprout, Budding, Bloomed }

    /// <summary>
    /// Generate a flower sprite at a specific growth stage.
    /// </summary>
    public static Sprite Generate(Color petalColor, Color centerColor, GrowthStage stage = GrowthStage.Bloomed)
    {
        int sz = SpriteSize;
        var tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        var px = new Color[sz * sz];
        for (int i = 0; i < px.Length; i++) px[i] = Color.clear;

        int cx = sz / 2;
        Color stem = new Color(0.3f, 0.6f, 0.2f);
        Color leaf = new Color(0.35f, 0.65f, 0.25f);
        Color soil = new Color(0.45f, 0.3f, 0.15f);

        switch (stage)
        {
            case GrowthStage.Seed:
                // Small mound of soil with a seed
                FillEllipse(px, sz, cx, 8, sz / 5, 6, soil);
                FillCircle(px, sz, cx, 12, 4, new Color(0.55f, 0.4f, 0.2f));
                break;

            case GrowthStage.Sprout:
                // Short stem with two tiny leaves
                int sproutH = sz / 3;
                FillRect(px, sz, cx - StemWidth / 2, 4, StemWidth, sproutH, stem);
                FillEllipse(px, sz, cx - 6, sproutH - 2, 6, 3, leaf);
                FillEllipse(px, sz, cx + 6, sproutH + 2, 6, 3, leaf);
                FillEllipse(px, sz, cx, 4, sz / 5, 4, soil);
                break;

            case GrowthStage.Budding:
                // Full stem, leaves, closed bud at top
                int budBase = 4;
                int budTop = sz * 2 / 3;
                FillRect(px, sz, cx - StemWidth / 2, budBase, StemWidth, budTop - budBase, stem);
                FillEllipse(px, sz, cx - LeafRx + 2, sz / 3, LeafRx, LeafRy, leaf);
                FillEllipse(px, sz, cx + LeafRx - 2, sz / 3 + 4, LeafRx, LeafRy, leaf);
                // Closed bud — smaller, darker petals bunched together
                Color budColor = Color.Lerp(petalColor, stem, 0.4f);
                FillEllipse(px, sz, cx, budTop + 6, PetalRadius / 2, PetalRadius * 2 / 3, budColor);
                FillEllipse(px, sz, cx, 4, sz / 5, 4, soil);
                break;

            case GrowthStage.Bloomed:
                // Full flower
                int bloomBase = 4;
                int stemTop = sz / 2 - 2;
                FillRect(px, sz, cx - StemWidth / 2, bloomBase, StemWidth, stemTop - bloomBase, stem);
                FillEllipse(px, sz, cx - LeafRx + 2, sz / 3 - 4, LeafRx, LeafRy, leaf);
                FillEllipse(px, sz, cx + LeafRx - 2, sz / 3, LeafRx, LeafRy, leaf);

                // Petals
                int flowerCy = stemTop + PetalDistance + 4;
                for (int i = 0; i < PetalCount; i++)
                {
                    float angle = i * Mathf.PI * 2f / PetalCount - Mathf.PI / 2f;
                    int ppx = cx + Mathf.RoundToInt(Mathf.Cos(angle) * PetalDistance);
                    int ppy = flowerCy + Mathf.RoundToInt(Mathf.Sin(angle) * PetalDistance);
                    FillCircle(px, sz, ppx, ppy, PetalRadius, petalColor);
                }
                FillCircle(px, sz, cx, flowerCy, CenterRadius, centerColor);
                FillEllipse(px, sz, cx, 4, sz / 5, 4, soil);
                break;
        }

        tex.SetPixels(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, sz, sz), new Vector2(0.5f, 0.5f), sz);
    }

    /// <summary>
    /// Returns a default color palette for a flower index.
    /// </summary>
    public static (Color petal, Color center) GetPalette(int index)
    {
        return index switch
        {
            0 => (new Color(1f, 0.4f, 0.4f), new Color(1f, 0.9f, 0.3f)),       // Red
            1 => (new Color(0.4f, 0.6f, 1f), new Color(1f, 1f, 0.5f)),          // Blue
            2 => (new Color(1f, 0.6f, 0.9f), new Color(1f, 0.85f, 0.4f)),       // Pink
            3 => (new Color(1f, 0.8f, 0.2f), new Color(0.6f, 0.3f, 0.1f)),      // Yellow
            4 => (new Color(0.7f, 0.4f, 1f), new Color(1f, 1f, 0.6f)),          // Purple
            5 => (new Color(1f, 0.5f, 0.2f), new Color(0.9f, 0.9f, 0.3f)),      // Orange
            _ => (new Color(0.9f, 0.9f, 0.9f), new Color(1f, 0.9f, 0.4f)),      // White
        };
    }

    // --- Drawing helpers ---

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

    static void FillEllipse(Color[] px, int sz, int cx, int cy, int rx, int ry, Color col)
    {
        for (int y = cy - ry; y <= cy + ry; y++)
            for (int x = cx - rx; x <= cx + rx; x++)
            {
                if (x < 0 || x >= sz || y < 0 || y >= sz) continue;
                float dx = (float)(x - cx) / rx;
                float dy = (float)(y - cy) / ry;
                if (dx * dx + dy * dy <= 1f)
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
}