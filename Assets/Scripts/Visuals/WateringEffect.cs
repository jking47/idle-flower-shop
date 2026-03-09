using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Animated water droplet effect for flower beds.
/// Spawns teardrop shapes that fall and fade.
/// Attach to the WateringEffect child of each FlowerBed.
///
/// Uses an object pool (size = dropCount) — no per-frame allocations.
/// </summary>
public class WateringEffect : MonoBehaviour
{
    [SerializeField] int dropCount = 8;
    [SerializeField] float spawnInterval = 0.12f;
    [SerializeField] float dropSpeed = 120f;
    [SerializeField] float dropLifetime = 0.7f;
    [SerializeField] float spreadX = 80f;

    // Pool
    readonly List<RectTransform> pool = new();
    readonly List<Image>         poolImgs = new();
    readonly List<bool>          inUse = new();

    // Active drop tracking
    readonly List<int>   activeIndices = new();
    readonly List<float> dropTimers    = new();

    float spawnTimer;
    Sprite dropSprite;

    void Awake()
    {
        dropSprite = CreateDropSprite();
        BuildPool();
    }

    void BuildPool()
    {
        for (int i = 0; i < dropCount; i++)
        {
            var go = new GameObject("Drop");
            var rt = go.AddComponent<RectTransform>();
            rt.SetParent(transform, false);
            rt.sizeDelta = new Vector2(12, 16);

            var img = go.AddComponent<Image>();
            img.sprite = dropSprite;
            img.color = new Color(0.4f, 0.7f, 1f, 0.9f);
            img.raycastTarget = false;

            go.SetActive(false);
            pool.Add(rt);
            poolImgs.Add(img);
            inUse.Add(false);
        }
    }

    void OnEnable()
    {
        spawnTimer = 0f;
    }

    void OnDisable()
    {
        // Return all active drops to the pool
        for (int i = activeIndices.Count - 1; i >= 0; i--)
        {
            int idx = activeIndices[i];
            pool[idx].gameObject.SetActive(false);
            inUse[idx] = false;
        }
        activeIndices.Clear();
        dropTimers.Clear();
    }

    void Update()
    {
        spawnTimer += Time.deltaTime;
        if (spawnTimer >= spawnInterval && activeIndices.Count < dropCount)
        {
            spawnTimer = 0f;
            SpawnDrop();
        }

        for (int i = activeIndices.Count - 1; i >= 0; i--)
        {
            int idx = activeIndices[i];

            dropTimers[i] += Time.deltaTime;
            float t = dropTimers[i] / dropLifetime;

            if (t >= 1f)
            {
                // Return to pool
                pool[idx].gameObject.SetActive(false);
                inUse[idx] = false;
                activeIndices.RemoveAt(i);
                dropTimers.RemoveAt(i);
                continue;
            }

            var pos = pool[idx].anchoredPosition;
            pos.y -= dropSpeed * Time.deltaTime;
            pool[idx].anchoredPosition = pos;

            var c = poolImgs[idx].color;
            c.a = 1f - t;
            poolImgs[idx].color = c;
        }
    }

    void SpawnDrop()
    {
        int freeIdx = -1;
        for (int i = 0; i < pool.Count; i++)
        {
            if (!inUse[i]) { freeIdx = i; break; }
        }
        if (freeIdx < 0) return;

        pool[freeIdx].anchoredPosition = new Vector2(
            Random.Range(-spreadX / 2f, spreadX / 2f),
            Random.Range(15f, 50f)
        );

        var c = poolImgs[freeIdx].color;
        c.a = 0.9f;
        poolImgs[freeIdx].color = c;

        pool[freeIdx].gameObject.SetActive(true);
        inUse[freeIdx] = true;
        activeIndices.Add(freeIdx);
        dropTimers.Add(0f);
    }

    Sprite CreateDropSprite()
    {
        int sz = 16;
        var tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        var px = new Color[sz * sz];
        for (int i = 0; i < px.Length; i++) px[i] = Color.clear;

        Color drop = Color.white;
        // Bottom circle — larger
        for (int y = 0; y < 12; y++)
            for (int x = 0; x < sz; x++)
            {
                float dx = (x - 7.5f) / 6f;
                float dy = (y - 5f) / 5f;
                if (dx * dx + dy * dy <= 1f)
                    px[y * sz + x] = drop;
            }
        // Point at top
        for (int row = 10; row < 15; row++)
        {
            int halfW = Mathf.Max(1, (15 - row));
            for (int x = 8 - halfW; x <= 8 + halfW; x++)
            {
                if (x >= 0 && x < sz && row < sz)
                    px[row * sz + x] = drop;
            }
        }

        tex.SetPixels(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, sz, sz), new Vector2(0.5f, 0.5f), sz);
    }
}
