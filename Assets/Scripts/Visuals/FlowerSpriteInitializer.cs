using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Generates all procedural sprites at startup: flowers, watering can, fill bars.
/// Attach to GameManager.
/// </summary>
public class FlowerSpriteInitializer : MonoBehaviour
{
    [Header("Fill Bar Colors")]
    [SerializeField] Color growthFillColor = new Color(0.3f, 0.75f, 0.35f);
    [SerializeField] Color growthBgColor = new Color(0.15f, 0.2f, 0.15f, 0.8f);
    [SerializeField] Color waterFillColor = new Color(0.3f, 0.6f, 0.9f);
    [SerializeField] Color waterBgColor = new Color(0.15f, 0.15f, 0.25f, 0.8f);

    // flowerName → stage → sprite
    static Dictionary<string, Dictionary<FlowerSpriteGenerator.GrowthStage, Sprite>> sprites = new();

    // Cached fill bar sprites (still generated for anything that references them)
    static Sprite growthFill, growthBg, waterFill, waterBg;

    void Awake()
    {
        Services.Register(this);
    }

    void Start()
    {
        GenerateFlowerSprites();
        GenerateWateringCanSprite();
        GenerateFillBarSprites();
        ApplyFillBarColors();
        EventBus.Publish(new SpritesInitializedEvent());
    }

    void GenerateFlowerSprites()
    {
        if (!Services.TryGet<GardenManager>(out var garden)) return;

        // Destroy old GPU-backed sprites before regenerating to prevent texture leaks on reload
        foreach (var stageDict in sprites.Values)
            foreach (var sprite in stageDict.Values)
                if (sprite != null) Destroy(sprite);
        sprites.Clear();

        foreach (var s in new[] { growthFill, growthBg, waterFill, waterBg })
            if (s != null) Destroy(s);
        growthFill = growthBg = waterFill = waterBg = null;

        FlowerSpriteGenerator.SpriteSize = 128;
        FlowerSpriteGenerator.PetalCount = 5;
        FlowerSpriteGenerator.PetalRadius = 20;
        FlowerSpriteGenerator.PetalDistance = 18;
        FlowerSpriteGenerator.CenterRadius = 10;
        FlowerSpriteGenerator.StemWidth = 4;
        FlowerSpriteGenerator.LeafRx = 12;
        FlowerSpriteGenerator.LeafRy = 6;

        var flowers = garden.AvailableFlowers;
        for (int i = 0; i < flowers.Count; i++)
        {
            var (petal, center) = FlowerSpriteGenerator.GetPalette(i);
            var stages = new Dictionary<FlowerSpriteGenerator.GrowthStage, Sprite>();

            foreach (FlowerSpriteGenerator.GrowthStage stage in
                System.Enum.GetValues(typeof(FlowerSpriteGenerator.GrowthStage)))
            {
                stages[stage] = FlowerSpriteGenerator.Generate(petal, center, stage);
            }

            flowers[i].icon = stages[FlowerSpriteGenerator.GrowthStage.Bloomed];
            sprites[flowers[i].name] = stages;
        }

        Debug.Log($"[FlowerSpriteInit] Generated {flowers.Count} flowers x 4 stages");
    }

    void GenerateWateringCanSprite()
    {
        if (!Services.TryGet<WateringCan>(out var can)) return;

        var canSprite = WateringCanSpriteGenerator.Generate(128);

        var canIcon = can.GetComponentInChildren<Image>(true);
        if (canIcon != null)
            canIcon.sprite = canSprite;
    }

    void GenerateFillBarSprites()
    {
        growthFill = FillBarSpriteGenerator.Generate(64, 12, growthFillColor, 3);
        growthBg = FillBarSpriteGenerator.Generate(64, 12, growthBgColor, 3);
        waterFill = FillBarSpriteGenerator.Generate(64, 12, waterFillColor, 3);
        waterBg = FillBarSpriteGenerator.Generate(64, 12, waterBgColor, 3);
    }

    /// <summary>
    /// Color-only approach: tints fill bar Images directly without swapping sprites
    /// or touching parent Images. This prevents stomping on FlowerBedTint backgrounds.
    /// </summary>
    void ApplyFillBarColors()
    {
        if (!Services.TryGet<GardenManager>(out var garden)) return;

        foreach (var plot in garden.Plots)
        {
            var fills = plot.GetComponentsInChildren<Image>(true);
            foreach (var img in fills)
            {
                // Only touch Filled-type images that aren't the plot's own background
                if (img.type == Image.Type.Filled && img.gameObject != plot.gameObject)
                {
                    img.color = growthFillColor;
                }
            }
        }

        // Watering can fill bar
        if (Services.TryGet<WateringCan>(out var can))
        {
            var fills = can.GetComponentsInChildren<Image>(true);
            foreach (var img in fills)
            {
                if (img.type == Image.Type.Filled)
                {
                    img.color = waterFillColor;
                }
            }
        }
    }

    /// <summary>
    /// Get the sprite for a specific flower at a specific growth stage.
    /// </summary>
    public static Sprite GetStageSprite(string flowerName, float growthProgress)
    {
        if (!sprites.TryGetValue(flowerName, out var stages)) return null;

        var stage = growthProgress switch
        {
            < 0.01f => FlowerSpriteGenerator.GrowthStage.Seed,
            < 0.33f => FlowerSpriteGenerator.GrowthStage.Sprout,
            < 0.8f  => FlowerSpriteGenerator.GrowthStage.Budding,
            _       => FlowerSpriteGenerator.GrowthStage.Bloomed
        };

        return stages.TryGetValue(stage, out var sprite) ? sprite : null;
    }

    public static Sprite GetGrowthFillSprite() => growthFill;
    public static Sprite GetGrowthBgSprite() => growthBg;
    public static Sprite GetWaterFillSprite() => waterFill;
    public static Sprite GetWaterBgSprite() => waterBg;
}