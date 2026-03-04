using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Animated water droplet effect for flower beds.
/// Spawns teardrop shapes that fall and fade.
/// Attach to the WateringEffect child of each FlowerBed.
/// </summary>
public class WateringEffect : MonoBehaviour
{
    [SerializeField] int dropCount = 8;
    [SerializeField] float spawnInterval = 0.12f;
    [SerializeField] float dropSpeed = 120f;
    [SerializeField] float dropLifetime = 0.7f;
    [SerializeField] float spreadX = 80f;

    readonly List<RectTransform> drops = new();
    readonly List<float> dropTimers = new();
    float spawnTimer;
    Sprite dropSprite;

    void Awake()
    {
        dropSprite = CreateDropSprite();
    }

    void OnEnable()
    {
        spawnTimer = 0f;
    }

    void OnDisable()
    {
        foreach (var d in drops)
        {
            if (d != null) Destroy(d.gameObject);
        }
        drops.Clear();
        dropTimers.Clear();
    }

    void Update()
    {
        spawnTimer += Time.deltaTime;
        if (spawnTimer >= spawnInterval && drops.Count < dropCount)
        {
            spawnTimer = 0f;
            SpawnDrop();
        }

        for (int i = drops.Count - 1; i >= 0; i--)
        {
            if (drops[i] == null)
            {
                drops.RemoveAt(i);
                dropTimers.RemoveAt(i);
                continue;
            }

            dropTimers[i] += Time.deltaTime;
            float t = dropTimers[i] / dropLifetime;

            if (t >= 1f)
            {
                Destroy(drops[i].gameObject);
                drops.RemoveAt(i);
                dropTimers.RemoveAt(i);
                continue;
            }

            var pos = drops[i].anchoredPosition;
            pos.y -= dropSpeed * Time.deltaTime;
            drops[i].anchoredPosition = pos;

            var img = drops[i].GetComponent<Image>();
            if (img != null)
            {
                var c = img.color;
                c.a = 1f - t;
                img.color = c;
            }
        }
    }

    void SpawnDrop()
    {
        var go = new GameObject("Drop");
        var rt = go.AddComponent<RectTransform>();
        rt.SetParent(transform, false);
        rt.sizeDelta = new Vector2(12, 16);
        rt.anchoredPosition = new Vector2(
            Random.Range(-spreadX / 2f, spreadX / 2f),
            Random.Range(15f, 50f)
        );

        var img = go.AddComponent<Image>();
        img.sprite = dropSprite;
        img.color = new Color(0.4f, 0.7f, 1f, 0.9f);
        img.raycastTarget = false;

        drops.Add(rt);
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